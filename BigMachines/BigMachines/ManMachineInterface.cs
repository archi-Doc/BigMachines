// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;

namespace BigMachines
{
    /// <summary>
    /// Base class of <see cref="ManMachineInterface{TIdentifier, TState}"/>.
    /// </summary>
    /// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
    public abstract class ManMachineInterface<TIdentifier>
        where TIdentifier : notnull
    {
    }

    /// <summary>
    /// Class for operating a machine.<br/>
    /// To achieve lock-free operation, you need to use <see cref="ManMachineInterface{TIdentifier, TState}"/> instead of using machines directly.
    /// </summary>
    /// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
    /// <typeparam name="TState">The type of machine state.</typeparam>
    public abstract class ManMachineInterface<TIdentifier, TState> : ManMachineInterface<TIdentifier>
        where TIdentifier : notnull
        where TState : struct
    {
        /// <summary>
        /// Gets <see cref="BigMachine{TIdentifier}"/> instance.
        /// </summary>
        public BigMachine<TIdentifier> BigMachine { get; }

        /// <summary>
        /// Gets <see cref="MachineGroup{TIdentifier}"/> instance.
        /// </summary>
        public IMachineGroup<TIdentifier> Group { get; }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public TIdentifier Identifier { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManMachineInterface{TIdentifier, TState}"/> class.
        /// </summary>
        /// <param name="group">The group to which the machine belongs.</param>
        /// <param name="identifier">The identifier.</param>
        public ManMachineInterface(IMachineGroup<TIdentifier> group, TIdentifier identifier)
        {
            this.BigMachine = group.BigMachine;
            this.Group = group;
            this.Identifier = TinyhandSerializer.Clone(identifier);
        }

        /// <summary>
        /// Get the current state of the machine.
        /// </summary>
        /// <returns>The state of the machine..<br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public TState? GetCurrentState()
        {
            if (this.Group.TryGetMachine(this.Identifier, out var machine))
            {
                if (machine is Machine<TIdentifier> m && m.Status != MachineStatus.Terminated)
                {
                    return System.Runtime.CompilerServices.Unsafe.As<int, TState>(ref m.CurrentState);
                }
            }

            return null;
        }

        /// <summary>
        /// Get the status of the machine.
        /// </summary>
        /// <returns>The status of the machine.<br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public MachineStatus? GetMachineStatus()
        {
            if (this.Group.TryGetMachine(this.Identifier, out var machine))
            {
                return machine.Status;
            }

            return null;
        }

        /// <summary>
        /// Change the status of the machine.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns><see langword="true"/>: The status is successfully changed.<br/>
        /// <see langword="false"/>: Machine is not available.</returns>
        public bool SetMachineStatus(MachineStatus status)
        {
            if (this.Group.TryGetMachine(this.Identifier, out var machine))
            {
                machine.Status = status;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Runs the machine manually.<br/>
        /// This function does not change <see cref="Machine{TIdentifier}.Timeout"/> or <see cref="Machine{TIdentifier}.NextRun"/>.
        /// </summary>
        public void Run() => this.BigMachine.CommandPost.Send(CommandPost<TIdentifier>.CommandType.Run, this.Group, this.Identifier, 0);

        /// <summary>
        /// Runs the machine manually and receives the result.<br/>
        /// This function does not change <see cref="Machine{TIdentifier}.Timeout"/> or <see cref="Machine{TIdentifier}.NextRun"/>.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">Message.</param>
        /// <param name="millisecondTimeout">Timeout in milliseconds.</param>
        /// <returns>The result.</returns>
        public StateResult? RunTwoWay<TMessage>(TMessage message, int millisecondTimeout = 100) => this.BigMachine.CommandPost.SendTwoWay<TMessage, StateResult>(CommandPost<TIdentifier>.CommandType.RunTwoWay, this.Group, this.Identifier, message, millisecondTimeout);

        /// <summary>
        /// Sends a message to the machine.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">Message.</param>
        public void Command<TMessage>(TMessage message) => this.BigMachine.CommandPost.Send(CommandPost<TIdentifier>.CommandType.Command, this.Group, this.Identifier, message);

        /// <summary>
        /// Sends a message to the machine and receives the result.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="message">Message.</param>
        /// <param name="millisecondTimeout">Timeout in milliseconds.</param>
        /// <returns>The response.</returns>
        public TResponse? CommandTwoWay<TMessage, TResponse>(TMessage message, int millisecondTimeout = 100) => this.BigMachine.CommandPost.SendTwoWay<TMessage, TResponse>(CommandPost<TIdentifier>.CommandType.CommandTwoWay, this.Group, this.Identifier, message, millisecondTimeout);

        /// <summary>
        /// Try to change the state of the machine.
        /// </summary>
        /// <param name="state">The next machine state.</param>
        public void ChangeState(TState state) => this.BigMachine.CommandPost.Send(CommandPost<TIdentifier>.CommandType.State, this.Group, this.Identifier, state);

        /// <summary>
        /// Try to change the state of the machine.
        /// </summary>
        /// <param name="state">The next machine state.</param>
        /// <param name="millisecondTimeout">Timeout in milliseconds.</param>
        /// <returns><see langword="true"/>: The state is successfully changed.<br/>
        /// <see langword="false"/>: Not changed (change denied or the machine is not available.)</returns>
        public bool ChangeStateTwoWay(TState state, int millisecondTimeout = 100) => this.BigMachine.CommandPost.SendTwoWay<TState, bool>(CommandPost<TIdentifier>.CommandType.StateTwoWay, this.Group, this.Identifier, state, millisecondTimeout);
    }
}
