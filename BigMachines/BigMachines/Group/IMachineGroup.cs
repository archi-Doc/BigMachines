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
    public interface IMachineGroup<TIdentifier>
        where TIdentifier : notnull
    {
        public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
            where TMachineInterface : ManMachineInterface<TIdentifier>;

        public IEnumerable<TIdentifier> GetIdentifiers();

        public void CommandGroup<TMessage>(TMessage message);

        public KeyValuePair<TIdentifier, TResponse?>[] CommandGroupTwoWay<TMessage, TResponse>(TMessage message, int millisecondTimeout = 100);

        public BigMachine<TIdentifier> BigMachine { get; }

        public MachineInfo<TIdentifier> Info { get; }

        public int Count { get; }

        internal void Assign(MachineInfo<TIdentifier> info);

        internal IEnumerable<Machine<TIdentifier>> GetMachines();

        internal bool TryGetMachine(TIdentifier identifier, [MaybeNullWhen(false)] out Machine<TIdentifier> machine);

        internal Machine<TIdentifier> GetOrAddMachine(TIdentifier identifier, Machine<TIdentifier> machine);

        internal void AddMachine(TIdentifier identifier, Machine<TIdentifier> machine);

        internal bool TryRemoveMachine(TIdentifier identifier);
    }
}
