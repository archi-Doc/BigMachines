// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
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
            if (!this.BigMachine.MachineTypeToGroup.TryGetValue(this.GetType(), out var group))
            {
                throw new InvalidOperationException($"Machine type {this.GetType().FullName} is not registered.");
            }

            this.Group = group;
            this.TypeId = group.Info.TypeId;
        }

        public BigMachine<TIdentifier> BigMachine { get; }

        public BigMachine<TIdentifier>.Group Group { get; }

        [Key(1)] // Key(0) is Machine.CurrentState
        public TIdentifier Identifier { get; protected set; } = default!;

        [Key(2)]
        public MachineStatus Status { get; protected internal set; } = MachineStatus.Running;

        [Key(3)]
        public TimeSpan DefaultTimeout { get; protected internal set; }

#pragma warning disable SA1401
        [Key(4)]
        internal long Timeout = long.MaxValue; // TimeSpan.Ticks (for interlocked)
#pragma warning restore SA1401

        [Key(5)]
        public DateTime LastRun { get; protected internal set; }

        [Key(6)]
        public DateTime NextRun { get; protected internal set; }

        [IgnoreMember]
        public bool IsSerializable { get; protected set; } = false;

        [IgnoreMember]
        public int TypeId { get; internal set; }

        protected internal ManMachineInterface? InterfaceInstance { get; set; }

        protected internal bool StateChanged { get; set; }

        protected internal virtual void ProcessCommand(CommandPost<TIdentifier>.Command command)
        {// Custom
        }

        protected internal virtual void SetParameter(object? parameter)
        {// Custom
        }

        protected internal virtual void CreateInterface(TIdentifier identifier)
        {// Generated
            throw new NotImplementedException();
        }

        protected internal virtual StateResult RunInternal(StateParameter parameter)
        {// Generated
            return StateResult.Terminate;
        }

        protected internal virtual bool DistributeCommand(CommandPost<TIdentifier>.Command command)
        {// Implemented in Machine<TIdentifier, TState>
            return true;
        }

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

        protected void SetTimeout(double seconds, bool absoluteDateTime = false) => this.SetTimeout(TimeSpan.FromSeconds(seconds), absoluteDateTime);
    }
}
