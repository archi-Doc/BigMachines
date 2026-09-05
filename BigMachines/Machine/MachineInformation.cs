// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace BigMachines;

/// <summary>
/// Describes a machine type registered by the source generator.
/// </summary>
/// <param name="MachineType">The registered machine type.</param>
/// <param name="Constructor">The generated constructor, or <see langword="null"/> when a service provider creates the machine.</param>
/// <param name="Serializable">Whether the machine participates in Tinyhand serialization.</param>
/// <param name="IdentifierType">The identifier type, or <see langword="null"/> for a single machine.</param>
/// <param name="NumberOfTasks">The number of dedicated sequential workers.</param>
public record MachineInformation(Type MachineType, Func<Machine>? Constructor, bool Serializable, Type? IdentifierType, int NumberOfTasks)
{
    public static readonly MachineInformation Default = new(typeof(Machine), null, false, null, 0);
}
