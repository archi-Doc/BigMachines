// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tinyhand;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines
{
    [TinyhandObject]
    public abstract class MachineBase<TIdentifier>
        where TIdentifier : notnull
    {
        public MachineBase(BigMachine<TIdentifier> bigMachine)
        {
            this.BigMachine = bigMachine;
        }

        public BigMachine<TIdentifier> BigMachine { get; }

        [Key(0)]
        public TIdentifier Identifier { get; protected set; } = default!;

        [IgnoreMember]
        public bool IsTerminated { get; protected set; } = false;

        [IgnoreMember]
        public bool IsSerializable { get; protected set; } = false;

        protected internal ManMachineInterface? InterfaceInstance { get; set; }

        protected internal virtual void ProcessCommand(CommandPost<TIdentifier>.Command command)
        {// Custom
        }

        protected internal virtual void InitializeAndIsolate(object? parameter)
        {// Custom
        }

        protected internal virtual void CreateInterface(TIdentifier identifier)
        {// Generated
            throw new NotImplementedException();
        }

        protected internal virtual StateResult RunInternal()
        {// Generated
            return StateResult.Terminate;
        }

        protected internal virtual void DistributeCommand(CommandPost<TIdentifier>.Command command)
        {// Implemented in Machine<TIdentifier, TState>
        }

        protected void SetTimeout(int millisecond)
        {
        }
    }
}
