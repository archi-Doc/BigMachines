// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.Threading.Tasks;
global using Arc.Threading;
global using Arc.Unit;
global using BigMachines;
global using CrystalData;
global using Tinyhand;
using Arc;
using Microsoft.Extensions.DependencyInjection;

namespace Advanced;

public class Program
{
    private static ExecutionRoot? root;

    public static async Task Main(string[] args)
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

        // Create a builder for BigMachine and CrystalData.
        var builder = new CrystalUnit.Builder()
            .Configure(context =>
            {// Register some services.
                context.AddSingleton<SomeService>();
                context.AddTransient<ServiceProviderMachine>();
            })
            .ConfigureCrystal(context =>
            {
                context.SetJournal(new SimpleJournalConfiguration(new LocalDirectoryConfiguration("Data/Journal")));
                context.AddCrystal<BigMachine>(new()
                {
                    FileConfiguration = new LocalFileConfiguration("Data/BigMachine.tinyhand"),
                    SaveFormat = SaveFormat.Utf8,
                    NumberOfFileHistories = 3,
                });
            });

        var unit = builder.Build();
        root = unit.Context.Root;
        TinyhandSerializer.ServiceProvider = unit.Context.ServiceProvider; // Set ServiceProvider (required).

        var crystalControl = unit.Context.ServiceProvider.GetRequiredService<CrystalControl>();
        await crystalControl.PrepareAndLoad(false);

        var bigMachine = unit.Context.ServiceProvider.GetRequiredService<BigMachine>();
        bigMachine.Start(); // Start BigMachine.

        // bigMachine.TerminatorMachine.Get(); // This machine will stop the app thread if there is no working machine. -> Start by default

        TestMachine.Test(bigMachine);
        // await PassiveMachine.Test(bigMachine);
        // IntermittentMachine.Test(bigMachine);
        // SequentialMachine.Test(bigMachine);
        // ContinuousMachine.Test(bigMachine);

        // DerivedMachine.Test2(bigMachine);
        // DerivedMachine2.Test(bigMachine);
        // GenericMachine<string>.Test(bigMachine, "gen");
        // await RecursiveMachine.Test(bigMachine);
        // SingleMachine.Test(bigMachine);
        // ServiceProviderMachine.Test(bigMachine);
        // ExternalMachineTest.Test(bigMachine);
        // ParentMachine.Test(bigMachine);

        // var bin = TinyhandSerializer.Serialize(bigMachine);
        // var bigMachine2 = TinyhandSerializer.Deserialize<BigMachine>(bin);

        await bigMachine.ExecutionGroup.WaitForTermination();

        await crystalControl.StoreAndRip();
        await root.WaitForTermination(TerminationOptions.IncludeIndependent); // Wait for the termination infinitely.
    }
}
