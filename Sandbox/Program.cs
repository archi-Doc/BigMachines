using System;
using System.Threading.Tasks;
using Arc.Threading;
using Arc.Unit;
using BigMachines;
using Microsoft.Extensions.DependencyInjection;
using Tinyhand;

namespace Sandbox;

[BigMachineObject(Inclusive = true)]
public partial class BigMachine { }

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

        var builder2 = new UnitBuilder();
        builder2.Configure(context =>
        {
            context.AddSingleton<BigMachines.SingleMachine>();
            context.AddSingleton<BigMachines.TestMachine>();
        });

        var unit2 = builder2.Build();
        TinyhandSerializer.ServiceProvider = unit2.Context.ServiceProvider;

        // MachineRegistry.Register(new(0, typeof(BigMachines.SingleMachine), typeof(SingleMachineControl<,>)) { Serializable = true, });
        // MachineRegistry.Register(new(1, typeof(BigMachines.TestMachine), typeof(UnorderedMachineControl<,,>)) { Constructor = () => new BigMachines.TestMachine(), IdentifierType = typeof(int), Serializable = true, });

        /*var testBigMachine = new TestBigMachine();
        var m = testBigMachine.TestMachines.GetOrCreate(1);
        m = testBigMachine.TestMachines.GetOrCreate(1);
        m.PauseMachine();
        m.UnpauseMachine();
        var m2 = testBigMachine.SingleMachine.Get();
        var bin = TinyhandSerializer.Serialize(testBigMachine);
        var testBigMachine2 = TinyhandSerializer.Deserialize<TestBigMachine>(bin);*/

        Console.WriteLine("BigMachines Sandbox");

        var bigMachine = new BigMachine();
        bigMachine.Start(ThreadCore.Root);

        var tinyControl = bigMachine.TinyMachine;
        var machine = tinyControl.Get();
        // var result = await bigMachine.TinyMachine.Get().Command.Command1(10);

        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.

        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
        Console.WriteLine("Terminated.");
    }

    /*public static void Test1(BigMachine<int> bigMachine)
    {
    }*/
}
