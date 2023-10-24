// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace BigMachines.Redesign;

/// <summary>
/// Defines the type of delegate used to handle exceptions.
/// </summary>
/// <param name="exception">Exception.</param>
public delegate void ExceptionHandlerDelegate(BigMachineException exception);

public class BigMachineException
{
    public BigMachineException(Machine machine, Exception exception)
        : base()
    {
        this.Machine = machine;
        this.Exception = exception;
    }

    public Machine Machine { get; }

    public Exception Exception { get; }
}
