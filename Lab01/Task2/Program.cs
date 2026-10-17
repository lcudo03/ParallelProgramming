
using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static int coalDeposit = 2000;
    static int warehouse = 0;
    static int minersCount = 5;

    static SemaphoreSlim mineSemaphore = new SemaphoreSlim(2, 2);
    static SemaphoreSlim warehouseSemaphore = new SemaphoreSlim(1, 1);

    static object lockObject = new object();
    static object consoleLock = new object();

    static string[] minerStatus = new string[minersCount];

    static bool simulationRunning = true;

    static void UpdateStatus(int id, string status)
    {
        lock (lockObject)
        {
            minerStatus[id - 1] = status;
        }

        lock (consoleLock)
        {
            Console.SetCursorPosition(0, id + 3);
            Console.Write($"Górnik {id}: {status}".PadRight(60));
        }
    }

    static void DisplayResources()
    {
        while (simulationRunning)
        {
            int deposit;
            int storage;

            lock (lockObject)
            {
                deposit = coalDeposit;
                storage = warehouse;
            }

            lock (consoleLock)
            {
                Console.SetCursorPosition(0, 0);
                Console.Write($"Stan złoża: {deposit} jednostek węgla".PadRight(60));

                Console.SetCursorPosition(0, 1);
                Console.Write($"Stan magazynu: {storage} jednostek węgla".PadRight(60));
            }

            Thread.Sleep(100);
        }
    }

    static void Miner(int id)
    {
        while (true)
        {
            int coal = 0;

            UpdateStatus(id, "Czeka na dostęp do złoża...");

            mineSemaphore.Wait();

            try
            {
                UpdateStatus(id, "Wydobywa węgiel...");

                while (coal < 200)
                {
                    bool extracted = false;

                    lock (lockObject)
                    {
                        if (coalDeposit > 0)
                        {
                            coalDeposit--;
                            coal++;
                            extracted = true;
                        }
                    }

                    if (!extracted)
                        break;

                    Thread.Sleep(10);
                }
            }
            finally
            {
                mineSemaphore.Release();
            }

            if (coal == 0)
                break;

            UpdateStatus(id, $"Transportuje {coal} jednostek węgla...");

            Thread.Sleep(10000);

            UpdateStatus(id, "Czeka na rozładunek...");

            warehouseSemaphore.Wait();

            try
            {
                UpdateStatus(id, "Rozładowuje węgiel...");

                for (int i = 0; i < coal; i++)
                {
                    Thread.Sleep(10);

                    lock (lockObject)
                    {
                        warehouse++;
                    }
                }
            }
            finally
            {
                warehouseSemaphore.Release();
            }

            UpdateStatus(id, "Wraca do kopalni");
            Thread.Sleep(10000);
        }

        UpdateStatus(id, "Zakończył pracę.");
    }

    static void Main()
    {
        Console.Clear();

        Task[] tasks = new Task[minersCount];

        Task displayTask = Task.Run(DisplayResources);

        for (int i = 0; i < minersCount; i++)
        {
            int id = i + 1;
            tasks[i] = Task.Run(() => Miner(id));
        }

        Task.WaitAll(tasks);

        simulationRunning = false;
        displayTask.Wait();

        lock (consoleLock)
        {
            Console.SetCursorPosition(0, minersCount + 5);
            Console.WriteLine("KONIEC SYMULACJI");
            Console.WriteLine($"Pozostało w złożu: {coalDeposit}");
            Console.WriteLine($"W magazynie: {warehouse}");
        }
    }
}
