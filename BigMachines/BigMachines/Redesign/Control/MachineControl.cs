﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
    /// Gets or sets a <see cref="BigMachineBase"/> instance.
    /// </summary>
    [IgnoreMember]
    public BigMachineBase? BigMachine { get; protected set; }

    public abstract Machine.ManMachineInterface[] GetArray();

    internal abstract bool RemoveMachine(Machine machine);
}
