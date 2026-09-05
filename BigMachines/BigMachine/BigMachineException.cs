// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace BigMachines;

/// <summary>
/// Defines the type of delegate for handling BigMachine exceptions.
/// </summary>
/// <param name="exception">The queued machine exception.</param>
public delegate void ExceptionHandlerDelegate(BigMachineException exception);

/// <summary>
/// Associates an exception with the machine that raised it.
/// </summary>
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

    public override string ToString()
        => $"{this.Machine.ToString()} Exception: {this.Exception.ToString()}";
}

/// <summary>
/// Represents an error caused by circular command invocation.
/// </summary>
public class CircularCommandException : Exception
{
    public CircularCommandException(string message)
        : base(message)
    {
    }
}
