
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static int coalDeposit;
    static int warehouse;

    static SemaphoreSlim mineSemaphore = new SemaphoreSlim(2, 2);
    static SemaphoreSlim warehouseSemaphore = new SemaphoreSlim(1, 1);

    static object lockObject = new object();
    static object consoleLock = new object();

    static string[] minerStatus = new string[0];
    static volatile bool simulationRunning;

    static void UpdateStatus(int id, string status)
    {
        lock (lockObject)
        {
            minerStatus[id - 1] = status;
        }
    }

    static void DisplayResources()
    {
        while (simulationRunning)
        {
            lock (lockObject)
            {
                lock (consoleLock)
                {
                    Console.Clear();

                    Console.WriteLine($"Stan złoża: {coalDeposit}");
                    Console.WriteLine($"Stan magazynu: {warehouse}");
                    Console.WriteLine();

                    for (int i = 0; i < minerStatus.Length; i++)
                    {
                        Console.WriteLine($"Górnik {i + 1}: {minerStatus[i]}");
                    }
                }
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

            UpdateStatus(id, "Transportuje węgiel...");

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

    static double RunSimulation(int minersCount)
    {
        coalDeposit = 2000;
        warehouse = 0;

        mineSemaphore = new SemaphoreSlim(2, 2);
        warehouseSemaphore = new SemaphoreSlim(1, 1);

        minerStatus = new string[minersCount];

        for (int i = 0; i < minersCount; i++)
        {
            minerStatus[i] = "Oczekuje...";
        }

        simulationRunning = true;

        Task displayTask = Task.Run(DisplayResources);

        Task[] tasks = new Task[minersCount];

        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < minersCount; i++)
        {
            int id = i + 1;
            tasks[i] = Task.Run(() => Miner(id));
        }

        Task.WaitAll(tasks);

        stopwatch.Stop();

        simulationRunning = false;
        displayTask.Wait();

        Console.Clear();
        Console.WriteLine($"Symulacja dla {minersCount} górników zakończona.");
        Console.WriteLine($"Stan złoża: {coalDeposit}");
        Console.WriteLine($"Stan magazynu: {warehouse}");
        Console.WriteLine($"Czas: {stopwatch.Elapsed.TotalSeconds:F2} s");

        Thread.Sleep(1000);

        return stopwatch.Elapsed.TotalSeconds;
    }

    static void Main()
    {
        double[] times = new double[6];

        for (int i = 1; i <= 6; i++)
        {
            times[i - 1] = RunSimulation(i);
        }

        Console.Clear();
        Console.WriteLine("WYNIKI SYMULACJI");
        Console.WriteLine();

        Console.WriteLine("Górnicy | Czas [s] | Przyspieszenie | Efektywność");
        Console.WriteLine("------------------------------------------------");

        for (int i = 1; i <= 6; i++)
        {
            double speedup = times[0] / times[i - 1];
            double efficiency = speedup / i;

            Console.WriteLine(
                $"{i,7} | {times[i - 1],8:F2} | {speedup,13:F2} | {efficiency,10:F2}");
        }

        Console.ReadKey();
    }
}
