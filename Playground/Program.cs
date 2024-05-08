using System;
using System.Threading.Tasks;
using Arc.Threading;
using Arc.Unit;
using BigMachines;
using Microsoft.Extensions.DependencyInjection;
using Playground;
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
            context.AddSingleton<TinyMachine>();
        });

        var unit2 = builder2.Build();
        TinyhandSerializer.ServiceProvider = unit2.Context.ServiceProvider;

        Console.WriteLine("BigMachines Playground");

        var bigMachine = new BigMachine();
        bigMachine.Start(ThreadCore.Root);

        var tinyControl = bigMachine.TinyMachine;
        var machine = tinyControl.GetOrCreate();

        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.

        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
        Console.WriteLine("Terminated.");
    }
}
