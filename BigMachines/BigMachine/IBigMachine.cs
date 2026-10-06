// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace BigMachines;

/// <summary>
/// Defines runtime operations shared by generated big-machine roots.
/// </summary>
public interface IBigMachine
{
    /// <summary>
    /// Gets the timer that processes this root's controls.
    /// </summary>
    public BigMachineBase.BigMachineCore Core { get; }

    /// <summary>
    /// Starts periodic processing and invokes the root's startup hook.
    /// </summary>
    public void Start();

    /// <summary>
    /// Records a command in the current asynchronous call context and detects circular calls.
    /// </summary>
    /// <param name="machineSerial">The machine's serial number.</param>
    /// <param name="commandId">The combined machine and command identifier.</param>
    /// <returns>Zero if the machine is already in the context; otherwise, one.</returns>
    /// <exception cref="CircularCommandException">The command is already in the context.</exception>
    /// <remarks>Generated commands do not currently call this method.</remarks>
    public int CheckCircularCommand(uint machineSerial, ulong commandId);

    /// <summary>
    /// Gets the UTC time of the last timer pass, or the default value before the first pass.
    /// </summary>
    public DateTime LastRunTime { get; }

    /// <summary>
    /// Determines whether any non-excluded machine is active or exceptions remain queued.
    /// </summary>
    /// <param name="excludedMachineType">The registered machine type to exclude, or <see langword="null"/> to include all types.</param>
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
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    public void SetExceptionHandler(MachineExceptionHandler handler);

    /// <summary>
    /// Drains queued exceptions using the current handler on the calling thread.
    /// </summary>
    /// <remarks>Exceptions thrown by the handler propagate to the caller.</remarks>
    public void ProcessExceptions();
}
