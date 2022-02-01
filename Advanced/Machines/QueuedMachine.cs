// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;

namespace Advanced
{
    // Single Machine
    [MachineObject(0xe5cff489, Group = typeof(MachineQueued<>))] // Change groups from MachineGroup<> to MachineQueued<>.
    public partial class QueuedMachine : Machine<int>
    {
        public static void Test(BigMachine<int> bigMachine)
        {
            var queuedMachine = bigMachine.CreateOrGet<QueuedMachine.Interface>(0);
            bigMachine.CreateOrGet<QueuedMachine.Interface>(2);
            bigMachine.CreateOrGet<QueuedMachine.Interface>(1);
            bigMachine.CreateNew<QueuedMachine.Interface>(0);

            var group = queuedMachine.Group;
            Console.WriteLine(string.Join(",", group.GetIdentifiers().Select(a => a.ToString())));
        }

        public QueuedMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
        }

        public int Count { get; set; }

        [StateMethod(0)]
        protected async Task<StateResult> Initial(StateParameter parameter)
        {
            Console.WriteLine($"Queued machine: ({this.Identifier.ToString()}) - {this.Count++}");
            if (this.Count == 3)
            {
                return StateResult.Terminate;
            }

            await Task.Delay(1000);
            return StateResult.Continue;
        }
    }
}
