// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;

namespace Advanced
{
    [MachineObject(0xf761dd51)]
    public partial class DerivedMachine : IntermittentMachine
    {
        public static void Test2(BigMachine<int> bigMachine)
        {
            var m = bigMachine.CreateOrGet<DerivedMachine.Interface>(0);
        }

        public DerivedMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
            this.SetLifespan(TimeSpan.FromSeconds(5)); // Time until the machine automatically terminates.
        }

        // [StateMethod(0)]
        protected new StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine($"DerivedMachine: Initial - {this.Count++}");
            if (this.Count > 2)
            {
                this.ChangeState(State.First);
            }

            return StateResult.Continue;
        }
    }

    public class EmptyMachineBase : Machine<int>
    {
        public EmptyMachineBase(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
        }

        public int Count { get; set; }

        public string Text { get; set; } = "EmptyMachine";
    }

    [MachineObject(0x609284ed)]
    public partial class DerivedMachine2 : EmptyMachineBase
    {
        public static void Test(BigMachine<int> bigMachine)
        {
            var m = bigMachine.CreateOrGet<DerivedMachine2.Interface>(0);
        }

        public DerivedMachine2(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
            this.SetLifespan(TimeSpan.FromSeconds(3)); // Time until the machine automatically terminates.
        }

        [StateMethod(0)]
        protected StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine($"{this.Text} - DerivedMachine2: Initial - {this.Count++}");

            return StateResult.Continue;
        }
    }
}
