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
    [MachineObject(0x928b319e)]
    public partial class GenericMachine<TIdentifier> : Machine<TIdentifier>
        where TIdentifier : notnull
    {
        public static void Test(BigMachine<TIdentifier> bigMachine)
        {
            bigMachine.TryCreate<GenericMachine<TIdentifier>.Interface>(default!);
        }

        public GenericMachine(BigMachine<TIdentifier> bigMachine)
            : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
            this.SetLifespan(TimeSpan.FromSeconds(5));
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
