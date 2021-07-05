// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;

namespace BigMachines
{
    public abstract class ManMachineInterface
    {
        public static object? NewInstance() => throw new NotImplementedException();
    }

    public abstract class ManMachineInterface<TIdentifier, TState> : ManMachineInterface
        where TIdentifier : notnull
        where TState : struct
    {
        public BigMachine<TIdentifier> BigMachine { get; }

        public TIdentifier Identifier { get; }

        public ManMachineInterface(BigMachine<TIdentifier> bigMachine, TIdentifier identifier)
        {
            this.BigMachine = bigMachine;
            this.Identifier = TinyhandSerializer.Clone(identifier);
        }

        public TState? GetCurrentState()
        {
            if (this.BigMachine.IdentificationToMachine.TryGetValue(this.Identifier, out var machine))
            {
                if (machine is Machine<TIdentifier, TState> m)
                {
                    return m.CurrentState;
                }
            }

            return null;
        }

        public void Send<TMessage>(TIdentifier identifier, TMessage message)
        {
            this.BigMachine.CommandPost.Send(identifier, message);
        }

        public TResponse? SendTwoWay<TMessage, TResponse>(TIdentifier identifier, TMessage message, int millisecondTimeout = 100)
        {
            return this.BigMachine.CommandPost.SendTwoWay<TMessage, TResponse>(identifier, message, millisecondTimeout);
        }

        public void ChangeState(TState state) => this.Send(this.Identifier, state);

        public bool ChangeStateTwoWay(TState state, int millisecondTimeout = 100) => this.SendTwoWay<TState, bool>(this.Identifier, state, millisecondTimeout);
    }
}
