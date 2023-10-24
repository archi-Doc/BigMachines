// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;

namespace BigMachines.Redesign;

public abstract class BigMachineBase
{
    /// <summary>
    /// Gets or sets a value to specify the operation mode of the loop checker.
    /// </summary>
    public LoopCheckerMode LoopCheckerMode { get; set; } = LoopCheckerMode.EnabledAndThrowException;

    /// <summary>
    /// Gets <see cref="IServiceProvider"/> used to create instances of <see cref="Machine{TIdentifier}"/>.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; }

    private ConcurrentQueue<BigMachineException> exceptionQueue = new();

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
}
