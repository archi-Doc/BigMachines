// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;

namespace Advanced;

/*[MachineObject(0x1b431670)]
public partial class IntermittentMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        var m = bigMachine.CreateOrGet<IntermittentMachine.Interface>(0);

        // The machine will run at regular intervals (1 second).
    }

    public IntermittentMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
        this.SetLifespan(TimeSpan.FromSeconds(5)); // The time until the machine automatically terminates.
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"IntermittentMachine: Initial - {this.Count++}");
        if (this.Count > 2)
        {
            this.ChangeState(State.First);
        }

        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected StateResult First(StateParameter parameter)
    {
        Console.WriteLine($"IntermittentMachine: First - {this.Count++}");
        this.SetTimeout(TimeSpan.FromSeconds(0.5)); // Change the timeout of the machine.
        return StateResult.Continue;
    }
}*/
