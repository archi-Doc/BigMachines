// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Arc.Threading;

namespace BigMachines;

/// <summary>
/// Defines runtime operations shared by generated big-machine roots.
/// </summary>
public interface IBigMachine
{
    /// <summary>
    /// Gets an instance of <see cref="BigMachineBase.BigMachineCore"/>.
    /// </summary>
    public BigMachineBase.BigMachineCore Core { get; }

    public void Start();

    public int CheckCircularCommand(uint machineSerial, ulong commandId);

    /// <summary>
    /// Gets <see cref="DateTime"/> when the BigMachine was last executed.
    /// </summary>
    public DateTime LastRunTime { get; }

    /// <summary>
    /// Determines whether any non-excluded machine is active or exceptions remain queued.
    /// </summary>
    /// <param name="excludedMachineType">The type of the machine to be excluded.</param>
    /// <returns>Whether execution or exception processing remains pending.</returns>
    public bool HasPendingWork(Type? excludedMachineType = null);

    /// <summary>
    /// Gets the number of exceptions queued.
    /// </summary>
    /// <returns>The number of exceptions queued.</returns>
    public int GetExceptionCount();

    /// <summary>
    /// Adds an exception to the root's queue.
    /// </summary>
    /// <param name="exception">The exception to be queued.</param>
    public void ReportException(MachineExceptionInfo exception);

    /// <summary>
    /// Sets an exception handler.
    /// </summary>
    /// <param name="handler">The exception handler.</param>
    public void SetExceptionHandler(MachineExceptionHandler handler);

    /// <summary>
    /// Processes the queued exceptions.
    /// </summary>
    public void ProcessExceptions();
}
