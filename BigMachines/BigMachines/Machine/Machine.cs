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
    /// <summary>
    /// Represents a machine class which is a base class for all machine classes.<br/>
    /// <see cref="Machine{TIdentifier, TState}"/> = <see cref="MachineBase{TIdentifier}"/> + State type.
    /// </summary>
    /// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
    /// <typeparam name="TState">The type of machine state.</typeparam>
    [TinyhandObject]
    public abstract class Machine<TIdentifier, TState> : MachineBase<TIdentifier>
        where TIdentifier : notnull
        where TState : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Machine{TIdentifier, TState}"/> class.
        /// </summary>
        /// <param name="bigMachine">BigMachine which contains an instance of this machine.</param>
        protected Machine(BigMachine<TIdentifier> bigMachine)
            : base(bigMachine)
        {
            this.CurrentState = default!;
        }

#pragma warning disable SA1401
        /// <summary>
        /// Gets or sets the current state of this machine.
        /// </summary>
        [Key(0)]
        protected internal int CurrentState;
#pragma warning restore SA1401

        /// <summary>
        /// Generated method which is called when the state changes.
        /// </summary>
        /// <param name="state">The next state.</param>
        /// <returns><see langword="true"/>: State changed. <see langword="false"/>: Not changed (same state or denied). </returns>
        protected internal virtual bool ChangeStateInternal(TState state) => false;

        /// <inheritdoc/>
        protected internal override bool DistributeCommand(CommandPost<TIdentifier>.Command command)
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
                command.Message is TState state)
            {// ChangeState
                command.Response = this.ChangeStateInternal(state);
            }
            else
            {// Command
                this.ProcessCommand(command);
            }

            return false;
        }
    }
}
