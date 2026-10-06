// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace BigMachines;

/// <summary>
/// Defines the type of delegate for handling BigMachine exceptions.
/// </summary>
/// <param name="exception">The queued machine exception.</param>
public delegate void MachineExceptionHandler(MachineExceptionInfo exception);

/// <summary>
/// Associates an exception with the machine that raised it.
/// </summary>
public class MachineExceptionInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MachineExceptionInfo"/> class.
    /// </summary>
    /// <param name="machine">The machine that raised the exception.</param>
    /// <param name="exception">The exception to report.</param>
    public MachineExceptionInfo(Machine machine, Exception exception)
    {
        this.Machine = machine;
        this.Exception = exception;
    }

    /// <summary>
    /// Gets the machine that raised the exception.
    /// </summary>
    public Machine Machine { get; }

    /// <summary>
    /// Gets the reported exception.
    /// </summary>
    public Exception Exception { get; }

    /// <inheritdoc/>
    public override string ToString()
        => $"{this.Machine} Exception: {this.Exception}";
}

/// <summary>
/// Represents an error caused by circular command invocation.
/// </summary>
public class CircularCommandException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CircularCommandException"/> class.
    /// </summary>
    /// <param name="message">The circular-call description.</param>
    public CircularCommandException(string message)
        : base(message)
    {
    }
}
