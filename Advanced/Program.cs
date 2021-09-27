// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using DryIoc;
using Tinyhand;

#pragma warning disable SA1201 // Elements should appear in the correct order

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

        /*TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Console.WriteLine(e.Exception);
        };*/

        await Test();
        // await Test2();

        ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
    }

    public static async Task Test()
    {
        var container = new Container(); // You can use DI container if you want.
        container.RegisterDelegate<BigMachine<int>>(x => new BigMachine<int>(ThreadCore.Root, container), Reuse.Singleton);
        container.Register<SomeService>(); // Register some service.
        container.Register<ServiceProviderMachine>(Reuse.Transient); // Register machine.
        // container.Register<TestMachine>(Reuse.Transient); BigMachine will use default constructor if not registered.
        var bigMachine = container.Resolve<BigMachine<int>>(); // Create BigMachine.
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

        TerminatorMachine<int>.Test(bigMachine, 0);

        // TestMachine.Test(bigMachine);
        // PassiveMachine.Test(bigMachine);
        // IntermittentMachine.Test(bigMachine);
        // ContinuousMachine.Test(bigMachine);

        // Other test code.
        DerivedMachine.Test2(bigMachine);
        // GenericMachine<int>.Test(bigMachine);
        // LoopMachine.Test(bigMachine);
        // LoopMachine2.Test(bigMachine);
        // SingleMachine.Test(bigMachine);
        // ServiceProviderMachine.Test(bigMachine);

        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.

        // Save
        var data = bigMachine.Serialize();
        using (var fs = new FileStream("app.data", FileMode.Create))
        {
            fs.Write(data);
        }
    }

    public static async Task Test2()
    {
        var bigMachine = new BigMachine<IdentifierClass>(ThreadCore.Root);
        bigMachine.TryCreate<IdentifierMachine.Interface>(new(1, "A"));
        // bigMachine.TryCreate<GenericMachine<IdentifierClass>.Interface>(new(2, "B"));
        TerminatorMachine<IdentifierClass>.Test(bigMachine, IdentifierClass.Default);

        // var bigMachine = new BigMachine<IdentifierClass2>(ThreadCore.Root);
        // bigMachine.TryCreate<IdentifierMachine2.Interface>(new(1, "A"));
        // TerminatorMachine<IdentifierClass2>.Test(bigMachine, default!);

        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
    }
}
