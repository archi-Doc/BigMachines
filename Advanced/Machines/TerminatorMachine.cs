// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;
using BigMachines;

namespace Advanced;

/*[MachineObject(0x48eb1f0f, Group = typeof(SingleGroup<>))] // Change groups from MachineGroup<> to SingleGroup<>.
public partial class TerminatorMachine<TIdentifier> : Machine<TIdentifier>
    where TIdentifier : notnull
{
    public static void Start(BigMachine<TIdentifier> bigMachine, TIdentifier identifier)
    {
        bigMachine.CreateOrGet<TerminatorMachine<TIdentifier>.Interface>(identifier);
    }

    public TerminatorMachine(BigMachine<TIdentifier> bigMachine)
        : base(bigMachine)
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        if (this.BigMachine.Continuous.GetInfo(true).Length > 0)
        {
            return StateResult.Continue;
        }

        foreach (var x in this.BigMachine.GetGroups())
        {
            if (x.Info.MachineType != typeof(TerminatorMachine<TIdentifier>) &&
                x.Count > 0)
            {
                foreach (var y in x.GetIdentifiers())
                {
                    if (x.TryGet<ManMachineInterface<TIdentifier>>(y)?.IsActive() == true)
                    {
                        return StateResult.Continue;
                    }
                }
            }
        }

        if (this.BigMachine.GetExceptionCount() > 0)
        {// Remaining exceptions.
            return StateResult.Continue;
        }

        Console.WriteLine("Terminate (no machine)");
        ThreadCore.Root.Terminate();
        return StateResult.Terminate;
    }
}*/
