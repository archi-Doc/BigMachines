using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmark.Design
{
    internal class CommandDesign2
    {// Task.Run
        internal const int N = 1000_000;

        private static object obj = new();
        private static int objCount = 0;
        private static int processedCount = 0;
        // private static ManualResetEvent manualEvent = new(false); // Just slow
        private static bool EventFlag = false;

        internal static async Task Test()
        {
            var sw = new Stopwatch();
            var cts = new CancellationTokenSource();

            var t = Task.Run(() => ReceiveAction(cts.Token));

            Start("lock + flag");
            await TestCommand();
            Stop();

            Start("lock + flag Parallel");
            await TestCommandParallel();
            Stop();

            Start("lock + flag TwoWay");
            await TestCommandTwoWay();
            Stop();

            cts.Cancel();
            await t;

            void Start(string name)
            {
                Console.WriteLine(name);
                sw.Restart();
            }

            void Stop()
            {
                sw.Stop();
                Console.WriteLine($"count: {objCount}");
                Console.WriteLine($"time: {sw.ElapsedMilliseconds} ms");
                Console.WriteLine();
            }
        }

        internal static async Task TestCommand()
        {
            for (var i = 0; i < N; i++)
            {
                Command(1);
            }
        }

        internal static async Task TestCommandParallel()
        {
            Parallel.For(0, N, x =>
            {
                Command(1);
            });
        }

        internal static async Task TestCommandTwoWay()
        {
            for (var i = 0; i < N; i++)
            {
                await CommandTwoWay(100);
            }
        }

        internal static void Command(int n)
        {
            lock (obj)
            {
                objCount++;
                EventFlag = true;
                // manualEvent.Set();
            }

            return;
        }

        internal static async Task<int> CommandTwoWay(int millisecondsTimeout)
        {
            lock (obj)
            {
                objCount++;
                EventFlag = true;
                // manualEvent.Set();
            }

            if (millisecondsTimeout <= 0)
            {
                return default;
            }

            while (true)
            {
                lock (obj)
                {
                    if (processedCount > 0)
                    {
                        processedCount--;
                        return processedCount;
                    }
                }

                await Task.Delay(millisecondsTimeout);
            }
        }

        internal static void ReceiveAction(object? parameter)
        {
            var cancellationToken = (CancellationToken)parameter!;
            var lastCount = 0;
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
                    Thread.Sleep(1);
                    continue;
                }
                else
                {
                    EventFlag = false;
                }

                lock (obj)
                {
                    while (objCount > lastCount)
                    {
                        processedCount++;
                        lastCount++;
                    }
                }
            }
        }
    }
}
