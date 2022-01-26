using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using DryIoc;
using Tinyhand;

namespace Sandbox
{
    class Program
    {
        public class TestMachineLoader<TIdentifier> : IMachineLoader<TIdentifier>
            where TIdentifier : notnull
        {
            public void Load()
            {
                GenericMachine<TIdentifier>.RegisterBM(102);
                // NestedGenericParent<int>.NestedGenericMachine<TIdentifier>.RegisterBM(111);
            }
        }

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
            // BigMachine<int>.StaticInfo[typeof(TestMachine.Interface)] = new(typeof(TestMachine), 0, x => new TestMachine(x), typeof(MachineSingle<>));

            ParentClassT<double>.NestedMachineT.RegisterBM(100);
            ParentClassT<string>.NestedMachineT.RegisterBM(101);
            // ParentClassT2<byte, int>.NestedMachineT2.RegisterBM(101);
            // GenericMachine<int>.RegisterBM(102);
            // MachineLoader.Add(typeof(TestMachineLoader<>));

            var container = new Container();
            container.RegisterDelegate<BigMachine<int>>(x => new BigMachine<int>(container), Reuse.Singleton);
            container.Register<TestMachine>(Reuse.Singleton);
            // container.Register<Sandbox.ParentClassT<>.NestedMachineT>(Reuse.Singleton);
            container.Register(typeof(Sandbox.ParentClassT<>.NestedMachineT));
            var bigMachine = container.Resolve<BigMachine<int>>();

            Console.WriteLine("BigMachines Sandbox");
            // var bigMachine = new BigMachine<int>(ThreadCore.Root);

            // Load
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

            bigMachine.Start();

            // Test1(bigMachine);
            // Test2(bigMachine);
            Test3(bigMachine);

            await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.

            // Save
            var data = bigMachine.Serialize();
            using (var fs = new FileStream("app.data", FileMode.Create))
            {
                fs.Write(data);
            }

            bigMachine.Deserialize(data);

            ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
        }

        public static void Test3(BigMachine<int> bigMachine)
        {
            var m = bigMachine.TryCreate<TestMachine.Interface>(0);

            m.Run();
            m.Run();
            m.Run();

            var ca = m.Group.Serialize();
            var ba = m.Serialize();
        }

        public static void Test2(BigMachine<int> bigMachine)
        {
            var m = bigMachine.TryCreate<LongRunningMachine.Interface>(0);

            m.Run();
            m.Run();
            m.Run();
        }

        public static void Test1(BigMachine<int> bigMachine)
        {
            bigMachine.TryCreate<TerminatorMachine.Interface>(0);

            var testMachine = bigMachine.TryGet<TestMachine.Interface>(3);
            testMachine = bigMachine.TryCreate<TestMachine.Interface>(4, null);
            testMachine = bigMachine.TryCreate<TestMachine.Interface>(3, null);
            if (testMachine != null)
            {
                // var b = testMachine.ChangeStateTwoWay(TestMachine.State.First);
                if (testMachine.GetCurrentState() == TestMachine.State.First)
                {
                }
            }

            if (testMachine != null)
            {
                var state = testMachine.GetCurrentState();
                // testMachine.ChangeStateTwoWay(TestMachine.State.ErrorState);
                state = testMachine.GetCurrentState();
            }

            var testGroup = bigMachine.GetGroup<TestMachine.Interface>();

            var res = testGroup.CommandGroupTwoWay<int, int>(4);
            // var result = testMachine?.RunTwoWay(33);

            var identifiers = testGroup.GetIdentifiers().ToArray();
            // bigMachine.Create<TestMachine.Interface>(4);
            identifiers = testGroup.GetIdentifiers().ToArray();

            // testMachine?.Run();

            // bigMachine.TryCreate<ParentClass.NestedMachine.Interface>(10);
            // bigMachine.TryCreate<ParentClassT<double>.NestedMachineT.Interface>(10);
            // bigMachine.TryCreate<GenericMachine<int>.Interface>(10);
            // bigMachine.TryCreate<ParentClassT2<byte, int>.NestedMachineT2.Interface>(11);

            // Continuous
            bigMachine.TryCreate<ContinuousMachine.Interface>(0);
            bigMachine.TryCreate<ContinuousMachine.Interface>(1);
            bigMachine.TryCreate<ContinuousMachine.Interface>(2);

            // Continuous Watcher
            bigMachine.TryCreate<ContinuousWatcher.Interface>(0);
        }
    }
}
