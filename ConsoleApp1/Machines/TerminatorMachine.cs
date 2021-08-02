// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;

namespace ConsoleApp1
{
    // Single Machine
    [MachineObject(0x48eb1f0f, Group = typeof(MachineSingle<>))] // Change groups from MachineGroup<> to MachineSingle<>.
    public partial class TerminatorMachine : Machine<int>
    {
        public TerminatorMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
        }

        [StateMethod(0)]
        protected StateResult Initial(StateParameter parameter)
        {
            foreach (var x in this.BigMachine.GetGroups())
            {
                if (x.Info.MachineType != typeof(TerminatorMachine) &&
                    x.Count > 0)
                {
                    foreach (var y in x.GetIdentifiers())
                    {
                        if (x.TryGet<ManMachineInterface<int>>(y)?.GetDefaultTimeout() is TimeSpan ts && ts > TimeSpan.Zero)
                        {
                            return StateResult.Continue;
                        }
                    }
                }
            }

            Console.WriteLine("Terminate (no machine)");
            ThreadCore.Root.Terminate();
            return StateResult.Terminate;
        }
    }
}
