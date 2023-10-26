// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace BigMachines.Redesign;

/// <summary>
/// Contains information of <see cref="MachineGroup{TIdentifier}"/>.
/// </summary>
public class MachineInformation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MachineInfo{TIdentifier}"/> class.
    /// </summary>
    /// <param name="machineType"><see cref="Type"/> of machine.</param>
    /// <param name="typeId">Type id (unique identifier for serialization).</param>
    /// <param name="hasAsync">True if the machine has async method.</param>
    /// <param name="continuous">True if the machine is continuous.</param>
    /// <param name="constructor">Constructor delegate of <see cref="Machine{TIdentifier}"/>.</param>
    /// <param name="groupType"><see cref="Type"/> of machine group (if you want to use customized <see cref="MachineGroup{TIdentifier}"/>).</param>
    public MachineInformation(Type machineType, uint typeId, bool hasAsync, bool continuous, Func<Machine>? constructor, Type? groupType = null)
    {
        this.MachineType = machineType;
        this.TypeId = typeId;
        this.HasAsync = hasAsync;
        this.Continuous = continuous;
        this.Constructor = constructor;
        this.GroupType = groupType;
    }

    /// <summary>
    /// Gets <see cref="Type"/> of machine.
    /// </summary>
    public Type MachineType { get; }

    /// <summary>
    /// Gets Type id (unique identifier for serialization).
    /// </summary>
    public uint TypeId { get; }

    /// <summary>
    /// Gets a value indicating whether or not the machine has async method.
    /// </summary>
    public bool HasAsync { get; }

    /// <summary>
    /// Gets a value indicating whether or not the machine is continuous machine.
    /// </summary>
    public bool Continuous { get; }

    /// <summary>
    /// Gets a constructor delegate of <see cref="Machine{TIdentifier}"/>.
    /// </summary>
    public Func<Machine>? Constructor { get; }

    /// <summary>
    /// Gets <see cref="Type"/> of machine group.
    /// </summary>
    public Type? GroupType { get; }
}
