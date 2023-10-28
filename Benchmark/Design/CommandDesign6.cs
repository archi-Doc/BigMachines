using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmark.Design;

internal class CommandDesign6
{// Thread + ManualResetEventSlim
    internal class Command
    {
        public Command(bool flag)
        {
            this.Flag = flag;
        }

        public bool Flag { get; set; }
    }

    internal const int N = 1000_000;
    internal const int MillisecondInterval = 5;

    // private static object obj = new();
    private static ConcurrentQueue<Command> concurrentQueue = new();
    private static ManualResetEventSlim manualEvent = new(false); // Just slow
    private static ManualResetEventSlim manualEvent2 = new(false); // Just slow
    // private static bool EventFlag = false;

    internal static async Task Test()
    {
        var sw = new Stopwatch();
        var cts = new CancellationTokenSource();

        var t = new Thread(ReceiveAction); // Task.Run(() => ReceiveAction(cts.Token));
        t.Priority = ThreadPriority.AboveNormal;
        t.Start(cts.Token);

        Start("Event");
        await TestCommand();
        Stop();

        Start("Event Parallel");
        await TestCommandParallel();
        Stop();

        Start("Event TwoWay");
        await TestCommandTwoWay();
        Stop();

        Start("Event TwoWay response");
        await SendTwoWay();
        Stop2();

        Start("Event TwoWay response");
        await SendTwoWay();
        Stop2();

        Start("Event TwoWay response");
        await SendTwoWay();
        Stop2();

        cts.Cancel();
        t.Join();

        Console.WriteLine();

        void Start(string name)
        {
            Console.Write($"{name,-25}: ");
            sw.Restart();
        }

        void Stop()
        {
            sw.Stop();
            Console.WriteLine($"{sw.ElapsedMilliseconds} ms");
        }

        void Stop2()
        {
            sw.Stop();
            Console.WriteLine($"{sw.ElapsedTicks} ticks");
        }
    }

    internal static async Task TestCommand()
    {
        for (var i = 0; i < N; i++)
        {
            Send(1);
        }
    }

    internal static async Task TestCommandParallel()
    {
        Parallel.For(0, N, x =>
        {
            Send(1);
        });
    }

    internal static async Task TestCommandTwoWay()
    {
        for (var i = 0; i < N; i++)
        {
            await SendTwoWay();
        }
    }

    internal static void Send(int n)
    {
        concurrentQueue.Enqueue(new Command(false));

        // EventFlag = true;
        manualEvent.Set();

        return;
    }

    internal static async Task SendTwoWay()
    {
        var c = new Command(false);
        concurrentQueue.Enqueue(c);

        // EventFlag = true;
        manualEvent.Set();

        while (true)
        {
            if (c.Flag)
            {
                return;
            }

            if (manualEvent2.Wait(5))
            {
                manualEvent2.Reset();
            }
        }
    }

    internal static void ReceiveAction(object? parameter)
    {
        var cancellationToken = (CancellationToken)parameter!;
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            else if (manualEvent.Wait(5))
            {
                manualEvent.Reset();
            }
            /*else if (!EventFlag)
            {// manualEvent.WaitOne();
                Thread.Sleep(1);
                continue;
            }
            else
            {
                EventFlag = false;
            }*/

            while (concurrentQueue.TryDequeue(out var command))
            {
                command.Flag = true;
            }

            manualEvent2.Set();
        }
    }
}
