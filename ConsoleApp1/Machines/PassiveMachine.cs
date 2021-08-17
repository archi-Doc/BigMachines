// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;

namespace ConsoleApp1
{
    [MachineObject(0xffd829b4)]
    public partial class PassiveMachine : Machine<int>
    {
        public static void Test(BigMachine<int> bigMachine)
        {
            var m = bigMachine.TryCreate<PassiveMachine.Interface>(0);

            m.Command("message 1"); // Send command.

            m.Run(); // Manually run machine.

            m.ChangeState(State.First); // Change state from State.Initial to State.First
            m.Run(); // Manually run machine.

            m.ChangeState(State.Second); // Change state from State.First to State.Second (denied)
            m.Run(); // Manually run machine.

            m.ChangeState(State.Second); // Change state from State.First to State.Second (approved)
            m.Run(); // Manually run machine.
        }

        public PassiveMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
        }

        public int Count { get; set; }

        [StateMethod(0)]
        protected StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine($"PassiveMachine: Initial - {this.Count++}");
            return StateResult.Continue;
        }

        [StateMethod(1)]
        protected StateResult First(StateParameter parameter)
        {
            Console.WriteLine($"PassiveMachine: First - {this.Count++}");
            return StateResult.Continue;
        }

        protected bool FirstCanExit()
        {// State Name + "CanExit": Determines if it is possible to change from the state.
            return true;
        }

        [StateMethod(2)]
        protected StateResult Second(StateParameter parameter)
        {
            Console.WriteLine($"PassiveMachine: Second - {this.Count++}");
            return StateResult.Continue;
        }

        protected bool SecondCanEnter()
        {// State Name + "CanEnter": Determines if it is possible to change to the state.
            var result = this.Count > 2;
            var message = result ? "Approved" : "Denied";
            Console.WriteLine($"PassiveMachine: {this.GetCurrentState().ToString()} -> {State.Second.ToString()}: {message}");
            return result;
        }

        protected override void ProcessCommand(CommandPost<int>.Command command)
        {
            if (command.Message is string message)
            {
                Console.WriteLine($"PassiveMachine command: {message}");
            }
        }
    }
}
