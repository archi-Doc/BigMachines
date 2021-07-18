// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BigMachines
{
    public class MachineSingle<TIdentifier> : IMachineGroup<TIdentifier>
        where TIdentifier : notnull
    {
        internal protected MachineSingle(BigMachine<TIdentifier> bigMachine)
        {
            this.BigMachine = bigMachine;
            this.Info = default!; // Must call Assign()
        }

        public struct Enumerator : IEnumerable<MachineBase<TIdentifier>>, IEnumerator<MachineBase<TIdentifier>>, IEnumerator
        {
            internal Enumerator(MachineSingle<TIdentifier> group)
            {
                this.group = group;
                this.current = default!;
            }

            public MachineBase<TIdentifier> Current => this.current;

            object System.Collections.IEnumerator.Current => this.current;

            public void Dispose()
            {
            }

            public IEnumerator<MachineBase<TIdentifier>> GetEnumerator() => this;

            public bool MoveNext()
            {
                if (this.current == null)
                {
                    this.current = Volatile.Read(ref this.group.machine1)!;
                    return this.current != null;
                }

                return false;
            }

            public void Reset()
            {
                this.current = default!;
            }

            IEnumerator IEnumerable.GetEnumerator() => this;

            private MachineSingle<TIdentifier> group;
            private MachineBase<TIdentifier> current;
        }

        IEnumerable<MachineBase<TIdentifier>> IMachineGroup<TIdentifier>.Machines => new Enumerator(this);

        public void CommandGroup<TMessage>(TMessage message)
        {
            var m = Volatile.Read(ref this.machine1);
            if (m != null)
            {
                this.BigMachine.CommandPost.Send(CommandPost<TIdentifier>.CommandType.Command, this, m.Identifier, message);
            }
        }

        public KeyValuePair<TIdentifier, TResponse>[] CommandGroupTwoWay<TMessage, TResponse>(TMessage message, int millisecondTimeout = 100)
        {
            var m = Volatile.Read(ref this.machine1);
            if (m != null)
            {
                var result = this.BigMachine.CommandPost.SendTwoWay<TMessage, TResponse>(CommandPost<TIdentifier>.CommandType.CommandTwoWay, this, m.Identifier, message, millisecondTimeout);
                if (result != null)
                {
                    return new[] { new KeyValuePair<TIdentifier, TResponse>(m.Identifier, result) };
                }
            }

            return Array.Empty<KeyValuePair<TIdentifier, TResponse>>();
        }

        public BigMachine<TIdentifier> BigMachine { get; }

        public MachineInfo<TIdentifier> Info { get; private set; }

        public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
            where TMachineInterface : ManMachineInterface
        {
            var m = Volatile.Read(ref this.machine1);
            if (m != null && Comparer<TIdentifier>.Default.Compare(m.Identifier, identifier) == 0)
            {
                return m.InterfaceInstance as TMachineInterface;
            }
            else
            {
                return null;
            }
        }

        void IMachineGroup<TIdentifier>.AddMachine(TIdentifier identifier, MachineBase<TIdentifier> machine)
        {
            Volatile.Write(ref this.machine1, machine);
        }

        void IMachineGroup<TIdentifier>.Assign(MachineInfo<TIdentifier> info)
        {
            this.Info = info;
        }

        MachineBase<TIdentifier> IMachineGroup<TIdentifier>.GetOrAddMachine(TIdentifier identifier, MachineBase<TIdentifier> machine)
        {
            var m = Volatile.Read(ref this.machine1);
            if (m == null)
            {
                Volatile.Write(ref this.machine1, machine);
                return machine;
            }
            else
            {
                return m;
            }
        }

        bool IMachineGroup<TIdentifier>.TryGetMachine(TIdentifier identifier, [MaybeNullWhen(false)] out MachineBase<TIdentifier> machine)
        {
            var m = Volatile.Read(ref this.machine1);
            if (m != null && Comparer<TIdentifier>.Default.Compare(m.Identifier, identifier) == 0)
            {
                machine = m;
                return true;
            }
            else
            {
                machine = null;
                return false;
            }
        }

        bool IMachineGroup<TIdentifier>.TryRemoveMachine(TIdentifier identifier)
        {
            var m = Volatile.Read(ref this.machine1);
            if (m != null && Comparer<TIdentifier>.Default.Compare(m.Identifier, identifier) == 0)
            {
                Volatile.Write(ref this.machine1, null);
                return true;
            }
            else
            {
                return false;
            }
        }

        private MachineBase<TIdentifier>? machine1;
    }
}
