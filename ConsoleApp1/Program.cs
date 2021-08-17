﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using DryIoc;
using Tinyhand;

namespace ConsoleApp1
{
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

            var container = new Container(); // You can use DI container if you want.
            container.RegisterDelegate<BigMachine<int>>(x => new BigMachine<int>(ThreadCore.Root, container), Reuse.Singleton);
            container.Register<SomeService>();
            container.Register<ServiceProviderMachine>(Reuse.Transient);
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

            TerminatorMachine.Test(bigMachine);

            // bigMachine.TryCreate<GenericMachine<int>.Interface>(1);

            // Other test code.
            // GenericMachine<int>.Test(bigMachine);
            // LoopMachine.Test(bigMachine);
            // SingleMachine.Test(bigMachine);
            // ServiceProviderMachine.Test(bigMachine);

            await ThreadCore.Root.WaitForTermination(-1); // Wait for the termination infinitely.

            // Save
            var data = bigMachine.Serialize();
            using (var fs = new FileStream("app.data", FileMode.Create))
            {
                fs.Write(data);
            }

            ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
        }
    }
}
