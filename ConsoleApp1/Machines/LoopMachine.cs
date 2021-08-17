// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;

namespace ConsoleApp1
{
    // Loop Machine
    [MachineObject(0xb7196ebc)] // Change groups from MachineGroup<> to MachineSingle<>.
    public partial class LoopMachine : Machine<int>
    {
        public static void Test(BigMachine<int> bigMachine)
        {
            var loopMachine = bigMachine.TryCreate<LoopMachine.Interface>(0);
            if (loopMachine != null)
            {
                loopMachine.Command(1);
                // loopMachine.Command("1");
            }
        }

        public LoopMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
        }

        protected override void ProcessCommand(CommandPost<int>.Command command)
        {
            if (command.Message is int n)
            {// LoopMachine
                this.BigMachine.TryGet<Interface>(this.Identifier)?.Command(0);
            }
            else if (command.Message is string st)
            {// LoopMachine -> TestMachine
                this.BigMachine.TryGet<TestMachine.Interface>(3)?.Command(st);
            }
        }
    }
}
