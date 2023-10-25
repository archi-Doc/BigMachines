// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading.Tasks;

namespace BigMachines.Redesign;

/// <summary>
/// Represents the abstract class for managing machines.
/// </summary>
public abstract class MachineControl
{
    public MachineControl(BigMachineBase bigMachine)
    {
        this.BigMachine = bigMachine;
    }

    /// <summary>
    /// Gets a BigMachine instance.
    /// </summary>
    public BigMachineBase BigMachine { get; }

    internal abstract bool RemoveMachine(Machine machine);
}
