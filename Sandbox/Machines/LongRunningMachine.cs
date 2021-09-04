// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;

namespace Sandbox
{
    // Loop Machine
    [MachineObject(0xbadbe735)]
    public partial class LongRunningMachine : Machine<int>
    {
        public static void Test(BigMachine<int> bigMachine)
        {
            var loopMachine = bigMachine.TryCreate<LongRunningMachine.Interface>(0);
        }

        public LongRunningMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            // this.DefaultTimeout = TimeSpan.FromSeconds(1);
            // this.SetLifespan(TimeSpan.FromSeconds(3));
        }

        [StateMethod(0)]
        protected StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine("Long running machine enter");

            for (var n = 0; n < 30; n++)
            {
                this.BigMachine.Core.Wait(100, 10);
            }

            Console.WriteLine("Long running machine exit");

            return StateResult.Continue;
        }
    }
}
