
using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static int coalDeposit = 2000;
    static int warehouse = 0;

    static SemaphoreSlim mineSemaphore = new SemaphoreSlim(2, 2);
    static SemaphoreSlim warehouseSemaphore = new SemaphoreSlim(1, 1);
    static object lockObject = new object();

    static void Miner(int id)
    {
        while (true)
        {
            int coal = 0;

            mineSemaphore.Wait();

            try
            {
                while (coal < 200)
                {
                    lock (lockObject)
                    {
                        if (coalDeposit > 0)
                        {
                            coalDeposit--;
                            coal++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    Thread.Sleep(10);
                }

                if (coal > 0)
                {
                    Console.WriteLine($"Górnik {id} wydobył {coal} jednostek. Pozostało: {coalDeposit}");
                }
            }
            finally
            {
                mineSemaphore.Release();
            }

            if (coal == 0)
                break;

            Console.WriteLine($"Górnik {id} transportuje węgiel...");

            Thread.Sleep(10000);

            warehouseSemaphore.Wait();

            try
            {
                Console.WriteLine($"Górnik {id} rozładowuje węgiel...");

                Thread.Sleep(coal * 10);

                lock (lockObject)
                {
                    warehouse += coal;
                }
            }
            finally
            {
                warehouseSemaphore.Release();
            }

            Console.WriteLine($"Górnik {id} zakończył rozładunek.");

            Console.WriteLine($"Górnik {id} wraca do kopalni...");

            Thread.Sleep(10000);
        }

        Console.WriteLine($"Górnik {id} zakończył pracę.");
    }

    static void Main()
    {
        int minersCount = 5;
        Task[] tasks = new Task[minersCount];

        for (int i = 0; i < minersCount; i++)
        {
            int id = i + 1;
            tasks[i] = Task.Run(() => Miner(id));
        }

        Task.WaitAll(tasks);

        Console.WriteLine("\nKONIEC SYMULACJI");
        Console.WriteLine($"Pozostało w złożu: {coalDeposit}");
        Console.WriteLine($"W magazynie: {warehouse}");
    }
}
