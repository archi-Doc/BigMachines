// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using BigMachines.Control;
using static BigMachines.BigMachineBase;

namespace BigMachines;

/// <summary>
///  The top-level abstract class for managing groups of machines.
/// </summary>
public interface IBigMachine
{
    /// <summary>
    /// Gets an instance of <see cref="BigMachineCore"/>.
    /// </summary>
    public BigMachineCore Core { get; }

    public DateTime LastRun { get; }

    public void Start();

    // public MachineControl[] GetControls();

    /// <summary>
    /// Gets the number of exceptions queued.
    /// </summary>
    /// <returns>The number of exceptions queued.</returns>
    public int GetExceptionCount();

    /// <summary>
    /// Add the exception to BigMachine's exception queue.
    /// </summary>
    /// <param name="exception">The exception to be queued.</param>
    public void ReportException(BigMachineException exception);

    /// <summary>
    /// Sets an exception handler.
    /// </summary>
    /// <param name="handler">The exception handler.</param>
    public void SetExceptionHandler(ExceptionHandlerDelegate handler);

    public void ProcessException();
}
