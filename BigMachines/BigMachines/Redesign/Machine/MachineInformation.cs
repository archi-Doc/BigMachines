// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace BigMachines.Redesign;

public record MachineInformation(int Id, Type MachineType, Type ControlType)
{
    public Func<Machine>? Constructor { get; init; }

    public Type? IdentifierType { get; init; }

    public bool Continuous { get; init; }

    public bool Serializable { get; set; }
}
