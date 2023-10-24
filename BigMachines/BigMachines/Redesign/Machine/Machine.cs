// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Arc.Threading;
using Tinyhand;

#pragma warning disable SA1401 // Fields should be private

namespace BigMachines.Redesign;

[TinyhandObject(ReservedKeys = 10)]
public abstract partial class Machine
{
    public Machine(MachineControl control)
    {
        this.Control = control;
    }

    #region FieldAndProperty

    public MachineControl Control { get; }

    

    /// <summary>
    /// Gets the machine status (running, paused, terminated).
    /// </summary>
    [Key(0)]
    public MachineStatus Status { get; private set; } = MachineStatus.Running;

    protected SemaphoreLock Semaphore { get; } = new();

    /// <summary>
    /// Gets or sets a running state of the machine.
    /// </summary>
    internal RunType RunType;

    /// <summary>
    /// Gets or sets a value indicating whether the machine is going to re-run.
    /// </summary>
    protected internal bool RequestRerun { get; set; }

    protected object? interfaceInstance;

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
        this.RequestRerun = false;

        try
        {
            result = await this.InternalRun(new(runType)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result = StateResult.Terminate;
            // this.BigMachine.ReportException(new(this, ex));
        }

        if (result == StateResult.Terminate)
        {
            // this.LastRun = now;
            this.RunType = RunType.NotRunning;
            return result;
        }
        else if (this.RequestRerun)
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
            this.Status = MachineStatus.Terminated;
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
        this.Control.RemoveFromGroup(this);
        if (this.Info.Continuous)
        {
            this.BigMachine.Continuous.RemoveMachine(this);
        }
    }

    /// <summary>
    /// Generated method which is called when the state changes.
    /// </summary>
    /// <param name="state">The next state.</param>
    /// <param name="rerun">If <see langword="true"/>, the machine wll rerun if the Machine state is changed.</param>
    /// <returns><see langword="true"/>: State changed. <see langword="false"/>: Not changed (same state or denied). </returns>
    protected virtual ChangeStateResult InternalChangeState(int state, bool rerun) => ChangeStateResult.Terminated;

    /// <summary>
    /// Called when the machine is terminating.<br/>
    /// This code is inside 'lock (this.Machine)' statement.
    /// </summary>
    protected virtual void OnTerminated()
    {
    }
}

[TinyhandObject(ReservedKeys = 10)]
public abstract partial class Machine<TIdentifier> : Machine
{
    public Machine(MachineControl<TIdentifier> control, TIdentifier identifier)
        : base(control)
    {
        this.Control = control;
        this.Identifier = identifier;
    }

    public new MachineControl<TIdentifier> Control { get; }

    public TIdentifier Identifier { get; }
}
