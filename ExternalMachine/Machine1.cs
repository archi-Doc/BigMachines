// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BigMachines;

namespace ExternalMachine;

[MachineObject]
public partial class Machine1 : Machine
{// A machine without an identifier is derived from the Machine class.
    public Machine1()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
        this.Lifespan = TimeSpan.FromSeconds(3);
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"External machine: {this.Count++}");
        return StateResult.Continue;
    }
}
