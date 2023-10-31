// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;
using BigMachines;

namespace Sandbox;

[MachineObject]
public partial class TerminatorMachine : Machine
{
    public TerminatorMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        /*if (this.BigMachine.Continuous.GetInfo(true).Length > 0)
        {
            return StateResult.Continue;
        }*/

        foreach (var x in this.BigMachine.GetArray())
        {
            if (x.MachineInformation.MachineType != typeof(TerminatorMachine) && x.Count > 0)
            {
                foreach (var y in x.GetArray())
                {
                    if (y.IsActive() == true)
                    {
                        return StateResult.Continue;
                    }
                }
            }
        }

        if (((IBigMachine)this.BigMachine).GetExceptionCount() > 0)
        {// Remaining exceptions.
            return StateResult.Continue;
        }

        Console.WriteLine("Terminate (no machine)");
        ThreadCore.Root.Terminate();
        return StateResult.Terminate;
    }
}
