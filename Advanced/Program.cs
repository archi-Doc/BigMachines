// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using DryIoc;

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

        // await Test();

        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }

    public static async Task Test()
    {
        var container = new Container(); // You can use DI container if you want.
        container.RegisterDelegate<BigMachine<int>>(x => new BigMachine<int>(container), Reuse.Singleton);
        // container.Register<SomeService>(); // Register some service.
        // container.Register<ServiceProviderMachine>(Reuse.Transient); // Register machine.
        var bigMachine = container.Resolve<BigMachine<int>>(); // Create BigMachine.

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

        bigMachine.Start(); // Start BigMachine.

        // TerminatorMachine<int>.Start(bigMachine, 0); // This machine will stop the app thread if there is no working machine.

        // TestMachine.Test(bigMachine);
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

        // Save
        var data = await bigMachine.SerializeAsync();
        if (data != null)
        {
            using (var fs = new FileStream("app.data", FileMode.Create))
            {
                fs.Write(data);
            }
        }
    }
}
