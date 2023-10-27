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
    /// <param name="machineType"><see cref="Type"/> of the machine.</param>
    /// <param name="controlType"><see cref="Type"/> of the machine control.</param>
    /// <param name="identifierType"><see cref="Type"/> of the identifier.</param>
    /// <param name="continuous">True if the machine is continuous.</param>
    /// <param name="constructor">Constructor delegate of <see cref="Machine"/>.</param>
    public MachineInformation(Type machineType, Type controlType, Type? identifierType, bool continuous, Func<Machine>? constructor)
    {
        this.MachineType = machineType;
        this.ControlType = controlType;
        this.IdentifierType = identifierType;
        this.Continuous = continuous;
        this.Constructor = constructor;
    }

    /// <summary>
    /// Gets <see cref="Type"/> of the machine.
    /// </summary>
    public Type MachineType { get; }

    /// <summary>
    /// Gets <see cref="Type"/> of the machine control.
    /// </summary>
    public Type ControlType { get; }

    /// <summary>
    /// Gets <see cref="Type"/> of the identifier.
    /// </summary>
    public Type? IdentifierType { get; }

    /// <summary>
    /// Gets a value indicating whether or not the machine is continuous machine.
    /// </summary>
    public bool Continuous { get; }

    /// <summary>
    /// Gets a constructor delegate of <see cref="Machine"/>.
    /// </summary>
    public Func<Machine>? Constructor { get; }
}
