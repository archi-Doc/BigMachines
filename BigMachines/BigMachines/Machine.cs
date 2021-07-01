// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arc.Threading;
using Tinyhand;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines
{
    public abstract class Machine<TIdentifier, TState> : MachineBase<TIdentifier>
        where TIdentifier : notnull
        where TState : struct
    {
        public Machine(BigMachine<TIdentifier> bigMachine, TIdentifier identifier)
            : base(bigMachine, identifier)
        {
            this.CurrentState = default!;
        }

        public TState CurrentState { get; protected set; }

        protected virtual bool ChangeStateInternal(TState state) => false;

        protected virtual StateResult RunInternal() => StateResult.Terminate;
    }
}
