using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arc.Threading;
using Arc.Unit;
using BigMachines;
using BigMachines.Redesign;
using Microsoft.Extensions.DependencyInjection;
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

            MachineRegistry.Register(new(0, typeof(BigMachines.Redesign.SingleMachine), typeof(SingleMachineControl<,>)) { Constructor = () => new BigMachines.Redesign.SingleMachine(), Serializable = true, });

            MachineRegistry.Register(new(1, typeof(BigMachines.Redesign.TestMachine), typeof(UnorderedMachineControl<,,>)) { Constructor = () => new BigMachines.Redesign.TestMachine(), IdentifierType = typeof(int), Serializable = true, });

            var testBigMachine = new TestBigMachine();
            var m = testBigMachine.TestMachines.GetOrCreate(1);
            m = testBigMachine.TestMachines.GetOrCreate(1);
            m.PauseMachine();
            m.UnpauseMachine();
            var bin = TinyhandSerializer.Serialize(testBigMachine);
            var testBigMachine2 = TinyhandSerializer.Deserialize<TestBigMachine>(bin);

            // typeof(TestMachine.Interface) => GroupInfo ( Constructor, TypeId, typeof(TestMachine) )
            // BigMachine<int>.StaticInfo[typeof(TestMachine.Interface)] = new(typeof(TestMachine), 0, x => new TestMachine(x), typeof(MachineSingle<>));

            ParentClassT<double>.NestedMachineT.RegisterBM(100);
            ParentClassT<string>.NestedMachineT.RegisterBM(101);
            // ParentClassT2<byte, int>.NestedMachineT2.RegisterBM(101);
            // GenericMachine<int>.RegisterBM(102);
            // MachineLoader.Add(typeof(TestMachineLoader<>));

            var builder = new UnitBuilder()
                .Configure(context =>
                {
                    BigMachine<int>.Configure(context);

                    context.AddTransient<TestMachine>();
                    context.AddTransient(typeof(Sandbox.ParentClassT<>.NestedMachineT));
                });

            var unit = builder.Build();
            var bigMachine = unit.Context.ServiceProvider.GetRequiredService<BigMachine<int>>();

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
            var terminator = bigMachine.CreateOrGet<TerminatorMachine.Interface>(0);
            terminator.RunAsync().Wait();

            // Test1(bigMachine);
            // Test2(bigMachine);
            Test3(bigMachine);

            await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.

            // Save
            var data = await bigMachine.SerializeAsync();
            if (data != null)
            {
                using (var fs = new FileStream("app.data", FileMode.Create))
                {
                    fs.Write(data);
                }

                bigMachine.Deserialize(data);
            }

            ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
        }

        public static void Test3(BigMachine<int> bigMachine)
        {
            var m = bigMachine.CreateOrGet<TestMachine.Interface>(0);

            m.RunAsync();
            m.RunAsync();
            m.ChangeStateAsync(TestMachine.State.First);

            var gorup = m.Group;

            m.CommandAsync(TestMachine.Command.GetInfo, 0);
            var ba = m.SerializeAsync().Result;
        }

        public static void Test2(BigMachine<int> bigMachine)
        {
            var m = bigMachine.CreateOrGet<LongRunningMachine.Interface>(0);

            m.RunAsync();
            m.RunAsync();
            m.RunAsync();
        }

        public static void Test1(BigMachine<int> bigMachine)
        {
            var testMachine = bigMachine.TryGet<TestMachine.Interface>(3);
            testMachine = bigMachine.CreateOrGet<TestMachine.Interface>(4, null);
            testMachine = bigMachine.CreateOrGet<TestMachine.Interface>(3, null);
            if (testMachine != null)
            {
                // var b = testMachine.ChangeStateTwoWay(TestMachine.State.First);
                if (testMachine.TryGetState(out var state) && state == TestMachine.State.First)
                {
                }
            }

            if (testMachine != null)
            {
                testMachine.TryGetState(out var state);
                // testMachine.ChangeStateTwoWay(TestMachine.State.ErrorState);
                testMachine.TryGetState(out state);
            }

            var testGroup = bigMachine.GetGroup<TestMachine.Interface>();
            var res = testGroup.CommandAndReceiveAsync<int, int, int>(4, 4);
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
            bigMachine.CreateOrGet<ContinuousMachine.Interface>(0);
            bigMachine.CreateOrGet<ContinuousMachine.Interface>(1);
            bigMachine.CreateOrGet<ContinuousMachine.Interface>(2);

            // Continuous Watcher
            bigMachine.CreateOrGet<ContinuousWatcher.Interface>(0);
        }
    }
}
