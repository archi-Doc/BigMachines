using System;
using System.Threading.Tasks;
using Arc;
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
    private static ExecutionRoot? root;

    static async Task Main(string[] args)
    {
        AppCloseHandler.Set(() =>
        {// Closing the console window or terminating the process.
            root?.RequestTermination(); // Send a termination signal to the root.
            root?.WaitForTermination(TimeSpan.FromSeconds(2)).Wait();
        });

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed.
            e.Cancel = true;
            root?.RequestTermination(); // Send a termination signal to the root.
        };

        var builder2 = new UnitBuilder();
        builder2.Configure(context =>
        {
            context.AddSingleton<TinyMachine>();
        });

        var unit = builder2.Build();
        root = unit.Context.Root;
        TinyhandSerializer.ServiceProvider = unit.Context.ServiceProvider;

        Console.WriteLine("BigMachines Playground");

        var bigMachine = new BigMachine();
        bigMachine.Start();

        var tinyControl = bigMachine.TinyMachine;
        var machine = tinyControl.GetOrCreate();

        await root.WaitForTermination(); // Wait for the termination infinitely.

        Console.WriteLine("Terminated.");
    }
}
