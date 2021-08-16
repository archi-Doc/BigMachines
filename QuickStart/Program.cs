// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;

namespace QuickStart
{
    [MachineObject(0)] // Annotate MachineObject and set Machine type id (unique number).
    public partial class TestMachine : Machine<int> // Inherit Machine<TIdentifier> class. The type of an identifier is int.
    {
        public TestMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
            this.SetLifespan(TimeSpan.FromSeconds(5)); // Time until the machine automatically terminates.
        }

        public int Count { get; set; }

        [StateMethod(0)] // Annotate StateMethod attribute and set state method id (0 for default state).
        protected StateResult Initial(StateParameter parameter) // The name of method becomes the state name.
        {// This code is inside 'lock (this.SyncMachine) {}'.
            Console.WriteLine($"TestMachine {this.Identifier}: Initial");
            this.ChangeState(TestMachine.State.One); // Change to state One.
            return StateResult.Continue; // Continue (StateResult.Terminate to terminate machine).
        }

        [StateMethod(1)]
        protected StateResult One(StateParameter parameter)
        {
            Console.WriteLine($"TestMachine {this.Identifier}: One - {this.Count++}");
            return StateResult.Continue;
        }

        protected override void OnTerminated()
        {
            Console.WriteLine($"TestMachine {this.Identifier}: Terminated");
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        }
    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var bigMachine = new BigMachine<int>(ThreadCore.Root); // Create BigMachine and set thread core (parent thread).

            var testMachine = bigMachine.TryCreate<TestMachine.Interface>(42); // Machine is created via the interface class and identifier, not the machine class itself.
            if (testMachine != null)
            {
                var currentState = testMachine.GetCurrentState(); // Get current state. You can operate machines using interface class.
            }

            testMachine = bigMachine.TryGet<TestMachine.Interface>(42); // Get the created machine.

            var testGroup = bigMachine.GetGroup<TestMachine.Interface>(); // Group is a collection of machines.
            testMachine = testGroup.TryGet<TestMachine.Interface>(42); // Same as above

            await ThreadCore.Root.WaitForTermination(-1); // Wait for the termination infinitely.
        }
    }
}
