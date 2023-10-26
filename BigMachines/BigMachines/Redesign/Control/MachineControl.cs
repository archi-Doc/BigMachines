// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    internal abstract bool RemoveMachine(Machine machine);
}
