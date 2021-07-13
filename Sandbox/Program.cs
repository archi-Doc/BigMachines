using System;
using System.IO;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using DryIoc;
using Tinyhand;

namespace Sandbox
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += async (s, e) =>
            {// Console window closing or process terminated.
                ThreadCore.Root.Terminate(); // Send a termination signal to the root.
                ThreadCore.Root.TerminationEvent.WaitOne(2000); // Wait until the termination process is complete (#1).
            };

            Console.CancelKeyPress += (s, e) =>
            {// Ctrl+C pressed
                e.Cancel = true;
                ThreadCore.Root.Terminate(); // Send a termination signal to the root.
            };

            // typeof(TestMachine.Interface) => GroupInfo ( Constructor, TypeId, typeof(TestMachine) )
            BigMachine<int>.StaticInfo[typeof(TestMachine.Interface)] = new(typeof(TestMachine), 0, x => new TestMachine(x));

            /*var container = new Container();
            container.RegisterDelegate<BigMachine<int>>(x => new BigMachine<int>(ThreadCore.Root, container), Reuse.Singleton);
            container.Register<TestMachine>(Reuse.Singleton);
            var bigMachine = container.Resolve<BigMachine<int>>();*/
            // var container_machine = container.Resolve<TestMachine>();

            Console.WriteLine("BigMachines Sandbox");
            var bigMachine = new BigMachine<int>(ThreadCore.Root);

            try
            {
                using (var fs = new FileStream("app.data", FileMode.Open))
                {
                    var bs = new byte[fs.Length];
                    fs.Read(bs);
                    bigMachine.Deserialize(bs);
                }
            }
            catch
            {
            }

            var testMachine = bigMachine.TryGet<TestMachine.Interface>(3);
            testMachine = bigMachine.TryCreate<TestMachine.Interface>(3, null);
            testMachine = bigMachine.TryCreate<TestMachine.Interface>(3, null);
            if (testMachine != null)
            {
                // var b = testMachine.ChangeStateTwoWay(TestMachine.State.First);
                if (testMachine.GetCurrentState() == TestMachine.State.First)
                {
                }
            }

            var testGroup = bigMachine.GetGroup<TestMachine.Interface>();

            var bb = bigMachine.Serialize();
            bigMachine.Deserialize(bb);

            var testMachine2 = bigMachine.TryCreate<TestMachine.Interface>(4, null);

            var res = testGroup.CommandGroupTwoWay<int, int>(4);
            // var result = testMachine?.RunTwoWay(33);

            // testMachine?.Run();

            await ThreadCore.Root.WaitForTermination(-1); // Wait for the termination infinitely.

            var data = bigMachine.Serialize();
            using (var fs = new FileStream("app.data", FileMode.Create))
            {
                fs.Write(data);
            }

            ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
        }
    }
}
