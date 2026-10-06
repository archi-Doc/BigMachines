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
    /// <summary>
    /// The name of the execution group used by each root.
    /// </summary>
    public const string ExecutionGroupName = "BigMachine";

    #region FieldAndProperty

    /// <summary>
    /// Gets the group that owns the timer and sequential workers.
    /// </summary>
    public ExecutionGroup ExecutionGroup { get; }

    BigMachineCore IBigMachine.Core => this.core;

    /// <summary>
    /// Gets the cancellation token for this root's timer.
    /// </summary>
    public CancellationToken CancellationToken => this.core.CancellationToken;

    DateTime IBigMachine.LastRunTime => this.lastRun;

    // RecursiveDetectionMode IBigMachine.RecursiveDetectionMode { get; set; }

    private readonly BigMachineCore core;
    private readonly ConcurrentQueue<MachineExceptionInfo> exceptionQueue = new();
    private DateTime lastRun;
    private MachineExceptionHandler exceptionHandler = DefaultExceptionHandler;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="BigMachineBase"/> class.
    /// </summary>
    /// <param name="root">The execution root that owns this instance.</param>
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

    /// <summary>
    /// Gets the controls for internal processing without requiring a public snapshot.
    /// </summary>
    /// <returns>The controls, including the manual control. Callers must not modify the array.</returns>
    protected virtual MachineControl[] GetControlsCore() => this.GetControls();

    /// <summary>
    /// Starts periodic processing and invokes the startup hook.
    /// </summary>
    public void Start()
    {
        this.core.SendSignal(ExecutionSignal.Start);
        this.OnStart();
    }

    bool IBigMachine.HasPendingWork(Type? excludedMachineType)
    {
        foreach (var x in this.GetControlsCore())
        {
            if (x is ManualMachineControl manual)
            {
                if (manual.ContainsActiveMachine(excludedMachineType))
                {
                    return true;
                }
            }
            else if (x.MachineInformation.MachineType != excludedMachineType && x.ContainsActiveMachine())
            {
                return true;
            }
        }

        return !this.exceptionQueue.IsEmpty;
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
            throw new CircularCommandException("Circular commands detected");
        }

        RecursiveChecker.AsyncLocalInstance.Value = newDetection;
        return result;
    }

    /// <summary>
    /// Runs after the timer receives its start signal.
    /// </summary>
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
    /// Adds an exception to this root's queue.
    /// </summary>
    /// <param name="exception">The exception to be queued.</param>
    void IBigMachine.ReportException(MachineExceptionInfo exception)
        => this.exceptionQueue.Enqueue(exception);

    /// <summary>
    /// Sets an exception handler.
    /// </summary>
    /// <param name="handler">The exception handler.</param>
    void IBigMachine.SetExceptionHandler(MachineExceptionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Volatile.Write(ref this.exceptionHandler, handler);
    }

    /// <summary>
    /// Processes queued exceptions using the current exception handler.
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
