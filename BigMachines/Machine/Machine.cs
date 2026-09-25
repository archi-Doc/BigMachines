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
/// Provides the runtime base for generated state machines.
/// </summary>
// TinyhandObject must be applied to each serializable concrete machine. Applying it here
// makes Tinyhand's NativeAOT registration treat every derived machine as serializable.
public abstract partial class Machine
{
    internal const int ReservedKeyCount = 9;
    private static uint serialNumber;
    private int removed;
    private int runQueued;

    private static void DecreaseRemaining(ref long remaining, long elapsed)
    {
        var current = Volatile.Read(ref remaining);
        while (current > 0 && current != long.MaxValue && elapsed > 0)
        {
            var next = current <= elapsed ? 0 : current - elapsed;
            var observed = Interlocked.CompareExchange(ref remaining, next, current);
            if (observed == current)
            {
                return;
            }

            current = observed;
        }
    }

    public Machine()
    {
        this.__machineSerial__ = Interlocked.Increment(ref serialNumber);
    }

    private void Prepare(MachineControl control)
    {// Deserialize
        if (Interlocked.CompareExchange(ref this.__machineControl__, control, null!) is not null)
        {
            throw new InvalidOperationException("A machine instance cannot be attached more than once. Register machine services as transient.");
        }

        // The control sets up the structure after publishing the machine; changes made by OnCreate/OnStart are persisted by its addition record.
        this.__operationalState__ = default; // Operational flags are not persisted.
        if (this.DefaultInterval != TimeSpan.Zero && this.__timeUntilRun__ == long.MaxValue)
        {
            this.__timeUntilRun__ = 0;
        }
    }

    internal void PrepareStart(MachineControl control)
    {// Deserialize
        this.Prepare(control);
        this.OnStart();
    }

    internal bool IsPreparedFor(MachineControl control)
        => ReferenceEquals(this.__machineControl__, control);

    internal void PrepareCreateStart(MachineControl control, object? createParameter)
    {// Create machine
        this.Prepare(control);
        this.OnCreate(createParameter);
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
                writer.WriteKeyRecord();
                writer.Write(1);
                writer.WriteValueRecord();
                writer.Write(value);
                root.AddJournalAndDispose(ref writer);
            }

            this.__machineState__ = value;
        }
    }

    /// <summary>
    /// The remaining time until the machine runs.
    /// </summary>
    [Key(2)]
    protected long __timeUntilRun__ = long.MaxValue; // TimeSpan.Ticks (for interlocked)

    [IgnoreMember]
    protected TimeSpan TimeUntilRun
    {
        get => new(Volatile.Read(ref this.__timeUntilRun__));
        set
        {
            if (this.__timeUntilRun__ == value.Ticks)
            {
                return;
            }

            if (this is IStructuralObject structuralObject &&
                structuralObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.WriteKeyRecord();
                writer.Write(2);
                writer.WriteValueRecord();
                writer.Write(value.Ticks);
                root.AddJournalAndDispose(ref writer);
            }

            Volatile.Write(ref this.__timeUntilRun__, value.Ticks);
        }
    }

    /// <summary>
    /// The UTC time when this machine last ran.
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
                writer.WriteKeyRecord();
                writer.Write(3);
                writer.WriteValueRecord();
                writer.Write(value);
                root.AddJournalAndDispose(ref writer);
            }

            this.__lastRunTime__ = value;
        }
    }

    /// <summary>
    /// The next scheduled UTC time for this machine to run.
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
                writer.WriteKeyRecord();
                writer.Write(4);
                writer.WriteValueRecord();
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
        get => new(Volatile.Read(ref this.__lifespan__));
        set
        {
            if (this.__lifespan__ == value.Ticks)
            {
                return;
            }

            if (this is IStructuralObject structuralObject &&
                structuralObject.TryGetJournalWriter(out var root, out var writer, true))
            {
                writer.WriteKeyRecord();
                writer.Write(5);
                writer.WriteValueRecord();
                writer.Write(value.Ticks);
                root.AddJournalAndDispose(ref writer);
            }

            Volatile.Write(ref this.__lifespan__, value.Ticks);
        }
    }

    /// <summary>
    /// Stores the UTC time at which the machine terminates automatically.
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
                writer.WriteKeyRecord();
                writer.Write(6);
                writer.WriteValueRecord();
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

    public virtual MachineHandle HandleInstance => default!;

    internal OperationalFlags OperationalState => this.__operationalState__;

    internal bool IsActive =>
        !this.__operationalState__.HasFlag(OperationalFlags.Terminated) &&
        (this.__operationalState__.HasFlag(OperationalFlags.Running) || this.DefaultInterval > TimeSpan.Zero ||
            Volatile.Read(ref this.__timeUntilRun__) != long.MaxValue || this.__nextRunTime__ != default);

    internal bool IsRunning =>
        this.__operationalState__.HasFlag(OperationalFlags.Running) &&
        !this.__operationalState__.HasFlag(OperationalFlags.Terminated);

    internal bool IsTerminated
            => this.__operationalState__.HasFlag(OperationalFlags.Terminated);

    protected readonly SemaphoreLock Semaphore = new();

    /// <summary>
    /// Gets the default interval between timer runs. Zero disables periodic execution; this value is not serialized.
    /// </summary>
    [IgnoreMember]
    protected TimeSpan DefaultInterval { get; init; }

    [IgnoreMember]
    protected volatile OperationalFlags __operationalState__;

    [IgnoreMember]
    protected object __machineControl__ = default!;

    [IgnoreMember]
    protected object? __handleInstance__;

    /// <summary>
    /// Indicates whether the current state dispatch should run again.
    /// </summary>
    [IgnoreMember]
    protected bool __requestRerun__;

    /// <summary>
    /// Stores the process-local serial number of this machine.
    /// </summary>
    [IgnoreMember]
    protected uint __machineSerial__;

    #endregion

    internal void Process(DateTime now, TimeSpan elapsed)
    {
        DecreaseRemaining(ref this.__lifespan__, elapsed.Ticks);
        if (this.__operationalState__ == 0)
        {// Stand-by
            DecreaseRemaining(ref this.__timeUntilRun__, elapsed.Ticks);
        }

        if (this.__lifespan__ <= 0 || this.__terminationTime__ <= now)
        {// Terminate
            this.TryTerminate();
        }
        else if (this.__operationalState__ == 0 &&
            (this.__timeUntilRun__ <= 0 || (this.__nextRunTime__ != default && this.__nextRunTime__ <= now)))
        {// Screening
            this.RunAndForget(now);
        }
    }

    internal Task ProcessImmediately(DateTime now)
    {
        Volatile.Write(ref this.__timeUntilRun__, 0);
        if (this.__lifespan__ <= 0 || this.__terminationTime__ <= now)
        {// Terminate
            this.HandleInstance.Terminate();
        }
        else if (this.__operationalState__ == 0)
        {// Screening
            return this.RunAndForget(now, reserved: true);
        }

        Volatile.Write(ref this.runQueued, 0);
        return Task.CompletedTask;
    }

    internal bool TryReserveRun()
        => Interlocked.CompareExchange(ref this.runQueued, 1, 0) == 0;

    internal void ProcessLifespan(DateTime now, TimeSpan elapsed)
    {
        DecreaseRemaining(ref this.__lifespan__, elapsed.Ticks);
        if (this.__lifespan__ <= 0 || this.__terminationTime__ <= now)
        {// Terminate
            this.TryTerminate();
        }
    }

    /// <summary>
    /// Terminates and removes this machine from the shared timer loop without waiting for its semaphore.<br/>
    /// A busy machine keeps its expired lifespan or termination time and is retried on the next pass, so it cannot stall other machines.
    /// </summary>
    private void TryTerminate()
    {
        if (!this.Semaphore.TryEnter())
        {
            return;
        }

        var terminate = !this.IsTerminated;
        try
        {
            if (terminate)
            {
                this.Terminate();
            }
        }
        finally
        {
            this.Semaphore.Exit();
        }

        if (terminate)
        {
            this.RemoveFromControl();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Task RunAndForget(DateTime now, bool reserved = false)
    {
        if (!reserved && !this.TryReserveRun())
        {
            return Task.CompletedTask;
        }

        return Task.Run(async () =>
        {
            await this.Semaphore.EnterAsync().ConfigureAwait(false);
            try
            {
                if (await this.TryRun(now).ConfigureAwait(false) == StateResult.Terminate)
                {
                    this.Terminate();
                }
            }
            finally
            {
                this.Semaphore.Exit();

                try
                {
                    if (this.IsTerminated)
                    {
                        this.RemoveFromControl();
                    }
                }
                finally
                {
                    Volatile.Write(ref this.runQueued, 0);
                }
            }
        });
    }

    private async Task<StateResult> TryRun(DateTime now)
    {// Locked
        if (this.__operationalState__ != 0)
        {
            return StateResult.Continue;
        }

        var runFlag = false;
        if (this.__timeUntilRun__ <= 0)
        {// Timeout
            if (this.DefaultInterval <= TimeSpan.Zero)
            {
                this.TimeUntilRun = TimeSpan.MaxValue;
            }
            else
            {
                this.TimeUntilRun = this.DefaultInterval;
            }

            runFlag = true;
        }

        if (this.__nextRunTime__ != default && this.__nextRunTime__ <= now)
        {
            this.NextRunTime = default;
            runFlag = true;
        }

        if (!runFlag)
        {
            return StateResult.Continue;
        }

        return await this.RunMachine(RunType.Timer, now).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the machine while its semaphore is held.
    /// </summary>
    /// <param name="runType">A trigger of the machine running.</param>
    /// <param name="now">Current time.</param>
    /// <returns>The state-method result.</returns>
    private async Task<StateResult> RunMachine(RunType runType, DateTime now)
    {// Called: Machine.DistributeCommand(), BigMachine.MainLoop()
        if ((this.__operationalState__ & (OperationalFlags.Running | OperationalFlags.Paused | OperationalFlags.Terminated)) != 0)
        {// Machine cannot run
            return StateResult.Continue;
        }

        this.__operationalState__ |= OperationalFlags.Running;
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
            ((IBigMachine)this.BigMachine).ReportException(new(this, ex));
        }

        if (result == StateResult.Terminate)
        {
            this.LastRunTime = now;
            this.__operationalState__ &= ~OperationalFlags.Running;
            return result;
        }
        else if (this.__requestRerun__)
        {
            goto RerunLoop;
        }

        this.LastRunTime = now;
        this.__operationalState__ &= ~OperationalFlags.Running;
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool RemoveFromControl()
    {
        if (Interlocked.Exchange(ref this.removed, 1) != 0)
        {
            return false;
        }

        var result = (this.__machineControl__ as MachineControl)?.RemoveMachine(this) == true;

        if (this is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception exception)
            {
                ((IBigMachine)this.BigMachine).ReportException(new(this, exception));
            }
        }

        return result;
    }

    // Called with the machine semaphore held; callbacks cannot prevent removal.
    private void Terminate()
    {
        this.__operationalState__ |= OperationalFlags.Terminated;
        try
        {
            this.OnTerminate();
        }
        catch (Exception exception)
        {
            ((IBigMachine)this.BigMachine).ReportException(new(this, exception));
        }
    }

    /// <summary>
    /// Represents the generated dispatch method called when the machine executes.
    /// </summary>
    /// <param name="parameter">The state invocation context.</param>
    /// <returns>The state-method result.</returns>
    protected virtual Task<StateResult> __InternalRun__(StateParameter parameter)
    {// Called: Machine.RunMachine()
        return Task.FromResult(StateResult.Terminate);
    }

    /// <summary>
    /// Represents the generated dispatch method called when the state changes.
    /// </summary>
    /// <param name="state">The next state.</param>
    /// <param name="rerun">Whether to run the new state immediately after a successful transition.</param>
    /// <returns>The result of the transition.</returns>
    protected virtual ChangeStateResult __InternalChangeState__(int state, bool rerun)
        => ChangeStateResult.Terminated;

    /// <summary>
    /// Called once when the machine is newly created, including creation without a parameter.<br/>
    /// Note that it is not called after deserialization.<br/>
    /// <see cref="OnCreate(object?)"/> -> <see cref="OnStart()"/> -> <see cref="OnTerminate"/>.
    /// </summary>
    /// <param name="createParameter">The parameters used when creating a machine.</param>
    protected virtual void OnCreate(object? createParameter)
    {
    }

    /// <summary>
    /// Called when the machine is ready to start.<br/>
    /// Note that it is called before the actual state method.<br/>
    /// <see cref="OnCreate(object?)"/> -> <see cref="OnStart()"/> -> <see cref="OnTerminate"/>.
    /// </summary>
    protected virtual void OnStart()
    {
    }

    /// <summary>
    /// Called when the machine is terminating.<br/>
    /// This method runs once while the machine semaphore is held. Exceptions are queued on the root.<br/>
    /// <see cref="OnCreate(object?)"/> -> <see cref="OnStart()"/> -> <see cref="OnTerminate"/>.
    /// </summary>
    protected virtual void OnTerminate()
    {
    }

    public override string ToString()
        => $"Machine: {this.GetType().Name}";
}
