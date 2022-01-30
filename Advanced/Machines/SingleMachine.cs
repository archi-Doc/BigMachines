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
    [MachineObject(0xe5cff489, Group = typeof(MachineSingle<>))] // Change groups from MachineGroup<> to MachineSingle<>.
    public partial class SingleMachine : Machine<int>
    {
        public static void Test(BigMachine<int> bigMachine)
        {
            bigMachine.CreateOrGet<SingleMachine.Interface>(0);
            bigMachine.CreateOrGet<SingleMachine.Interface>(1); // Only one machine is created since SingleMachine belongs to MachineSingle<> group.
        }

        public SingleMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
        }

        public int Count { get; set; }

        [StateMethod(0)]
        protected StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine($"Single: ({this.Identifier.ToString()}) - {this.Count++}");

            var testGroup = this.BigMachine.GetGroup<TestMachine.Interface>();
            foreach (var x in testGroup.GetIdentifiers())
            {
                var machine = testGroup.TryGet<TestMachine.Interface>(x);
                if (machine != null)
                {
                    var result = machine.CommandAndReceiveAsync<int, int>(TestMachine.Command.GetCount, 0).Result;
                    Console.WriteLine($"Single: TestMachine found {x} - {result}");
                }
            }

            if (this.Count >= 5)
            {
                return StateResult.Terminate;
            }

            return StateResult.Continue;
        }
    }
}
