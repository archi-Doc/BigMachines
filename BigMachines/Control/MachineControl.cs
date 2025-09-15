// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;

namespace BigMachines.Control;

/// <summary>
/// Represents the abstract class for managing machines.
/// </summary>
public abstract class MachineControl
{
    public MachineControl()
    {
    }

    /// <summary>
    /// Gets or sets a <see cref="BigMachineBase"/> instance.
    /// </summary>
    [IgnoreMember]
    public BigMachineBase BigMachine { get; protected set; } = default!;

    /// <summary>
    /// Gets the number of machines.
    /// </summary>
    public abstract int Count { get; }

    /// <summary>
    /// Gets a <see cref="MachineInformation"/> instance.
    /// </summary>
    [IgnoreMember]
    public abstract MachineInformation MachineInformation { get; }

    public abstract Machine.ManMachineInterface[] GetArray();

    /// <summary>
    /// Check if there are any machines currently active.
    /// </summary>
    /// <returns><see langword="true"/>; Active machines are present.</returns>
    public abstract bool ContainsActiveMachine();

    internal abstract Machine[] GetMachines();

    internal abstract bool RemoveMachine(Machine machine);

    internal abstract void Process(MachineRunner runner);
}
