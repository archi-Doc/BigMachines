// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arc.Threading;
using BigMachines;
using Tinyhand;

namespace ConsoleApp1
{
    [TinyhandObject(UseServiceProvider = true)] // Annotate TinyhandObject attribute to enable serialization (and set UseServiceProvider to true to skip default constructor check).
    [StateMachine(0x6169e4ee)] // Annotate StateMachine and set Machine type id (unique number).
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
        {// StateName + CanEnter() or CanExit() method controls the state transitions.
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

            // ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        }
    }
}
