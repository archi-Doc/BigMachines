// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines.Control;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1202
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1309 // Field names should not begin with underscore

namespace BigMachines;

/// <summary>
/// Represents an abstract class that serves as the base for the actual machine class.
/// </summary>
[TinyhandObject(ReservedKeyCount = ReservedKeyCount)]
public abstract partial class Machine
{
    internal const int ReservedKeyCount = 9;
    private static uint serialNumber;

    public Machine()
    {
        this.__machineSerial__ = Interlocked.Increment(ref serialNumber);
    }

    private void Prepare(MachineControl control)
    {// Deserialize
        this.__machineControl__ = control;
        this.__operationalState__ = default; // Initialize the state in case the Machine instance is reused.
        if (this is IStructuralObject obj &&
            this.MachineControl is IStructuralObject parent)
        {
            obj.SetupStructure(parent);
        }

        if (this.DefaultTimeout != TimeSpan.Zero && this.__timeUntilRun__ == long.MaxValue)
        {
            this.__timeUntilRun__ = 0;
        }
    }

    internal void PrepareStart(MachineControl control)
    {// Deserialize
        this.Prepare(control);
        this.OnStart();
    }

    internal void PrepareCreateStart(MachineControl control, object? createParam)
    {// Create machine
        this.Prepare(control);
        this.OnCreate(createParam);
        this.OnStart();
    }

    #region Keys

    // Defined in Machine<TIdentifier>
    // [Key(0)]
    // public TIdentifier Identifier { get; protected set; }

    /// <summary>
    /// Gets or sets the current state of this machine.
    /// </summary>
    [Key(1)]
    protected int __machineState__;

    [IgnoreMember]
    protected int MachineState
    {
        get => this.__machineState__;
        set
        {
            if (this.__machineState__ == value)
            {
                return;
            }

            if (this is IStructuralObject structuralObject &&
                structuralObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(1);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournalAndDispose(ref writer);
            }

            this.__machineState__ = value;
        }
    }

    /// <summary>
    /// The time until the machine starts.
    /// </summary>
    [Key(2)]
    protected long __timeUntilRun__ = long.MaxValue; // TimeSpan.Ticks (for interlocked)

    [IgnoreMember]
    protected TimeSpan TimeUntilRun
    {
        get => new(this.__timeUntilRun__);
        set
        {
            if (this.__timeUntilRun__ == value.Ticks)
            {
                return;
            }

            if (this is IStructuralObject structuralObject &&
                structuralObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(2);
                writer.Write_Value();
                writer.Write(value.Ticks);
                root.AddJournalAndDispose(ref writer);
            }

            this.__timeUntilRun__ = value.Ticks;
        }
    }

    /// <summary>
    /// The last <see cref="DateTime"/> when this machine ran.
    /// </summary>
    [Key(3)]
    protected DateTime __lastRunTime__;

    [IgnoreMember]
    protected DateTime LastRunTime
    {
        get => this.__lastRunTime__;
        set
        {
            if (this.__lastRunTime__ == value)
            {
                return;
            }

            if (this is IStructuralObject structuralObject &&
                structuralObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(3);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournalAndDispose(ref writer);
            }

            this.__lastRunTime__ = value;
        }
    }

    /// <summary>
    /// The next scheduled <see cref="DateTime"/> for this machine to run.
    /// </summary>
    [Key(4)]
    protected DateTime __nextRunTime__;

    [IgnoreMember]
    protected DateTime NextRunTime
    {
        get => this.__nextRunTime__;
        set
        {
            if (this.__nextRunTime__ == value)
            {
                return;
            }

            if (this is IStructuralObject structuralObject &&
                structuralObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(4);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournalAndDispose(ref writer);
            }

            this.__nextRunTime__ = value;
        }
    }

    /// <summary>
    /// The remaining lifespan of the machine.<br/>
    /// When it reaches 0, the machine will terminate.
    /// </summary>
    [Key(5)]
    protected long __lifespan__ = long.MaxValue; // TimeSpan.Ticks (for interlocked)

    [IgnoreMember]
    protected TimeSpan Lifespan
    {
        get => new(this.__lifespan__);
        set
        {
            if (this.__lifespan__ == value.Ticks)
            {
                return;
            }

            if (this is IStructuralObject structuralObject &&
                structuralObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(5);
                writer.Write_Value();
                writer.Write(value.Ticks);
                root.AddJournalAndDispose(ref writer);
            }

            this.__lifespan__ = value.Ticks;
        }
    }

    /// <summary>
    /// Gets or sets the time for the machine to shut down automatically.
    /// </summary>
    [Key(6)]
    protected DateTime __terminationTime__ = DateTime.MaxValue;

    [IgnoreMember]
    protected DateTime TerminationTime
    {
        get => this.__terminationTime__;
        set
        {
            if (this.__terminationTime__ == value)
            {
                return;
            }

            if (this is IStructuralObject structuralObject &&
                structuralObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.Write_Key();
                writer.Write(6);
                writer.Write_Value();
                writer.Write(value);
                root.AddJournalAndDispose(ref writer);
            }

            this.__terminationTime__ = value;
        }
    }

    #endregion

    #region FieldAndProperty

    /// <summary>
    /// Gets an instance of <see cref="BigMachineBase"/>.
    /// </summary>
    public BigMachineBase BigMachine => ((MachineControl)this.__machineControl__).BigMachine;

    /// <summary>
    /// Gets <see cref="System.Threading.CancellationToken"/> of the <see cref="BigMachineBase"/>.
    /// </summary>
    public CancellationToken CancellationToken => this.BigMachine.CancellationToken;

    /// <summary>
    /// Gets an instance of <see cref="Control.MachineControl"/>.
    /// </summary>
    public virtual MachineControl? MachineControl => default!;

    public virtual ManMachineInterface InterfaceInstance => default!;

    internal OperationalFlag OperationalState => this.__operationalState__;

    internal bool IsActive =>
        !this.__operationalState__.HasFlag(OperationalFlag.Terminated) &&
        (this.__operationalState__.HasFlag(OperationalFlag.Running) || (this.DefaultTimeout is TimeSpan ts && ts > TimeSpan.Zero));

    internal bool IsRunning =>
        this.__operationalState__.HasFlag(OperationalFlag.Running) &&
        !this.__operationalState__.HasFlag(OperationalFlag.Terminated);

    internal bool IsTerminated
            => this.__operationalState__.HasFlag(OperationalFlag.Terminated);

    protected readonly SemaphoreLock Semaphore = new();

    /// <summary>
    /// Gets the default time interval at which the machine will run.<br/>
    /// <see cref="TimeSpan.Zero"/>: No interval execution.<br/>
    /// This property is NOT serialization target.
    /// </summary>
    [IgnoreMember]
    protected TimeSpan DefaultTimeout { get; init; }

    [IgnoreMember]
    protected OperationalFlag __operationalState__;

    [IgnoreMember]
    protected object __machineControl__ = default!;

    [IgnoreMember]
    protected object? __interfaceInstance__;

    /// <summary>
    /// Gets or sets a value indicating whether the machine is going to re-run.
    /// </summary>
    [IgnoreMember]
    protected bool __requestRerun__;

    /// <summary>
    /// Get the serial (unique) number of this machine.
    /// </summary>
    [IgnoreMember]
    protected uint __machineSerial__;

    #endregion

    internal void Process(DateTime now, TimeSpan elapsed)
    {
        var canRun = true;

        Interlocked.Add(ref this.__lifespan__, -elapsed.Ticks);
        if (this.__operationalState__ == 0)
        {// Stand-by
            Interlocked.Add(ref this.__timeUntilRun__, -elapsed.Ticks);
        }

        if (this.__lifespan__ <= 0 || this.__terminationTime__ <= now)
        {// Terminate
            this.InterfaceInstance.TerminateMachine();
        }
        else if (canRun && (this.__timeUntilRun__ <= 0 || this.__nextRunTime__ >= now) && !this.__operationalState__.HasFlag(OperationalFlag.Running))
        {// Screening
            this.RunAndForget(now);
        }
    }

    internal Task ProcessImmediately(DateTime now)
    {
        Volatile.Write(ref this.__timeUntilRun__, 0);
        if (this.__lifespan__ <= 0 || this.__terminationTime__ <= now)
        {// Terminate
            this.InterfaceInstance.TerminateMachine();
        }
        else if (!this.__operationalState__.HasFlag(OperationalFlag.Running))
        {// Screening
            return this.RunAndForget(now);
        }

        return Task.CompletedTask;
    }

    internal void ProcessLifespan(DateTime now, TimeSpan elapsed)
    {
        Interlocked.Add(ref this.__lifespan__, -elapsed.Ticks);
        if (this.__lifespan__ <= 0 || this.__terminationTime__ <= now)
        {// Terminate
            this.InterfaceInstance.TerminateMachine();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Task RunAndForget(DateTime now)
    {
        return Task.Run(() =>
        {
            this.Semaphore.Enter();
            try
            {
                if (this.TryRun(now) == StateResult.Terminate)
                {
                    this.__operationalState__ |= OperationalFlag.Terminated;
                    this.OnTerminate();
                }
            }
            finally
            {
                this.Semaphore.Exit();

                if (this.__operationalState__.HasFlag(OperationalFlag.Terminated))
                {
                    this.RemoveFromControl();
                }
            }
        });
    }

    private StateResult TryRun(DateTime now)
    {// Locked
        var runFlag = false;
        if (this.__timeUntilRun__ <= 0)
        {// Timeout
            if (this.DefaultTimeout <= TimeSpan.Zero)
            {
                Volatile.Write(ref this.__timeUntilRun__, long.MinValue);
            }
            else
            {
                Volatile.Write(ref this.__timeUntilRun__, this.DefaultTimeout.Ticks);
            }

            runFlag = true;
        }

        if (this.__nextRunTime__ >= now)
        {
            this.__nextRunTime__ = default;
            runFlag = true;
        }

        if (!runFlag)
        {
            return StateResult.Continue;
        }

        return this.RunMachine(RunType.Timer, now).Result;
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
        if (this.__operationalState__.HasFlag(OperationalFlag.Running))
        {// Machine is running
            return StateResult.Continue;
        }

        this.__operationalState__ |= OperationalFlag.Running;
RerunLoop:
        StateResult result;
        this.__requestRerun__ = false;

        try
        {
            result = await this.__InternalRun__(new(runType)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result = StateResult.Terminate;
            (this.MachineControl?.BigMachine as IBigMachine)?.ReportException(new(this, ex));
        }

        if (result == StateResult.Terminate)
        {
            this.LastRunTime = now;
            this.__operationalState__ &= ~OperationalFlag.Running;
            return result;
        }
        else if (this.__requestRerun__)
        {
            goto RerunLoop;
        }

        this.LastRunTime = now;
        this.__operationalState__ &= ~OperationalFlag.Running;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool RemoveFromControl()
    {
        var result = (this.__machineControl__ as MachineControl)?.RemoveMachine(this) == true;

        if (this is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return result;
    }

    /// <summary>
    /// Generated method which is called when the machine executes.
    /// </summary>
    /// <param name="parameter">StateParameter.</param>
    /// <returns>StateResult.</returns>
    protected virtual Task<StateResult> __InternalRun__(StateParameter parameter)
    {// Called: Machine.RunMachine()
        return Task.FromResult(StateResult.Terminate);
    }

    /// <summary>
    /// Generated method which is called when the state changes.
    /// </summary>
    /// <param name="state">The next state.</param>
    /// <param name="rerun">The machine wll re-run if <paramref name="rerun"/> is <see langword="true"/>, and the machine state is changed.</param>
    /// <returns><see langword="true"/>: State changed. <see langword="false"/>: Not changed (same state or denied). </returns>
    protected virtual ChangeStateResult __InternalChangeState__(int state, bool rerun)
        => ChangeStateResult.Terminated;

    /// <summary>
    /// Called when the machine is newly created.<br/>
    /// Note that it is not called after deserialization.<br/>
    /// <see cref="OnCreate(object?)"/> -> <see cref="OnStart()"/> -> <see cref="OnTerminate"/>.
    /// </summary>
    /// <param name="createParam">The parameters used when creating a machine.</param>
    protected virtual void OnCreate(object? createParam)
    {
    }

    /// <summary>
    /// Called when the machine is ready to start<br/>
    /// Note that it is called before the actual state method.<br/>
    /// <see cref="OnCreate(object?)"/> -> <see cref="OnStart()"/> -> <see cref="OnTerminate"/>.
    /// </summary>
    protected virtual void OnStart()
    {
    }

    /// <summary>
    /// Called when the machine is terminating.<br/>
    ///  This code is inside a semaphore lock.<br/>
    ///  <see cref="OnCreate(object?)"/> -> <see cref="OnStart()"/> -> <see cref="OnTerminate"/>.
    /// </summary>
    protected virtual void OnTerminate()
    {
    }

    public override string ToString()
        => $"Machine: {this.GetType().Name}";
}
