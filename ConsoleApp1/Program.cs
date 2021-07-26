﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using DryIoc;
using Tinyhand;

namespace ConsoleApp1
{
    [TinyhandObject(UseServiceProvider = true)] // Annotate TinyhandObject attribute to enable serialization (and set UseServiceProvider to true to skip default constructor check).
    [StateMachine(0)] // Annotate StateMachine and set Machine type id (unique number).
    public partial class TestMachine : Machine<int> // Inherit Machine<TIdentifier> class.
    {
        public TestMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            this.IsSerializable = true; // Set true to serialize the machine
            this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine processing.
            this.SetLifespan(TimeSpan.FromSeconds(10)); // Time until the machine automatically terminates.
        }

        [Key(10)]
        public int Count { get; set; }

        [StateMethod(0)] // Annotate StateMethod attribute and set state method id (0 for default state).
        protected StateResult Initial(StateParameter parameter)
        {// Inside lock (machine) statement.
            Console.WriteLine($"TestMachine {this.Identifier}: Initial");

            this.ChangeState(TestMachine.State.One); // Change to state One (The method name becomes the state name).
            return StateResult.Continue;
        }

        [StateMethod(1)]
        protected StateResult One(StateParameter parameter)
        {
            Console.WriteLine($"TestMachine {this.Identifier}: One - {this.Count}");

            this.ChangeState(State.Two); // Try to change to state Two.

            return StateResult.Continue;
        }

        protected bool TwoCanEnter()
        {// StateName + CanEnter() or CanExit() can control the transition of the state.
            var result = this.Count++ > 2;
            if (result)
            {
                Console.WriteLine("One -> Two approved.");
            }
            else
            {
                Console.WriteLine("One -> Two denied.");

            }

            return result;
        }

        [StateMethod(2)]
        protected StateResult Two(StateParameter parameter)
        {
            Console.WriteLine($"TestMachine {this.Identifier}: Two - {this.Count++}");
            this.SetTimeout(TimeSpan.FromSeconds(0.5)); // Change the time until the next run.

            if (this.Count > 10)
            {// Terminate the machine.
                return StateResult.Terminate;
            }

            return StateResult.Continue; // Continue
        }

        protected override void OnTerminated()
        {
            Console.WriteLine($"TestMachine {this.Identifier}: Terminated");

            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        }
    }

    class Program
    {
        static async Task Main(string[] args)
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

            /*var container = new Container(); // You can use DI container if you want.
            container.RegisterDelegate<BigMachine<int>>(x => new BigMachine<int>(ThreadCore.Root, container), Reuse.Singleton);
            container.Register<TestMachine>(Reuse.Transient);
            var bigMachine = container.Resolve<BigMachine<int>>();*/

            var bigMachine = new BigMachine<int>(ThreadCore.Root);

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

            var testMachine = bigMachine.TryCreate<TestMachine.Interface>(3); // Machine is created via the interface class and identifier, not the machine class itself.
            if (testMachine != null)
            {
                var currentState = testMachine.GetCurrentState(); // Get current state. You can operate machines using the interface class.
            }

            testMachine = bigMachine.TryGet<TestMachine.Interface>(3); // Get the created machine.

            var testGroup = bigMachine.GetGroup<TestMachine.Interface>(); // Group is a collection of machines.
            testMachine = testGroup.TryGet<TestMachine.Interface>(3); // Same as above

            await ThreadCore.Root.WaitForTermination(-1); // Wait for the termination infinitely.

            // Save
            var data = bigMachine.Serialize();
            using (var fs = new FileStream("app.data", FileMode.Create))
            {
                fs.Write(data);
            }

            bigMachine.Deserialize(data);

            ThreadCore.Root.TerminationEvent.Set(); // The termination process is complete (#1).
        }
    }
}