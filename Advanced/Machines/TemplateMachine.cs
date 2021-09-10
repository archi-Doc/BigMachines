// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;

namespace Advanced
{
    // Template machine
    [MachineObject(0x6c51e7cf)]
    public partial class TemplateMachine : Machine<int>
    {
        public static void Test(BigMachine<int> bigMachine)
        {
        }

        public TemplateMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
        }

        public int Count { get; set; }

        [StateMethod(0)]
        protected StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine($"Template ({this.Identifier.ToString()}) - {this.Count++}");
            if (this.Count > 5)
            {
                return StateResult.Terminate;
            }

            return StateResult.Continue;
        }
    }
}
