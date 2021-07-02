using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmark.Design
{
    internal class CommandDesign3
    {// Task.Run
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
        // private static ManualResetEvent manualEvent = new(false); // Just slow
        private static bool EventFlag = false;

        internal static async Task Test()
        {
            var sw = new Stopwatch();
            var cts = new CancellationTokenSource();

            var t = new Thread(ReceiveAction); // Task.Run(() => ReceiveAction(cts.Token));
            t.Priority = ThreadPriority.AboveNormal;
            t.Start(cts.Token);

            Start("Flag");
            await TestCommand();
            Stop();

            Start("Flag Parallel");
            await TestCommandParallel();
            Stop();

            Start("Flag TwoWay");
            await TestCommandTwoWay();
            Stop();

            Start("Flag TwoWay response");
            await SendTwoWay();
            Stop2();

            Start("Flag TwoWay response");
            await SendTwoWay();
            Stop2();

            Start("Flag TwoWay response");
            await SendTwoWay();
            Stop2();

            cts.Cancel();
            t.Join();

            void Start(string name)
            {
                Console.WriteLine(name);
                sw.Restart();
            }

            void Stop()
            {
                sw.Stop();
                // Console.WriteLine($"count: {objCount}");
                Console.WriteLine($"time: {sw.ElapsedMilliseconds} ms");
                Console.WriteLine();
            }

            void Stop2()
            {
                sw.Stop();
                // Console.WriteLine($"count: {objCount}");
                Console.WriteLine($"ticks: {sw.ElapsedTicks}");
                Console.WriteLine();
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
            for (var i = 0; i < 100; i++)
            {
                await SendTwoWay();
            }
        }

        internal static void Send(int n)
        {
            concurrentQueue.Enqueue(new Command(false));

            EventFlag = true;
            // manualEvent.Set();

            return;
        }

        internal static async Task SendTwoWay()
        {
            var c = new Command(false);
            concurrentQueue.Enqueue(c);

            EventFlag = true;
            // manualEvent.Set();

            while (true)
            {
                if (c.Flag)
                {
                    return;
                }

                await Task.Delay(MillisecondInterval);
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
                /*else if (!manualEvent.WaitOne(1))
                {
                    continue;
                }
                else
                {
                    manualEvent.Reset();
                }*/
                else if (!EventFlag)
                {// manualEvent.WaitOne();
                    Thread.Sleep(5);
                    continue;
                }
                else
                {
                    EventFlag = false;
                }

                while (concurrentQueue.TryDequeue(out var command))
                {
                    command.Flag = true;
                }
            }
        }
    }
}
