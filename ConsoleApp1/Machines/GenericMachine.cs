// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;

namespace ConsoleApp1
{
    // Generic version.
    [StateMachine(0x928b319e)]
    public partial class GenericMachine<TIdentifier> : Machine<TIdentifier>
        where TIdentifier : notnull
    {
        public GenericMachine(BigMachine<TIdentifier> bigMachine)
            : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
        }

        public int Count { get; set; }

        [StateMethod(0)]
        protected StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine($"Generic ({this.Identifier.ToString()}) - {this.Count++}");
            return StateResult.Continue;
        }
    }
}
