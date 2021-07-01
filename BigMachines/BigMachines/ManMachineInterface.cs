// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;

namespace BigMachines
{
    public struct ManMachineInterface<TIdentifier, TState> // maybe class
        where TIdentifier : notnull
    {
        public BigMachine<TIdentifier> BigMachine { get; }

        public TIdentifier Identifier { get; }

        internal ManMachineInterface(BigMachine<TIdentifier> bigMachine, TIdentifier identifier)
        {
            this.BigMachine = bigMachine;
            this.Identifier = TinyhandSerializer.Clone(identifier);
        }

        public void Send<TMessage>(TIdentifier identifier, TMessage message)
        {
            this.BigMachine.CommandPost.Send(identifier, message);
        }

        public TResponse? SendTwoWay<TMessage, TResponse>(TIdentifier identifier, TMessage message, int millisecondTimeout = 100)
        {
            return this.BigMachine.CommandPost.SendTwoWay<TMessage, TResponse>(identifier, message, millisecondTimeout);
        }

        public void ChangeState(TIdentifier identifier, TState state) => this.Send(this.Identifier, state);

        public bool ChangeStateTwoWay(TIdentifier identifier, TState state, int millisecondTimeout = 100) => this.SendTwoWay<TState, bool>(this.Identifier, state, millisecondTimeout);
    }
}
