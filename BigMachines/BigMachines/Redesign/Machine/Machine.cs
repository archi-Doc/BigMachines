// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using Microsoft.VisualBasic;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1202
#pragma warning disable SA1401 // Fields should be private

namespace BigMachines.Redesign;

/// <summary>
/// Represents an abstract class that serves as the base for the actual machine class.
/// </summary>
[TinyhandObject(ReservedKeys = ReservedKeyNumber)]
public abstract partial class Machine
{
    internal const int ReservedKeyNumber = 8;
    private static uint serialNumber;

    internal Machine()
    {
        this.machineNumber = Interlocked.Increment(ref serialNumber);
    }

    internal void Prepare(MachineControl control)
    {
        this.Control = control;
        if (this is IStructualObject obj &&
            this.Control is IStructualObject parent)
        {
            obj.SetParent(parent);
        }

        if (this.DefaultTimeout != TimeSpan.Zero && this.timeToStart == long.MaxValue)
        {
            this.timeToStart = 0;
        }

        /*if (information.Continuous)
        {// tempcode
            // this.Continuous.AddMachine(machine);
        }*/
    }

    #region Keys

    // Machine<TIdentifier>
    // [Key(0)]
    // public TIdentifier Identifier { get; protected set; }

    /// <summary>
    /// Gets or sets the current state of this machine.
    /// </summary>
    [Key(1)]
    protected int machineState;

    [IgnoreMember]
    protected int MachineState
    {
        get => this.machineState;
        set
        {
            if (this.machineState == value)
            {
                return;
            }

            if (this is IStructualObject structualObject &&
                structualObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(1);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournal(writer);
            }

            this.machineState = value;
        }
    }

    /// <summary>
    /// The time until the machine starts.
    /// </summary>
    [Key(2)]
    protected long timeToStart = long.MaxValue; // TimeSpan.Ticks (for interlocked)

    [IgnoreMember]
    protected long TimeToStart
    {
        get => this.timeToStart;
        set
        {
            if (this.timeToStart == value)
            {
                return;
            }

            if (this is IStructualObject structualObject &&
                structualObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(2);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournal(writer);
            }

            this.timeToStart = value;
        }
    }

    /// <summary>
    /// The last <see cref="DateTime"/> when this machine ran.
    /// </summary>
    [Key(3)]
    protected DateTime lastRunTime;

    [IgnoreMember]
    protected DateTime LastRunTime
    {
        get => this.lastRunTime;
        set
        {
            if (this.lastRunTime == value)
            {
                return;
            }

            if (this is IStructualObject structualObject &&
                structualObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(3);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournal(writer);
            }

            this.lastRunTime = value;
        }
    }

    /// <summary>
    /// The next scheduled <see cref="DateTime"/> for this machine to run.
    /// </summary>
    [Key(4)]
    protected DateTime nextRunTime;

    [IgnoreMember]
    protected DateTime NextRunTime
    {
        get => this.nextRunTime;
        set
        {
            if (this.nextRunTime == value)
            {
                return;
            }

            if (this is IStructualObject structualObject &&
                structualObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(4);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournal(writer);
            }

            this.nextRunTime = value;
        }
    }

    /// <summary>
    /// The remaining lifespan of the machine.<br/>
    /// When it reaches 0, the machine will terminate.
    /// </summary>
    [Key(5)]
    protected long lifespan = long.MaxValue; // TimeSpan.Ticks (for interlocked)

    [IgnoreMember]
    protected long Lifespan
    {
        get => this.lifespan;
        set
        {
            if (this.lifespan == value)
            {
                return;
            }

            if (this is IStructualObject structualObject &&
                structualObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(5);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournal(writer);
            }

            this.lifespan = value;
        }
    }

    /// <summary>
    /// Gets or sets the time for the machine to shut down automatically.
    /// </summary>
    [Key(6)]
    protected DateTime terminationTime = DateTime.MaxValue;

    [IgnoreMember]
    protected DateTime TerminationTime
    {
        get => this.terminationTime;
        set
        {
            if (this.terminationTime == value)
            {
                return;
            }

            if (this is IStructualObject structualObject &&
                structualObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(6);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournal(writer);
            }

            this.terminationTime = value;
        }
    }

    #endregion

    #region FieldAndProperty

    /// <summary>
    /// Gets an instance of <see cref="MachineControl"/>.
    /// </summary>
    public MachineControl? Control { get; private set; }

    protected readonly SemaphoreLock Semaphore = new();

    /// <summary>
    /// Gets or sets the default time interval at which the machine will run.<br/>
    /// <see cref="TimeSpan.Zero"/>: No interval execution.<br/>
    /// This property is NOT serialization target.
    /// </summary>
    protected readonly TimeSpan DefaultTimeout;

    [IgnoreMember]
    protected OperationalFlag operationalState;

    [IgnoreMember]
    protected object? interfaceInstance;

    internal abstract object InterfaceInstance { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the machine is going to re-run.
    /// </summary>
    [IgnoreMember]
    private bool requestRerun;

    /// <summary>
    /// Get the serial (unique) number of this machine.
    /// </summary>
    [IgnoreMember]
    private uint machineNumber;

    #endregion

    internal void StartIfDefaultTimeoutIsSet()
    {
        if (this.DefaultTimeout != TimeSpan.Zero && this.TimeToStart == long.MaxValue)
        {
            this.timeToStart = 0;
        }
    }

    /// <summary>
    /// Run the machine.<br/>
    /// This code is inside 'lock (this.Machine)' statement.
    /// </summary>
    /// <param name="runType">A trigger of the machine running.</param>
    /// <param name="now">Current time.</param>
    /// <returns>true: The machine is terminated.</returns>
    private async Task<StateResult> RunMachine(RunType runType, DateTime now)
    {// Called: Machine.DistributeCommand(), BigMachine.MainLoop()
        if (this.operationalState.HasFlag(OperationalFlag.Running))
        {// Machine is running
            return StateResult.Continue;
        }

        this.operationalState |= OperationalFlag.Running;
RerunLoop:
        StateResult result;
        this.requestRerun = false;

        try
        {
            result = await this.InternalRun(new(runType)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result = StateResult.Terminate;
            this.Control?.BigMachine?.ReportException(new(this, ex));
        }

        if (result == StateResult.Terminate)
        {
            // this.LastRun = now;
            this.operationalState &= ~OperationalFlag.Running;
            return result;
        }
        else if (this.requestRerun)
        {
            goto RerunLoop;
        }

        this.LastRunTime = now;
        this.operationalState &= OperationalFlag.Running;
        return result;
    }

    private Task TerminateAndRemoveFromControlAsync()
        => Task.Run(() => this.TerminateAndRemoveFromControl());

    private async Task TerminateAndRemoveFromControl()
    {
        var terminated = false;
        await this.Semaphore.EnterAsync().ConfigureAwait(false);
        try
        {
            if (this.operationalState.HasFlag(OperationalFlag.Terminated))
            {
                terminated = true;
                this.OnTerminated();
            }
        }
        finally
        {
            this.Semaphore.Exit();
        }

        if (terminated)
        {
            this.RemoveFromControl();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveFromControl()
    {
        this.Control?.RemoveMachine(this);
        /*if (this.Info.Continuous)
        {
            this.BigMachine.Continuous.RemoveMachine(this);
        }*/
    }

    /// <summary>
    /// Generated method which is called when the machine executes.
    /// </summary>
    /// <param name="parameter">StateParameter.</param>
    /// <returns>StateResult.</returns>
    protected virtual Task<StateResult> InternalRun(StateParameter parameter)
    {// Called: Machine.RunMachine()
        return Task.FromResult(StateResult.Terminate);
    }

    /// <summary>
    /// Generated method which is called when the state changes.
    /// </summary>
    /// <param name="state">The next state.</param>
    /// <param name="rerun">The machine wll re-run if <paramref name="rerun"/> is <see langword="true"/>, and the machine state is changed.</param>
    /// <returns><see langword="true"/>: State changed. <see langword="false"/>: Not changed (same state or denied). </returns>
    protected virtual ChangeStateResult InternalChangeState(int state, bool rerun)
        => ChangeStateResult.Terminated;

    /// <summary>
    /// Called when the machine is terminating.<br/>
    /// This code is inside 'lock (this.Machine)' statement.
    /// </summary>
    protected virtual void OnTerminated()
    {
    }
}
