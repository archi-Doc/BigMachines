// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BigMachines
{
    public class MachineGroup<TIdentifier> : IMachineGroup<TIdentifier>
        where TIdentifier : notnull
    {
        internal protected MachineGroup(BigMachine<TIdentifier> bigMachine)
        {
            this.BigMachine = bigMachine;
            this.Info = default!; // Must call Assign()
        }

        public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
            where TMachineInterface : ManMachineInterface<TIdentifier>
        {
            if (((IMachineGroup<TIdentifier>)this).TryGetMachine(identifier, out var machine))
            {
                return machine.InterfaceInstance as TMachineInterface;
            }

            return null;
        }

        public Task CommandAsync<TCommand, TMessage>(TCommand command, TMessage message)
            where TCommand : struct
        {
            return this.BigMachine.CommandPost.SendGroupAsync(this, CommandPost<TIdentifier>.CommandType.Command, this.IdentificationToMachine.Keys, Unsafe.As<TCommand, int>(ref command), message);
        }

        public Task<KeyValuePair<TIdentifier, TResponse?>[]> CommandAndReceiveAsync<TCommand, TMessage, TResponse>(TCommand command, TMessage message)
            where TCommand : struct
        {
            return this.BigMachine.CommandPost.SendAndReceiveGroupAsync<TMessage, TResponse>(this, CommandPost<TIdentifier>.CommandType.Command, this.IdentificationToMachine.Keys, Unsafe.As<TCommand, int>(ref command), message);
        }

        public BigMachine<TIdentifier> BigMachine { get; }

        public MachineInfo<TIdentifier> Info { get; private set; }

        public int Count => this.IdentificationToMachine.Count;

        public IEnumerable<TIdentifier> GetIdentifiers() => this.IdentificationToMachine.Keys;

        void IMachineGroup<TIdentifier>.Assign(MachineInfo<TIdentifier> info)
        {
            this.Info = info;
        }

        Machine<TIdentifier> IMachineGroup<TIdentifier>.GetOrAddMachine(TIdentifier identifier, Machine<TIdentifier> machine) => this.IdentificationToMachine.GetOrAdd(identifier, machine);

        void IMachineGroup<TIdentifier>.AddMachine(TIdentifier identifier, Machine<TIdentifier> machine)
        {
            Machine<TIdentifier>? machineToRemove = null;
            this.IdentificationToMachine.AddOrUpdate(identifier, x => machine, (i, m) =>
            {
                machineToRemove = m;
                return machine;
            });

            if (machineToRemove != null)
            {
                machineToRemove.TryTerminate();
                this.BigMachine.CommandPost.SendTerminate(this, machineToRemove.Identifier);
            }
        }

        bool IMachineGroup<TIdentifier>.TryGetMachine(TIdentifier identifier, [MaybeNullWhen(false)] out Machine<TIdentifier> machine) => this.IdentificationToMachine.TryGetValue(identifier, out machine);

        bool IMachineGroup<TIdentifier>.RemoveFromGroup(TIdentifier identifier)
        {
            if (this.IdentificationToMachine.TryRemove(identifier, out var machine))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        IEnumerable<Machine<TIdentifier>> IMachineGroup<TIdentifier>.GetMachines() => this.IdentificationToMachine.Values;

        protected ConcurrentDictionary<TIdentifier, Machine<TIdentifier>> IdentificationToMachine { get; } = new();
    }
}
