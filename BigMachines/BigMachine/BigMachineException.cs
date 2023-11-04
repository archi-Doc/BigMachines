// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace BigMachines;

/// <summary>
/// Defines the type of delegate for handling BigMachine exceptions.
/// </summary>
/// <param name="exception">Exception.</param>
public delegate void ExceptionHandlerDelegate(BigMachineException exception);

/// <summary>
/// A class for handling exceptions that occur during BigMachine processing.
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
/// An exception caused by a circular invocation of commands.
/// </summary>
public class CircularCommandException : Exception
{
    public CircularCommandException(string message)
        : base(message)
    {
    }
}
