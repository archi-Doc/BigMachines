// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    /// Represents a machine class which is a base class for all machine classes.<br/>
    /// </summary>
    /// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
    [TinyhandObject]
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
            this.IntInitState();
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
        /// Gets or sets the identifier of this machine.<br/>
        /// TIdentifier type must have <see cref="TinyhandObjectAttribute"/>.
        /// </summary>
        [Key(0)] // Key(0) is Machine.CurrentState
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
        /// Gets or sets the default time interval at which the machine will run.
        /// </summary>
        [Key(3)]
        public TimeSpan DefaultTimeout { get; protected internal set; }

        /// <summary>
        /// The time until the machine is executed.
        /// </summary>
        [Key(4)]
        protected internal long Timeout = long.MaxValue; // TimeSpan.Ticks (for interlocked)

        /// <summary>
        /// Gets or sets <see cref="DateTime"/> when the machine is executed last time.
        /// </summary>
        [Key(5)]
        public DateTime LastRun { get; protected internal set; }

        /// <summary>
        /// Gets or sets <see cref="DateTime"/> when the machine is will be executed.
        /// </summary>
        [Key(6)]
        public DateTime NextRun { get; protected internal set; }

        /// <summary>
        /// The lifespan of this machine. When this value reaches 0, the machine is terminated.
        /// </summary>
        [Key(7)]
        protected internal long Lifespan = long.MaxValue; // TimeSpan.Ticks (for interlocked)

        /// <summary>
        /// Gets or sets <see cref="DateTime"/> when the machine will be automatically terminated.
        /// </summary>
        [Key(8)]
        public DateTime TerminationDate { get; protected internal set; } = DateTime.MaxValue;

        /// <summary>
        /// Gets or sets a value indicating whether this machine is to be serialized.
        /// </summary>
        [IgnoreMember]
        public bool IsSerializable { get; protected set; } = false;

        /// <summary>
        /// Gets a TypeId of the machine.
        /// </summary>
        [IgnoreMember]
        public int TypeId { get; internal set; }

        /// <summary>
        /// Gets or sets ManMachineInterface.
        /// </summary>
        protected internal ManMachineInterface? InterfaceInstance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the state is changed in <see cref="RunInternal(StateParameter)"/>.
        /// </summary>
        protected internal bool StateChanged { get; set; }

        /// <summary>
        /// Expected to be implemented by the user.<br/>
        /// Receives commands and respond if necessary.<br/>
        /// Inside lock (machine) statement.
        /// </summary>
        /// <param name="command">The command.</param>
        protected internal virtual void ProcessCommand(CommandPost<TIdentifier>.Command command)
        {// Override
        }

        /// <summary>
        /// Expected to be implemented by the user.<br/>
        /// Set a parameter for the machine after the instance is created.<br/>
        /// Note that this method is called when the instance is created, but not called during deserialization.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        protected internal virtual void SetParameter(object? parameter)
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
        protected internal virtual StateResult RunInternal(StateParameter parameter)
        {// Generated
            return StateResult.Terminate;
        }

        /// <summary>
        /// Generated method which is called when the state changes.
        /// </summary>
        /// <param name="state">The next state.</param>
        /// <returns><see langword="true"/>: State changed. <see langword="false"/>: Not changed (same state or denied). </returns>
        protected internal virtual bool IntChangeState(int state) => false;

        /// <summary>
        /// Generated method which is called to initialize the state.
        /// </summary>
        protected internal virtual void IntInitState() => this.CurrentState = 0;

        /// <summary>
        /// Receivea a command and invoke the appropriate method.<br/>
        /// Inside lock (machine) statement.
        /// </summary>
        /// <param name="command">Command.</param>
        /// <returns><see langword="true"/>: Terminated, <see langword="false"/>: Continue.</returns>
        protected internal virtual bool DistributeCommand(CommandPost<TIdentifier>.Command command)
        {// lock (machine)
            if (this.Status == MachineStatus.Terminated)
            {// Terminated
                return true;
            }
            else if (command.Type == CommandPost<TIdentifier>.CommandType.Run ||
                command.Type == CommandPost<TIdentifier>.CommandType.RunTwoWay)
            {// Run
                var result = this.RunInternal(new(RunType.RunManual, command.Message));
                this.LastRun = DateTime.UtcNow;
                command.Response = result;
                if (result == StateResult.Terminate)
                {
                    return true;
                }
            }
            else if ((command.Type == CommandPost<TIdentifier>.CommandType.State ||
                command.Type == CommandPost<TIdentifier>.CommandType.StateTwoWay) &&
                command.Message is int state)
            {// ChangeState
                command.Response = this.IntChangeState(state);
            }
            else
            {// Command
                this.ProcessCommand(command);
            }

            return false;
        }

        /// <summary>
        /// Called when the machine is terminating.<br/>
        /// Inside lock (machine) statement.
        /// </summary>
        protected internal virtual void TerminateInternal()
        {
            this.Status = MachineStatus.Terminated;
        }

        /// <summary>
        /// Set the timeout of the machine.<br/>
        /// The time decreases while the program is running, and the machine will run when it reaches zero.
        /// </summary>
        /// <param name="timeSpan">The timeout.</param>
        /// <param name="absoluteDateTime">Set true to specify the next execution time by adding the current time and timeout.</param>
        protected void SetTimeout(TimeSpan timeSpan, bool absoluteDateTime = false)
        {
            this.StateChanged = false;
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
