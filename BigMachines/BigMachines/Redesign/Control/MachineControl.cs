// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;

namespace BigMachines.Redesign;

/// <summary>
/// Represents the abstract class for managing machines.
/// </summary>
public abstract class MachineControl
{
    public MachineControl()
    {
    }

    /// <summary>
    /// Gets or sets a BigMachine instance.
    /// </summary>
    [IgnoreMember]
    public BigMachineBase? BigMachine { get; protected set; }

    protected MachineInformation? machineInformation;

    internal abstract bool RemoveMachine(Machine machine);

    internal Machine CreateMachine()
    {
        if (this.BigMachine is null)
        {
            throw new InvalidOperationException("Call Prepare() function to specify a valid BigMachine object.");
        }
        else if (this.machineInformation is null)
        {
            throw new InvalidOperationException("Call Prepare() function to specify a valid machine information.");
        }

        var machine = this.BigMachine.CreateMachine(this.machineInformation);
        if (machine is null)
        {
            throw new InvalidOperationException("Unable to create an instance of the machine.");
        }

        return machine;
    }
}
