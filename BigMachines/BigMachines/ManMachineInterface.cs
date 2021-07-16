// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;

namespace BigMachines
{
    /// <summary>
    /// Base class for <see cref="ManMachineInterface{TIdentifier, TState}"/>.
    /// </summary>
    public abstract class ManMachineInterface
    {
    }

    public abstract class ManMachineInterface<TIdentifier, TState> : ManMachineInterface
        where TIdentifier : notnull
        where TState : struct
    {
        public BigMachine<TIdentifier> BigMachine { get; }

        public MachineGroup<TIdentifier> Group { get; }

        public TIdentifier Identifier { get; }

        public ManMachineInterface(MachineGroup<TIdentifier> group, TIdentifier identifier)
        {
            this.BigMachine = group.BigMachine;
            this.Group = group;
            this.Identifier = TinyhandSerializer.Clone(identifier);
        }

        public TState? GetCurrentState()
        {
            if (this.Group.TryGetMachine(this.Identifier, out var machine))
            {
                if (machine is Machine<TIdentifier, TState> m && m.Status != MachineStatus.Terminated)
                {
                    return m.CurrentState;
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
        /// Manually run the machine.<br/>
        /// This function does not change <see cref="MachineBase{TIdentifier}.Timeout"/> or <see cref="MachineBase{TIdentifier}.NextRun"/>.
        /// </summary>
        public void Run() => this.BigMachine.CommandPost.Send(CommandPost<TIdentifier>.CommandType.Run, this.Group, this.Identifier, 0);

        public StateResult? RunTwoWay<TMessage>(TMessage message, int millisecondTimeout = 100) => this.BigMachine.CommandPost.SendTwoWay<TMessage, StateResult>(CommandPost<TIdentifier>.CommandType.RunTwoWay, this.Group, this.Identifier, message, millisecondTimeout);

        public void Command<TMessage>(TMessage message) => this.BigMachine.CommandPost.Send(CommandPost<TIdentifier>.CommandType.Command, this.Group, this.Identifier, message);

        public TResponse? CommandTwoWay<TMessage, TResponse>(TMessage message, int millisecondTimeout = 100) => this.BigMachine.CommandPost.SendTwoWay<TMessage, TResponse>(CommandPost<TIdentifier>.CommandType.CommandTwoWay, this.Group, this.Identifier, message, millisecondTimeout);

        public void ChangeState(TState state) => this.BigMachine.CommandPost.Send(CommandPost<TIdentifier>.CommandType.State, this.Group, this.Identifier, state);

        public bool ChangeStateTwoWay(TState state, int millisecondTimeout = 100) => this.BigMachine.CommandPost.SendTwoWay<TState, bool>(CommandPost<TIdentifier>.CommandType.StateTwoWay, this.Group, this.Identifier, state, millisecondTimeout);
    }
}
