// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;

namespace ConsoleApp1
{
    // Single Machine
    [MachineObject(0xe5cff489, Group = typeof(MachineSingle<>))] // Change groups from MachineGroup<> to MachineSingle<>.
    public partial class SingleMachine : Machine<int>
    {
        public SingleMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
        }

        public int Count { get; set; }

        [StateMethod(0)]
        protected StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine($"Single ({this.Identifier.ToString()}) - {this.Count++}");
            if (this.Count >= 5)
            {
                return StateResult.Terminate;
            }

            return StateResult.Continue;
        }
    }
}
