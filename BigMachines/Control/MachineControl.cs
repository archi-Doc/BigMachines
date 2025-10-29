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
    /// Determines whether this control contains any active machines.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if at least one active machine is present; otherwise, <see langword="false"/>.
    /// </returns>
    public abstract bool ContainsActiveMachine();

    /// <summary>
    /// Retrieves all machines currently managed by this control.
    /// </summary>
    /// <returns>An array of <see cref="Machine"/> instances managed by this control.</returns>
    internal abstract Machine[] GetMachines();

    /// <summary>
    /// Removes the specified machine from this control.
    /// </summary>
    /// <param name="machine">The <see cref="Machine"/> instance to remove.</param>
    /// <returns><see langword="true"/> if the machine was successfully removed; otherwise, <see langword="false"/>.</returns>
    internal abstract bool RemoveMachine(Machine machine);

    /// <summary>
    /// Processes all machines managed by this control using the specified runner.
    /// </summary>
    /// <param name="runner">The <see cref="MachineRunner"/> instance used to execute machine processing.</param>
    internal abstract void Process(MachineRunner runner);
}
