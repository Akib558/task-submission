using System;
using System.Threading;

class MutexExample
{
    private static int globalCounter = 0;
    private static Mutex mut = new Mutex();

    private static void IncrementGlobalCounterWithMutex()
    {
        mut.WaitOne();
        globalCounter++;
        Console.WriteLine($"Thread {Thread.CurrentThread.Name} found counter {globalCounter}");
        mut.ReleaseMutex();
    }

    private static void IncrementGlobalCounterWithoutMutex()
    {
        globalCounter++;
        Console.WriteLine($"Thread {Thread.CurrentThread.Name} found counter {globalCounter}");
    }
    
    static void Main()
    {
        Console.WriteLine("---------------WithOutMutex------------------");
        for (int i = 0; i < 10; i++)
        {
            var thread = new Thread(IncrementGlobalCounterWithoutMutex);
            thread.Name = "Thread " + i;
            thread.Start();
        }
        Thread.Sleep(1000);
        Console.WriteLine($"Global Counter: {globalCounter}");
        
        globalCounter = 0;
        Console.WriteLine("---------------WithMutex------------------");
        Thread.Sleep(50);
        for (int i = 0; i < 10; i++)
        {
            var thread = new Thread(IncrementGlobalCounterWithMutex);
            thread.Name = "Thread " + i;
            thread.Start();
        }
        
        Thread.Sleep(1000);
        Console.WriteLine($"Global Counter: {globalCounter}");

    }
}