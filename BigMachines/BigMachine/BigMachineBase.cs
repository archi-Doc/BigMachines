// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Arc.Threading;
using BigMachines.Control;

namespace BigMachines;

/// <summary>
///  The top-level abstract class for managing groups of machines.
/// </summary>
public abstract partial class BigMachineBase : IBigMachine
{
    public BigMachineBase()
    {
        this.core = new(this);
    }

    public abstract MachineControl[] GetArray();

    #region Core

    public bool Start(ThreadCoreBase? parent)
    {
        if (this.started)
        {
            return false;
        }

        this.started = true;
        var core = ((IBigMachine)this).Core;
        core.ChangeParent(parent);
        core.Start();

        return true;
    }

    #endregion

    #region FieldAndProperty

    BigMachineCore IBigMachine.Core => this.core;

    DateTime IBigMachine.LastRun => this.lastRun;

    // RecursiveDetectionMode IBigMachine.RecursiveDetectionMode { get; set; }

    private bool started;
    private BigMachineCore core;
    private DateTime lastRun;
    private ExceptionHandlerDelegate exceptionHandler = DefaultExceptionHandler;
    private ConcurrentQueue<BigMachineException> exceptionQueue = new();

    #endregion

    int IBigMachine.CheckRecursive(uint machineSerial, ulong id)
    {
        /*if (((IBigMachine)this).RecursiveDetectionMode == RecursiveDetectionMode.Disabled)
        {
            return -1;
        }*/

        var detection = RecursiveChecker.AsyncLocalInstance.Value;
        var result = detection.TryAdd(machineSerial, id, out var newDetection); // -1: Id collision, 0: Machine collision, 1: No collision
        if (result < 0)
        {
            // this.exceptionQueue.Enqueue(new BigMachineException(default!, new CircularCommandException($"Circular commands detected")));
            throw new CircularCommandException($"Circular commands detected");
        }

        RecursiveChecker.AsyncLocalInstance.Value = newDetection;
        return result;
    }

    #region Exception

    /// <summary>
    /// Gets the number of exceptions queued.
    /// </summary>
    /// <returns>The number of exceptions queued.</returns>
    int IBigMachine.GetExceptionCount()
        => this.exceptionQueue.Count;

    /// <summary>
    /// Add the exception to BigMachine's exception queue.
    /// </summary>
    /// <param name="exception">The exception to be queued.</param>
    void IBigMachine.ReportException(BigMachineException exception)
        => this.exceptionQueue.Enqueue(exception);

    /// <summary>
    /// Sets an exception handler.
    /// </summary>
    /// <param name="handler">The exception handler.</param>
    void IBigMachine.SetExceptionHandler(ExceptionHandlerDelegate handler)
        => Volatile.Write(ref this.exceptionHandler, handler);

    /// <summary>
    /// Process queued exceptions using the exception handler.
    /// </summary>
    void IBigMachine.ProcessException()
    {
        while (this.exceptionQueue.TryDequeue(out var exception))
        {
            this.exceptionHandler(exception);
        }
    }

    private static void DefaultExceptionHandler(BigMachineException exception)
    {// throw exception.Exception;
        Console.WriteLine(exception.ToString());
    }

    #endregion
}
