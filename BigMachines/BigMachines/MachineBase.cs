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

        protected bool IsSerializable => false;

        // public virtual Type GetStateType() => throw new NotImplementedException();

        internal void ProcessCommand(CommandPost<TIdentifier>.Command command) => throw new NotImplementedException();

        protected void SetTimeout(int millisecond)
        {
        }
    }
}
