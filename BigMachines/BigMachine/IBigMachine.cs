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

    /// <summary>
    /// Initiate BigMachine to process active machines.
    /// </summary>
    /// <param name="parent">Specify the parent for the BigMachine's processing tasks. If <see langword="null"/> is specified, it becomes an independent task.</param>
    /// <returns><see langword="true"/>: Success; otherwise <see langword="false"/>.</returns>
    public bool Start(ThreadCoreBase? parent);

    public int CheckRecursive(uint machineSerial, ulong id);

    /// <summary>
    /// Gets <see cref="DateTime"/> when the BigMachine was last executed.
    /// </summary>
    public DateTime LastRun { get; }

    /// <summary>
    /// Check if there are any machines currently active.
    /// </summary>
    /// <returns><see langword="true"/>; Active machines are present.</returns>
    public bool CheckActiveMachine(Type? machineTypeToBeExcluded = null);

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
