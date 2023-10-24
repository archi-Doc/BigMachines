// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using BigMachines.Redesign;

namespace BigMachines;

public abstract class BigMachineBase
{
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
