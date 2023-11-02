// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;

namespace BigMachines;

/// <summary>
///  The top-level abstract class for managing groups of machines.
/// </summary>
public interface IBigMachine
{
    /// <summary>
    /// Gets an instance of <see cref="BigMachineBase.BigMachineCore"/>.
    /// </summary>
    public BigMachineBase.BigMachineCore Core { get; }

    public void Start(ThreadCoreBase? parent);

    /// <summary>
    /// Gets or sets a value to specify the operation mode of the detection of recursive calls.
    /// </summary>
    public RecursiveDetectionMode RecursiveDetectionMode { get; set; }

    public int CheckRecursive(uint machineSerial, ulong id);

    /// <summary>
    /// Gets <see cref="DateTime"/> when the BigMachine was last executed.
    /// </summary>
    public DateTime LastRun { get; }

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

    /// <summary>
    /// Processes the queued exceptions.
    /// </summary>
    public void ProcessException();
}
