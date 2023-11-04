﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace BigMachines;

public record MachineInformation(Type MachineType, Func<Machine>? Constructor, bool Serializable, Type? IdentifierType, int NumberOfTasks)
{
}
