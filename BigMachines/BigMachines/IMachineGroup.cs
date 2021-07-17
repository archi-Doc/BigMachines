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
            where TMachineInterface : ManMachineInterface;

        public void CommandGroup<TMessage>(TMessage message);

        public KeyValuePair<TIdentifier, TResponse>[] CommandGroupTwoWay<TMessage, TResponse>(TMessage message, int millisecondTimeout = 100);

        public BigMachine<TIdentifier> BigMachine { get; }

        public MachineInfo<TIdentifier> Info { get; }

        internal void Assign(MachineInfo<TIdentifier> info);

        internal bool TryGetMachine(TIdentifier identifier, [MaybeNullWhen(false)] out MachineBase<TIdentifier> machine);

        internal MachineBase<TIdentifier> GetOrAddMachine(TIdentifier identifier, MachineBase<TIdentifier> machine);

        internal void AddMachine(TIdentifier identifier, MachineBase<TIdentifier> machine);

        internal bool TryRemoveMachine(TIdentifier identifier);

        internal ICollection<MachineBase<TIdentifier>> Machines { get; }
    }
}
