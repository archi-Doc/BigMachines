// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Arc.Threading;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1202
#pragma warning disable SA1401 // Fields should be private

namespace BigMachines.Redesign;

/// <summary>
/// Represents an abstract class that serves as the base for the actual machine class.<br/>
/// <see cref="Machine{TIdentifier}"/> = <see cref="Machine"/>+<typeparamref name="TIdentifier"/>.
/// </summary>
/// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
[TinyhandObject(ReservedKeys = Machine.ReservedKeyNumber)]
public abstract partial class Machine<TIdentifier> : Machine
    where TIdentifier : notnull
{
    internal Machine()
        : base(default!)
    {
        this.Control = default!;
        this.Identifier = default!;
    }

    public Machine(MachineControl<TIdentifier> control, TIdentifier identifier)
        : base(control)
    {
        this.Control = control;
        this.Identifier = identifier;
    }

    /// <summary>
    /// Gets an instance of <see cref="MachineControl{TIdentifier}"/>.
    /// </summary>
    public new MachineControl<TIdentifier> Control { get; }

    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    [Key(0, IgnoreKeyReservation = true)]
    public TIdentifier Identifier { get; protected set; }
}

/// <summary>
/// Represents an abstract class that serves as the base for the actual machine class.
/// </summary>
[TinyhandObject(ReservedKeys = ReservedKeyNumber)]
public abstract partial class Machine
{
    internal const int ReservedKeyNumber = 10;
    private static uint serialNumber;

    public Machine()
    {
        this.Control = default!;
    }

    public Machine(MachineControl control)
    {
        this.Control = control;
        this.machineNumber = Interlocked.Increment(ref serialNumber);
    }

    #region Keys

    /// <summary>
    /// Gets or sets the operational state of the machine (running, paused, terminated).
    /// </summary>
    [Key(1)]
    protected OperationalState operationalState = OperationalState.Running;

    [IgnoreMember]
    protected OperationalState OperationalState
    {
        get => this.operationalState;
        set
        {
            if (this.operationalState == value)
            {
                return;
            }

            if (this is ITreeObject treeObject &&
                treeObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(1);
                writer.Write_Value();
                var ev = value;
                writer.Write(Unsafe.As<OperationalState, int>(ref ev));
                root.AddJournal(writer);
            }

            this.operationalState = value;
        }
    }

    /// <summary>
    /// Gets or sets the current state of this machine.
    /// </summary>
    [Key(2)]
    protected int currentState;

    [IgnoreMember]
    protected int CurrentState
    {
        get => this.currentState;
        set
        {
            if (this.currentState == value)
            {
                return;
            }

            if (this is ITreeObject treeObject &&
                treeObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(2);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournal(writer);
            }

            this.currentState = value;
        }
    }

    /// <summary>
    /// The time until this machine starts.
    /// </summary>
    [Key(3)]
    protected long startTime = long.MaxValue; // TimeSpan.Ticks (for interlocked)

    [IgnoreMember]
    protected long StartTime
    {
        get => this.currentState;
        set
        {
            if (this.startTime == value)
            {
                return;
            }

            if (this is ITreeObject treeObject &&
                treeObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(3);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournal(writer);
            }

            this.startTime = value;
        }
    }

    /// <summary>
    /// The last <see cref="DateTime"/> when this machine ran.
    /// </summary>
    [Key(4)]
    protected DateTime lastRun;

    [IgnoreMember]
    protected DateTime LastRun
    {
        get => this.lastRun;
        set
        {
            if (this.lastRun == value)
            {
                return;
            }

            if (this is ITreeObject treeObject &&
                treeObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(4);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournal(writer);
            }

            this.lastRun = value;
        }
    }

    /// <summary>
    /// The next scheduled <see cref="DateTime"/> for this machine to run.
    /// </summary>
    [Key(5)]
    protected DateTime nextRun;

    [IgnoreMember]
    protected DateTime NextRun
    {
        get => this.nextRun;
        set
        {
            if (this.nextRun == value)
            {
                return;
            }

            if (this is ITreeObject treeObject &&
                treeObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(5);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournal(writer);
            }

            this.nextRun = value;
        }
    }

    /// <summary>
    /// The remaining lifespan of this machine. When it reaches 0, the machine will terminate.
    /// </summary>
    [Key(6)]
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

            if (this is ITreeObject treeObject &&
                treeObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(6);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournal(writer);
            }

            this.lifespan = value;
        }
    }

    /// <summary>
    /// Gets or sets <see cref="DateTime"/> when the machine will be automatically terminated.
    /// </summary>
    [Key(7)]
    protected DateTime terminationDate = DateTime.MaxValue;

    [IgnoreMember]
    protected DateTime TerminationDate
    {
        get => this.terminationDate;
        set
        {
            if (this.terminationDate == value)
            {
                return;
            }

            if (this is ITreeObject treeObject &&
                treeObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(7);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournal(writer);
            }

            this.terminationDate = value;
        }
    }

    #endregion

    #region FieldAndProperty

    /// <summary>
    /// Gets an instance of <see cref="MachineControl"/>.
    /// </summary>
    public readonly MachineControl Control;
    protected readonly SemaphoreLock Semaphore = new();

    [IgnoreMember]
    protected object? interfaceInstance;

    /// <summary>
    /// Gets or sets a running state of the machine.
    /// </summary>
    internal RunType RunType;

    /// <summary>
    /// Gets or sets the default time interval at which the machine will run.<br/>
    /// <see cref="TimeSpan.Zero"/>: No interval execution.<br/>
    /// This property is NOT serialization target.
    /// </summary>
    [IgnoreMember]
    protected internal TimeSpan DefaultTimeout;

    /// <summary>
    /// Gets a TypeId of the machine.
    /// </summary>
    [IgnoreMember]
    protected internal uint TypeId;

    /// <summary>
    /// Gets or sets a value indicating whether the machine is going to re-run.
    /// </summary>
    [IgnoreMember]
    protected bool requestRerun;

    /// <summary>
    /// Get the serial (unique) number of this machine.
    /// </summary>
    [IgnoreMember]
    private uint machineNumber;

    #endregion

    /// <summary>
    /// Run the machine.<br/>
    /// This code is inside 'lock (this.Machine)' statement.
    /// </summary>
    /// <param name="runType">A trigger of the machine running.</param>
    /// <param name="now">Current time.</param>
    /// <returns>true: The machine is terminated.</returns>
    protected internal async Task<StateResult> RunMachine(RunType runType, DateTime now)
    {// Called: Machine.DistributeCommand(), BigMachine.MainLoop()
        if (this.RunType != RunType.NotRunning)
        {// Machine is running
            return StateResult.Continue;
        }

        this.RunType = runType;
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
            this.Control.BigMachine.ReportException(new(this, ex));
        }

        if (result == StateResult.Terminate)
        {
            // this.LastRun = now;
            this.RunType = RunType.NotRunning;
            return result;
        }
        else if (this.requestRerun)
        {
            goto RerunLoop;
        }

        this.LastRun = now;
        this.RunType = RunType.NotRunning;
        return result;
    }

    /// <summary>
    /// Generated method which is called when the machine executes.
    /// </summary>
    /// <param name="parameter">StateParameter.</param>
    /// <returns>StateResult.</returns>
    protected internal virtual Task<StateResult> InternalRun(StateParameter parameter)
    {// Called: Machine.RunMachine()
        return Task.FromResult(StateResult.Terminate);
    }

    internal Task TaskRunAndTerminate()
    {
        return Task.Run(() => this.TerminateAndRemoveFromGroup());
    }

    internal async Task TerminateAndRemoveFromGroup()
    {
        await this.Semaphore.EnterAsync().ConfigureAwait(false);
        try
        {
            this.OperationalState = OperationalState.Terminated;
            this.OnTerminated();
        }
        finally
        {
            this.Semaphore.Exit();
            this.RemoveFromControl();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void RemoveFromControl()
    {
        this.Control.RemoveMachine(this);
        /*if (this.Info.Continuous)
        {
            this.BigMachine.Continuous.RemoveMachine(this);
        }*/
    }

    /// <summary>
    /// Generated method which is called when the state changes.
    /// </summary>
    /// <param name="state">The next state.</param>
    /// <param name="rerun">The machine wll re-run if <paramref name="rerun"/> is <see langword="true"/>, and the machine state is changed.</param>
    /// <returns><see langword="true"/>: State changed. <see langword="false"/>: Not changed (same state or denied). </returns>
    protected virtual ChangeStateResult InternalChangeState(int state, bool rerun) => ChangeStateResult.Terminated;

    /// <summary>
    /// Called when the machine is terminating.<br/>
    /// This code is inside 'lock (this.Machine)' statement.
    /// </summary>
    protected virtual void OnTerminated()
    {
    }

    /// <summary>
    /// Set the start time of the machine.<br/>
    /// The time decreases while the program is running, and the machine will run when it reaches zero.
    /// </summary>
    /// <param name="timeout">The timeout.</param>
    /// <param name="absoluteDateTime">Set <see langword="true"></see> to specify the next execution time by adding the current time and timeout.</param>
    protected internal void SetTimeout(TimeSpan timeout, bool absoluteDateTime = false)
    {
        this.requestRerun = false;
        if (timeout.Ticks < 0)
        {
            Volatile.Write(ref this.startTime, long.MaxValue);
            this.NextRun = default;
            return;
        }

        if (absoluteDateTime)
        {
            this.NextRun = DateTime.UtcNow + timeout;
        }
        else
        {
            Volatile.Write(ref this.startTime, timeout.Ticks);
        }
    }

    /// <summary>
    /// Set the lifespen of the machine.<br/>
    /// The lifespan decreases while the program is running, and the machine will terminate when it reaches zero.
    /// </summary>
    /// <param name="timeSpan">The lifespan.</param>
    /// <param name="absoluteDateTime">Set true to specify the terminate time by adding the current time and lifespan.</param>
    protected internal void SetLifespan(TimeSpan timeSpan, bool absoluteDateTime = false)
    {
        if (timeSpan.Ticks < 0)
        {
            Volatile.Write(ref this.lifespan, long.MaxValue);
            this.TerminationDate = DateTime.MaxValue;
            return;
        }

        if (absoluteDateTime)
        {
            this.TerminationDate = DateTime.UtcNow + timeSpan;
        }
        else
        {
            Volatile.Write(ref this.lifespan, timeSpan.Ticks);
        }
    }
}
