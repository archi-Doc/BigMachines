// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Arc.Threading;
using BigMachines.Control;

#pragma warning disable SA1202

namespace BigMachines;

/// <summary>
/// Provides the root runtime for a generated collection of machine controls.
/// </summary>
public abstract partial class BigMachineBase : IBigMachine
{
    public const string ExecutionGroupName = "BigMachine";

    #region FieldAndProperty

    public ExecutionGroup ExecutionGroup { get; }

    BigMachineCore IBigMachine.Core => this.core;

    /// <summary>
    /// Gets <see cref="System.Threading.CancellationToken"/> of the <see cref="BigMachineBase"/>.
    /// </summary>
    public CancellationToken CancellationToken => this.core.CancellationToken;

    DateTime IBigMachine.LastRunTime => this.lastRun;

    // RecursiveDetectionMode IBigMachine.RecursiveDetectionMode { get; set; }

    private readonly BigMachineCore core;
    private readonly ConcurrentQueue<MachineExceptionInfo> exceptionQueue = new();
    private DateTime lastRun;
    private MachineExceptionHandler exceptionHandler = DefaultExceptionHandler;

    #endregion

    public BigMachineBase(ExecutionRoot root)
    {
        this.ExecutionGroup = new(root, false, ExecutionGroupName);
        this.core = new(this.ExecutionGroup, this);
        this.ManualControl.Attach(this);
    }

    /// <summary>
    /// Gets the runtime-only control for manually registered machines.
    /// </summary>
    public ManualMachineControl ManualControl { get; } = new();

    /// <summary>
    /// Returns a snapshot of this root's controls. The controls themselves remain shared.
    /// </summary>
    /// <returns>The current controls, including the manual control.</returns>
    public abstract MachineControl[] GetControls();

    public void Start()
    {
        this.core.SendSignal(ExecutionSignal.Start);
        this.OnStart();
    }

    bool IBigMachine.HasPendingWork(Type? excludedMachineType)
    {
        foreach (var x in this.GetControls())
        {
            if (x.ContainsActiveMachine())
            {
                if (x.MachineInformation.MachineType != excludedMachineType)
                {
                    return true;
                }
            }
        }

        if (((IBigMachine)this).GetExceptionCount() > 0)
        {// Remaining exceptions.
            return true;
        }

        return false;
    }

    int IBigMachine.CheckCircularCommand(uint machineSerial, ulong commandId)
    {
        /*if (((IBigMachine)this).RecursiveDetectionMode == RecursiveDetectionMode.Disabled)
        {
            return -1;
        }*/

        var detection = RecursiveChecker.AsyncLocalInstance.Value;
        var result = detection.TryAdd(machineSerial, commandId, out var newDetection); // -1: Id collision, 0: Machine collision, 1: No collision
        if (result < 0)
        {
            // this.exceptionQueue.Enqueue(new MachineExceptionInfo(default!, new CircularCommandException($"Circular commands detected")));
            throw new CircularCommandException($"Circular commands detected");
        }

        RecursiveChecker.AsyncLocalInstance.Value = newDetection;
        return result;
    }

    protected virtual void OnStart()
    {
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
    void IBigMachine.ReportException(MachineExceptionInfo exception)
        => this.exceptionQueue.Enqueue(exception);

    /// <summary>
    /// Sets an exception handler.
    /// </summary>
    /// <param name="handler">The exception handler.</param>
    void IBigMachine.SetExceptionHandler(MachineExceptionHandler handler)
        => Volatile.Write(ref this.exceptionHandler, handler);

    /// <summary>
    /// Process queued exceptions using the exception handler.
    /// </summary>
    void IBigMachine.ProcessExceptions()
    {
        while (this.exceptionQueue.TryDequeue(out var exception))
        {
            Volatile.Read(ref this.exceptionHandler)(exception);
        }
    }

    private static void DefaultExceptionHandler(MachineExceptionInfo exception)
    {// throw exception.Exception;
        Console.WriteLine(exception.ToString());
    }

    #endregion
}
