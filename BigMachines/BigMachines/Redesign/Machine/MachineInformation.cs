// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace BigMachines.Redesign;

/// <summary>
/// Contains information of <see cref="MachineGroup{TIdentifier}"/>.
/// </summary>
public class MachineInformation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MachineInformation"/> class.
    /// </summary>
    /// <param name="machineType"><see cref="Type"/> of machine.</param>
    /// <param name="continuous">True if the machine is continuous.</param>
    /// <param name="constructor">Constructor delegate of <see cref="Machine{TIdentifier}"/>.</param>
    public MachineInformation(Type machineType, bool continuous, Func<Machine>? constructor, Func<object, object>? commandAllConstructor)
    {
        this.MachineType = machineType;
        this.Continuous = continuous;
        this.Constructor = constructor;
        this.CommandAllConstructor = commandAllConstructor;
    }

    /// <summary>
    /// Gets <see cref="Type"/> of machine.
    /// </summary>
    public Type MachineType { get; }

    /// <summary>
    /// Gets a value indicating whether or not the machine is continuous machine.
    /// </summary>
    public bool Continuous { get; }

    /// <summary>
    /// Gets a constructor delegate of <see cref="Machine{TIdentifier}"/>.
    /// </summary>
    public Func<Machine>? Constructor { get; }

    public Func<object, object>? CommandAllConstructor { get; }
}
