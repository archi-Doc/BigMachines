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
    internal class CommandDesign7
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
        private static CancellationTokenSource cancellationTokenSource = new();
        private static CancellationToken cancellationToken;
        // private static bool EventFlag = false;

        internal static async Task Test()
        {
            cancellationToken = cancellationTokenSource.Token;
            var sw = new Stopwatch();

            Start("Task.Run");
            await TestCommand();
            Stop();

            Start("Task.Run Parallel");
            await TestCommandParallel();
            Stop();

            Start("await Task.Run");
            await TestCommandTwoWay();
            Stop();

            Start("await Task.Run response");
            await SendTwoWay();
            Stop2();

            Start("await Task.Run response");
            await SendTwoWay();
            Stop2();

            Start("await Task.Run response");
            await SendTwoWay();
            Stop2();

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
            Task.Run(() => ReceiveAction(new Command(false)));

            return;
        }

        internal static async Task SendTwoWay()
        {
            await Task.Run(() => ReceiveAction(new Command(false)));

            /*while (true)
            {
                if (c.Flag)
                {
                    return;
                }

                if (manualEvent2.Wait(5))
                {
                    manualEvent2.Reset();
                }
            }*/
        }

        internal static void ReceiveAction(Command command)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            command.Flag = true;
        }
    }
}
