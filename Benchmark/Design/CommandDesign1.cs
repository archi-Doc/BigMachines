using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark.Design
{
    internal class CommandDesign1
    {// Task.Run
        internal const int N = 1000_000;

        private static object obj = new();
        private static object obj2 = new();
        private static int objCount = 0;

        internal static async Task Test()
        {
            var sw = new Stopwatch();

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

            sw.Restart();
            await TestCommandTwoWay();
            sw.Stop();
            Console.WriteLine($"count: {objCount}");
            Console.WriteLine($"time: {sw.ElapsedMilliseconds} ms");

            sw.Restart();
            await TestCommandTwoWayParallel();
            sw.Stop();
            Console.WriteLine($"count: {objCount}");
            Console.WriteLine($"time: {sw.ElapsedMilliseconds} ms");
        }

        internal static async Task TestCommand()
        {
            for (var i = 0; i < N; i++)
            {
                await Task.Run(() =>
                {
                    lock (obj)
                    {
                        Command(1);
                    }
                });
            }
        }

        internal static async Task TestCommandParallel()
        {
            Parallel.For(0, N, x =>
            {
                lock (obj)
                {
                    Command(1);
                }
            });
        }

        internal static async Task TestCommandTwoWay()
        {
            for (var i = 0; i < N; i++)
            {
                var result = await Task.Run(() =>
                {
                    lock (obj)
                    {
                        Command(1);
                        return objCount;
                    }
                });
            }
        }

        internal static async Task TestCommandTwoWayParallel()
        {
            Parallel.For(0, N, x =>
            {
                lock (obj)
                {
                    Command(1);
                    return objCount;
                }
            });

            for (var i = 0; i < N; i++)
            {
                var result = await Task.Run(() =>
                {
                    lock (obj)
                    {
                        Command(1);
                        return objCount;
                    }
                });
            }
        }

        internal static void Command(int n)
        {
            objCount++;
            return;
        }
    }
}
