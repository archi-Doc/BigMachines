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
        private static ManualResetEvent manualEvent = new(false);
        private static bool EventFlag = false;

        internal static async Task Test()
        {
            var sw = new Stopwatch();

            var t = new Thread(ReceiveAction);
            // t.Priority = ThreadPriority.Highest;
            t.Start();

            sw.Restart();
            await TestCommand();
            sw.Stop();

            Console.WriteLine($"count: {objCount}");
            Console.WriteLine($"time: {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            await TestCommandParallel();
            sw.Stop();

            Console.WriteLine($"count: {objCount}");
            Console.WriteLine($"time: {sw.ElapsedMilliseconds} ms");

            t.Join();
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

        internal static void ReceiveAction()
        {
            while (true)
            {
                // manualEvent.WaitOne();
                Thread.Sleep(1);
                if (EventFlag)
                {
                }
                if (objCount == 2000_000)
                {
                    break;
                }
            }
        }
    }
}
