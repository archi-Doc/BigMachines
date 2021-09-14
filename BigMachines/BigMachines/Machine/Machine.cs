// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tinyhand;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1401
#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines
{
    /// <summary>
    /// Represents a base class for all machine classes.<br/>
    /// </summary>
    /// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
    [TinyhandObject(ReservedKeys = 10)]
    public abstract class Machine<TIdentifier>
        where TIdentifier : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Machine{TIdentifier}"/> class.
        /// </summary>
        /// <param name="bigMachine">BigMachine which contains an instance of this machine.</param>
        public Machine(BigMachine<TIdentifier> bigMachine)
        {
            this.BigMachine = bigMachine;
            if (!this.BigMachine.MachineTypeToGroup.TryGetValue(this.GetType(), out var group))
            {
                throw new InvalidOperationException($"Machine type {this.GetType().FullName} is not registered.");
            }

            this.Group = group;
            this.TypeId = group.Info.TypeId;
        }

        /// <summary>
        /// Gets a instance of <see cref="BigMachine"/>.
        /// </summary>
        public BigMachine<TIdentifier> BigMachine { get; }

        /// <summary>
        /// Gets a instance of <see cref="MachineGroup{TIdentifier}"/>.
        /// </summary>
        public IMachineGroup<TIdentifier> Group { get; }

        /// <summary>
        /// Gets <see cref="MachineInfo{TIdentifier}"/>.
        /// </summary>
        public MachineInfo<TIdentifier> Info => this.Group.Info;

        /// <summary>
        /// Gets or sets the identifier of this machine.<br/>
        /// TIdentifier type must have <see cref="TinyhandObjectAttribute"/>.
        /// </summary>
        [Key(0)]
        public TIdentifier Identifier { get; protected set; } = default!;

        /// <summary>
        /// Gets or sets the status (running, paused, terminated) of this machine.
        /// </summary>
        [Key(1)]
        public MachineStatus Status { get; protected internal set; } = MachineStatus.Running;

        /// <summary>
        /// Gets or sets the current state of this machine.
        /// </summary>
        [Key(2)]
        protected internal int CurrentState;

        /// <summary>
        /// The time until the machine is executed.
        /// </summary>
        [Key(3)]
        protected internal long Timeout = long.MaxValue; // TimeSpan.Ticks (for interlocked)

        /// <summary>
        /// Gets or sets <see cref="DateTime"/> when the machine is executed last time.
        /// </summary>
        [Key(4)]
        public DateTime LastRun { get; protected internal set; }

        /// <summary>
        /// Gets or sets <see cref="DateTime"/> when the machine is will be executed.
        /// </summary>
        [Key(5)]
        public DateTime NextRun { get; protected internal set; }

        /// <summary>
        /// The lifespan of this machine. When this value reaches 0, the machine is terminated.
        /// </summary>
        [Key(6)]
        protected internal long Lifespan = long.MaxValue; // TimeSpan.Ticks (for interlocked)

        /// <summary>
        /// Gets or sets <see cref="DateTime"/> when the machine will be automatically terminated.
        /// </summary>
        [Key(7)]
        public DateTime TerminationDate { get; protected internal set; } = DateTime.MaxValue;

        /// <summary>
        /// Gets or sets the default time interval at which the machine will run.<br/>
        /// <see cref="TimeSpan.Zero"/>: No interval execution.<br/>
        /// This property is NOT serialization target.
        /// </summary>
        [IgnoreMember]
        public TimeSpan DefaultTimeout { get; protected internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether this machine is to be serialized.<br/>
        /// This property is NOT serialization target.
        /// </summary>
        [IgnoreMember]
        public bool IsSerializable { get; protected set; } = false;

        /// <summary>
        /// Gets a TypeId of the machine.
        /// </summary>
        [IgnoreMember]
        public uint TypeId { get; internal set; }

        /// <summary>
        /// Gets or sets a running state of the machine.
        /// </summary>
        internal RunType RunType { get; set; }

        /// <summary>
        /// Receivea a command and invoke an appropriate method.<br/>
        /// </summary>
        /// <param name="group">Machine group.</param>
        /// <param name="command">Command.</param>
        /// <returns><see langword="true"/>: Terminated, <see langword="false"/>: Continue.</returns>
        internal bool DistributeCommand(IMachineGroup<TIdentifier> group, CommandPost<TIdentifier>.Command command)
        {
            if (this.Status == MachineStatus.Terminated)
            {// Terminated
                return true;
            }
            else if (command.Type == CommandPost<TIdentifier>.CommandType.Run ||
                command.Type == CommandPost<TIdentifier>.CommandType.RunTwoWay)
            {// Run
                if (command.LoopChecker is { } checker)
                {
                    if (checker.FindRunId(this.TypeId))
                    {
                        var s = string.Join('-', checker.EnumerateRunId().Take(checker.CommandIdCount).Select(x => this.BigMachine.GetMachineInfoFromTypeId(x)?.MachineType.Name));
                        throw new BigMachine<TIdentifier>.CommandLoopException($"Run loop detected ({s})");
                    }

                    checker = checker.Clone();
                    checker.AddRunId(this.TypeId);
                    LoopChecker.AsyncLocalInstance.Value = checker;

                    // Console.WriteLine("run " + checker);
                }

                lock (this.SyncMachine)
                {
                    this.RunMachine(command, RunType.Manual, DateTime.UtcNow);
                }
            }
            else if ((command.Type == CommandPost<TIdentifier>.CommandType.State ||
                command.Type == CommandPost<TIdentifier>.CommandType.StateTwoWay) &&
                command.Message is int state)
            {// ChangeState
                lock (this.SyncMachine)
                {
                    command.Response = this.InternalChangeState(state, true);
                }
            }
            else
            {// Command
                if (command.LoopChecker is { } checker)
                {
                    if (checker.FindCommandId(this.TypeId))
                    {
                        var s = string.Join('-', checker.EnumerateCommandId().Take(checker.CommandIdCount).Select(x => this.BigMachine.GetMachineInfoFromTypeId(x)?.MachineType.Name));
                        throw new BigMachine<TIdentifier>.CommandLoopException($"Command loop detected ({s})");
                    }

                    checker = checker.Clone();
                    checker.AddCommandId(this.TypeId);
                    LoopChecker.AsyncLocalInstance.Value = checker;

                    // Console.WriteLine("command " + checker);
                }

                // lock (this.SyncMachine) // Not inside lock statement.
                this.ProcessCommand(command);
            }

            return false;
        }

        /// <summary>
        /// Called when the machine is terminating.<br/>
        /// This code is inside 'lock (this.SyncMachine) {}'.
        /// </summary>
        internal void TerminateInternal()
        {
            this.Status = MachineStatus.Terminated;
            if (this.Info.Continuous)
            {
                this.BigMachine.Continuous.RemoveMachine(this);
            }

            this.OnTerminated();
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the machine.
        /// </summary>
        protected internal object SyncMachine { get; } = new();

        /// <summary>
        /// Gets or sets ManMachineInterface.
        /// </summary>
        protected internal ManMachineInterface<TIdentifier>? InterfaceInstance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the machine is going to re-run.
        /// </summary>
        protected internal bool RequestRerun { get; set; }

        /// <summary>
        /// Expected to be implemented on the user side.<br/>
        /// Receives commands and respond if necessary.<br/>
        /// This code is NOT inside 'lock (this.SyncMachine) {}'.
        /// </summary>
        /// <param name="command">The command.</param>
        protected internal virtual void ProcessCommand(CommandPost<TIdentifier>.Command command)
        {// Called: Machine.DistributeCommand()
        }

        /// <summary>
        /// Expected to be implemented on the user side.<br/>
        /// Set a parameter of the machine during <see cref="BigMachine{TIdentifier}.TryCreate{TMachineInterface}(TIdentifier, object?)"/>.<br/>
        /// Note that this method is not called during deserialization.
        /// </summary>
        /// <param name="createParam">The parameter passed when a machine is newly created.</param>
        protected internal virtual void SetParameter(object? createParam)
        {// Override
        }

        /// <summary>
        /// Generated method which is called when creating <see cref="ManMachineInterface{TIdentifier, TState}"/>.
        /// </summary>
        /// <param name="identifier">The identifier of the machine.</param>
        protected internal virtual void CreateInterface(TIdentifier identifier)
        {// Generated
            throw new NotImplementedException();
        }

        /// <summary>
        /// Generated method which is called when the machine executes.
        /// </summary>
        /// <param name="parameter">StateParameter.</param>
        /// <returns>StateResult.</returns>
        protected internal virtual StateResult InternalRun(StateParameter parameter)
        {// Called: Machine.RunMachine()
            return StateResult.Terminate;
        }

        /// <summary>
        /// Generated method which is called when the state changes.
        /// </summary>
        /// <param name="state">The next state.</param>
        /// <param name="rerun">Requires rerun when the state is changed.</param>
        /// <returns><see langword="true"/>: State changed. <see langword="false"/>: Not changed (same state or denied). </returns>
        protected internal virtual bool InternalChangeState(int state, bool rerun) => false;

        /// <summary>
        /// Called when the machine is terminating.<br/>
        /// This code is inside 'lock (this.SyncMachine) {}'.
        /// </summary>
        protected internal virtual void OnTerminated()
        {
        }

        /// <summary>
        /// Run the machine.<br/>
        /// This code is inside 'lock (this.SyncMachine) {}'.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <param name="runType">A trigger of the machine running.</param>
        /// <param name="now">Current time.</param>
        /// <returns>true: The machine is terminated.</returns>
        protected internal StateResult RunMachine(CommandPost<TIdentifier>.Command? command, RunType runType, DateTime now)
        {// Called: Machine.DistributeCommand(), BigMachine.MainLoop()
            this.RunType = runType;

RerunLoop:
            StateResult result;
            this.RequestRerun = false;

            try
            {
                result = this.InternalRun(new(runType));
            }
            catch (Exception ex)
            {
                result = StateResult.Terminate;
                this.BigMachine.ReportException(new(this, ex));
                // command?.SetException(ex);
            }

            if (result == StateResult.Terminate)
            {
                this.LastRun = now;
                this.RunType = RunType.NotRunning;
                this.Group.TryRemoveMachine(this.Identifier);
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
        /// Set the timeout of the machine.<br/>
        /// The time decreases while the program is running, and the machine will run when it reaches zero.
        /// </summary>
        /// <param name="timeSpan">The timeout.</param>
        /// <param name="absoluteDateTime">Set true to specify the next execution time by adding the current time and timeout.</param>
        protected void SetTimeout(TimeSpan timeSpan, bool absoluteDateTime = false)
        {
            this.RequestRerun = false;
            if (timeSpan.Ticks < 0)
            {
                Volatile.Write(ref this.Timeout, long.MaxValue);
                this.NextRun = default;
                return;
            }

            if (absoluteDateTime)
            {
                this.NextRun = DateTime.UtcNow + timeSpan;
            }
            else
            {
                Volatile.Write(ref this.Timeout, timeSpan.Ticks);
            }
        }

        /// <summary>
        /// Set the lifespen of the machine.<br/>
        /// The lifespan decreases while the program is running, and the machine will terminate when it reaches zero.
        /// </summary>
        /// <param name="timeSpan">The lifespan.</param>
        /// <param name="absoluteDateTime">Set true to specify the terminate time by adding the current time and lifespan.</param>
        protected void SetLifespan(TimeSpan timeSpan, bool absoluteDateTime = false)
        {
            if (timeSpan.Ticks < 0)
            {
                Volatile.Write(ref this.Lifespan, long.MaxValue);
                this.TerminationDate = DateTime.MaxValue;
                return;
            }

            if (absoluteDateTime)
            {
                this.TerminationDate = DateTime.UtcNow + timeSpan;
            }
            else
            {
                Volatile.Write(ref this.Lifespan, timeSpan.Ticks);
            }
        }

        /// <summary>
        /// Set the timeout of the machine.<br/>
        /// The time decreases while the program is running, and the machine will run when it reaches zero.
        /// </summary>
        /// <param name="seconds">The timeout in seconds.</param>
        /// <param name="absoluteDateTime">Set true to specify the next execution time by adding the current time and timeout.</param>
        protected void SetTimeout(double seconds, bool absoluteDateTime = false) => this.SetTimeout(TimeSpan.FromSeconds(seconds), absoluteDateTime);
    }
}
