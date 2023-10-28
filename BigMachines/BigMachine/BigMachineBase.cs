// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BigMachines;

/// <summary>
///  The top-level abstract class for managing groups of machines.
/// </summary>
public abstract partial class BigMachineBase
{
    public BigMachineBase()
    {
        this.Core = new(this);
    }

    public abstract MachineControl[] GetArray();

    #region Core

    public void Start()
        => this.Core.Start();

    #endregion

    #region FieldAndProperty

    /// <summary>
    /// Gets or sets a value to specify the operation mode of the loop checker.
    /// </summary>
    public LoopCheckerMode LoopCheckerMode { get; set; } = LoopCheckerMode.EnabledAndThrowException;

    /// <summary>
    /// Gets an instance of <see cref="BigMachineCore"/>.
    /// </summary>
    public BigMachineCore Core { get; private set; }

    public DateTime LastRun { get; private set; }

    private ExceptionHandlerDelegate exceptionHandler = DefaultExceptionHandler;
    private ConcurrentQueue<BigMachineException> exceptionQueue = new();

    #endregion

    #region Exception

    /// <summary>
    /// Gets the number of exceptions queued.
    /// </summary>
    /// <returns>The number of exceptions queued.</returns>
    public int GetExceptionCount()
        => this.exceptionQueue.Count;

    /// <summary>
    /// Add the exception to BigMachine's exception queue.
    /// </summary>
    /// <param name="exception">The exception to be queued.</param>
    public void ReportException(BigMachineException exception)
        => this.exceptionQueue.Enqueue(exception);

    /// <summary>
    /// Sets an exception handler.
    /// </summary>
    /// <param name="handler">The exception handler.</param>
    public void SetExceptionHandler(ExceptionHandlerDelegate handler)
        => Volatile.Write(ref this.exceptionHandler, handler);

    /// <summary>
    /// Process queued exceptions using the exception handler.
    /// </summary>
    public void ProcessException()
    {
        while (this.exceptionQueue.TryDequeue(out var exception))
        {
            this.exceptionHandler(exception);
        }
    }

    private static void DefaultExceptionHandler(BigMachineException exception)
        => throw exception.Exception;

    #endregion
}
