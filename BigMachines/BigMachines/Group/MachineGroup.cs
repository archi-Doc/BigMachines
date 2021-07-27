// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

        public void CommandGroup<TMessage>(TMessage message) => this.BigMachine.CommandPost.SendGroup(CommandPost<TIdentifier>.CommandType.Command, this, this.IdentificationToMachine.Keys, message);

        public KeyValuePair<TIdentifier, TResponse>[] CommandGroupTwoWay<TMessage, TResponse>(TMessage message, int millisecondTimeout = 100) => this.BigMachine.CommandPost.SendGroupTwoWay<TMessage, TResponse>(CommandPost<TIdentifier>.CommandType.CommandTwoWay, this, this.IdentificationToMachine.Keys, message);

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
                lock (machineToRemove)
                {
                    machineToRemove.TerminateInternal();
                }
            }
        }

        bool IMachineGroup<TIdentifier>.TryGetMachine(TIdentifier identifier, [MaybeNullWhen(false)] out Machine<TIdentifier> machine) => this.IdentificationToMachine.TryGetValue(identifier, out machine);

        bool IMachineGroup<TIdentifier>.TryRemoveMachine(TIdentifier identifier)
        {
            if (this.IdentificationToMachine.TryRemove(identifier, out var machine))
            {
                lock (machine)
                {
                    machine.TerminateInternal();
                }

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
