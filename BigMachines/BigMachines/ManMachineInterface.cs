// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;

namespace BigMachines
{
    public abstract class ManMachineInterface
    {
    }

    public abstract class ManMachineInterface<TIdentifier, TState> : ManMachineInterface
        where TIdentifier : notnull
        where TState : struct
    {
        public BigMachine<TIdentifier> BigMachine { get; }

        public BigMachine<TIdentifier>.Group Group { get; }

        public TIdentifier Identifier { get; }

        public ManMachineInterface(BigMachine<TIdentifier>.Group group, TIdentifier identifier)
        {
            this.BigMachine = group.BigMachine;
            this.Group = group;
            this.Identifier = TinyhandSerializer.Clone(identifier);
        }

        public TState? GetCurrentState()
        {
            if (this.Group.IdentificationToMachine.TryGetValue(this.Identifier, out var machine))
            {
                if (machine is Machine<TIdentifier, TState> m && m.Status != MachineStatus.Terminated)
                {
                    return m.CurrentState;
                }
            }

            return null;
        }

        public MachineStatus? GetMachineStatus()
        {
            if (this.Group.IdentificationToMachine.TryGetValue(this.Identifier, out var machine))
            {
                return machine.Status;
            }

            return null;
        }

        public bool SetMachineStatus(MachineStatus status)
        {
            if (this.Group.IdentificationToMachine.TryGetValue(this.Identifier, out var machine))
            {
                machine.Status = status;
                return true;
            }

            return false;
        }

        public void Command<TMessage>(TIdentifier identifier, TMessage message)
        {
            this.BigMachine.CommandPost.Send(this.Group, identifier, message);
        }

        public TResponse? CommandTwoWay<TMessage, TResponse>(TIdentifier identifier, TMessage message, int millisecondTimeout = 100)
        {
            return this.BigMachine.CommandPost.SendTwoWay<TMessage, TResponse>(this.Group, identifier, message, millisecondTimeout);
        }

        public void ChangeState(TState state) => this.Command(this.Identifier, state);

        public bool ChangeStateTwoWay(TState state, int millisecondTimeout = 100) => this.CommandTwoWay<TState, bool>(this.Identifier, state, millisecondTimeout);
    }
}
