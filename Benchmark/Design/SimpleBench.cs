using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BigMachines;

namespace Benchmark.Design
{
    [MachineObject(0)]
    internal partial class SimpleBenchMachine : Machine<int>
    {
        public SimpleBenchMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
        }

        [CommandMethod(0)]
        public void Test(CommandPost<int>.Command command)
        {
            var message = command.Message;
        }
    }

    internal class SimpleBench
    {
        internal const int N = 1000_000;

        private static SimpleBenchMachine.Interface machine = default!;

        internal static async Task Test(BigMachine<int> bigMachine)
        {
            machine = bigMachine.CreateNew<SimpleBenchMachine.Interface>(0);
            var sw = new Stopwatch();

            Start("Command");
            await TestCommand();
            Stop();

            Start("CommandTwoWay");
            await TestCommandTwoWay();
            Stop();

            Console.WriteLine();

            void Start(string name)
            {
                Console.Write($"{name, -25}: ");
                sw.Restart();
            }

            void Stop()
            {
                sw.Stop();
                Console.WriteLine($"{sw.ElapsedMilliseconds} ms");
            }

            /*void Stop2()
            {
                sw.Stop();
                Console.WriteLine($"{sw.ElapsedTicks} ticks");
            }*/
        }

        internal static async Task TestCommand()
        {
            for (var i = 0; i < 1000_000; i++)
            {
                _ = machine.CommandAsync(SimpleBenchMachine.Command.Test, 0);
            }
        }

        internal static async Task TestCommandTwoWay()
        {
            for (var i = 0; i < 1000_000; i++)
            {
                _ = machine.CommandAndReceiveAsync<int, int>(SimpleBenchMachine.Command.Test, 0);
            }
        }
    }
}
