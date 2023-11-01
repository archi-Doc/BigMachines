// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

#pragma warning disable SA1210 // Using directives should be ordered alphabetically by namespace

global using System;
global using System.Threading.Tasks;
global using Arc.Threading;
global using Arc.Unit;
global using BigMachines;
global using CrystalData;
global using Tinyhand;
using Microsoft.Extensions.DependencyInjection;

namespace Advanced;

public class Program
{
    public static async Task Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += async (s, e) =>
        {// Console window is closing or process terminated.
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
            ThreadCore.Root.TerminationEvent.WaitOne(2000); // Wait until the termination process is complete (#1).
        };

        Console.CancelKeyPress += (s, e) =>
        {// Ctrl+C pressed
            e.Cancel = true;
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        };

        /*var dt = DateTime.MaxValue;
        dt = dt.ToUniversalTime();
        var aa = dt.Ticks;
        var st = dt.ToString("o", CultureInfo.InvariantCulture);
        DateTime.Parse(st, CultureInfo.InvariantCulture).ToUniversalTime(); //
        DateTime.Parse("2023-11-01T08:28:53.4299242Z", CultureInfo.InvariantCulture).ToUniversalTime();
        DateTime.Parse("9999-12-31T23:59:59.9999999Z", CultureInfo.InvariantCulture).ToUniversalTime();*/

        // Create a builder for BigMachine and CrystalData.
        var builder = new CrystalControl.Builder()
            .Configure(context =>
            {
                context.AddSingleton<BigMachine>();
            })
            .ConfigureCrystal(context =>
            {
                context.SetJournal(new SimpleJournalConfiguration(new LocalDirectoryConfiguration("Data/Journal")));
                context.AddCrystal<BigMachine>(new()
                {
                    FileConfiguration = new LocalFileConfiguration("Data/BigMachine.tinyhand"),
                    SavePolicy = SavePolicy.Manual,
                    SaveFormat = SaveFormat.Utf8,
                    NumberOfFileHistories = 3,
                });
            });

        var unit = builder.Build();
        TinyhandSerializer.ServiceProvider = unit.Context.ServiceProvider;

        var crystalizer = unit.Context.ServiceProvider.GetRequiredService<Crystalizer>();
        await crystalizer.PrepareAndLoadAll(false);

        var bigMachine = unit.Context.ServiceProvider.GetRequiredService<BigMachine>();
        ((IBigMachine)bigMachine).Start(ThreadCore.Root); // Start BigMachine.

        bigMachine.TerminatorMachine.Get(); // This machine will stop the app thread if there is no working machine.

        TestMachine.Test(bigMachine);
        // await PassiveMachine.Test(bigMachine);
        // IntermittentMachine.Test(bigMachine);
        // ContinuousMachine.Test(bigMachine);

        // Other test code.
        // DerivedMachine.Test2(bigMachine);
        // DerivedMachine2.Test(bigMachine);
        // GenericMachine<int>.Test(bigMachine);
        // LoopMachine.Test(bigMachine);
        // SingleMachine.Test(bigMachine);
        // ServiceProviderMachine.Test(bigMachine);
        // QueuedMachine.Test(bigMachine);

        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.

        await crystalizer.SaveAllAndTerminate();

        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }
}
