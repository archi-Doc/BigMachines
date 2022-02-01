// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1108 // Block statements should not contain embedded comments

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

        public struct Enumerator2 : IEnumerable<TIdentifier>
        {
            internal Enumerator2(MachineSingle<TIdentifier> group)
            {
                this.group = group;
            }

            public IEnumerator<TIdentifier> GetEnumerator()
            {
                var m = Volatile.Read(ref this.group.machine1);
                if (m != null)
                {
                    yield return m.Identifier;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            private MachineSingle<TIdentifier> group;
        }

        public IEnumerable<TIdentifier> GetIdentifiers() => new Enumerator2(this);

        public struct Enumerator : IEnumerable<Machine<TIdentifier>>
        {
            internal Enumerator(MachineSingle<TIdentifier> group)
            {
                this.group = group;
            }

            public IEnumerator<Machine<TIdentifier>> GetEnumerator()
            {
                var m = Volatile.Read(ref this.group.machine1);
                if (m != null)
                {
                    yield return m;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            private MachineSingle<TIdentifier> group;
        }

        IEnumerable<Machine<TIdentifier>> IMachineGroup<TIdentifier>.GetMachines() => new Enumerator(this);

        public Task CommandAsync<TCommand, TMessage>(TCommand command, TMessage message)
            where TCommand : struct
        {
            var m = Volatile.Read(ref this.machine1);
            if (m != null)
            {
                return this.BigMachine.CommandPost.SendAsync(this, CommandPost<TIdentifier>.CommandType.Command, m.Identifier, Unsafe.As<TCommand, int>(ref command), message);
            }

            return Task.CompletedTask;
        }

        public Task<KeyValuePair<TIdentifier, TResponse?>[]> CommandAndReceiveAsync<TCommand, TMessage, TResponse>(TCommand command, TMessage message)
            where TCommand : struct
        {
            var m = Volatile.Read(ref this.machine1);
            if (m != null)
            {
                return this.BigMachine.CommandPost.SendAndReceiveGroupAsync<TMessage, TResponse>(this, CommandPost<TIdentifier>.CommandType.Command, new[] { m.Identifier }, Unsafe.As<TCommand, int>(ref command), message);
            }

            return Task.FromResult(Array.Empty<KeyValuePair<TIdentifier, TResponse?>>());
        }

        public BigMachine<TIdentifier> BigMachine { get; }

        public MachineInfo<TIdentifier> Info { get; private set; }

        public int Count => this.machine1 == null ? 0 : 1;

        public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
            where TMachineInterface : ManMachineInterface<TIdentifier>
        {
            var m = Volatile.Read(ref this.machine1);
            if (m != null) // && Comparer<TIdentifier>.Default.Compare(m.Identifier, identifier) == 0)
            {
                return m.InterfaceInstance as TMachineInterface;
            }
            else
            {
                return null;
            }
        }

        public TMachineInterface? TryGet<TMachineInterface>()
            where TMachineInterface : ManMachineInterface<TIdentifier>
        {
            var m = Volatile.Read(ref this.machine1);
            if (m != null)
            {
                return m.InterfaceInstance as TMachineInterface;
            }
            else
            {
                return null;
            }
        }

        void IMachineGroup<TIdentifier>.AddMachine(TIdentifier identifier, Machine<TIdentifier> machine)
        {
            var m = Interlocked.Exchange(ref this.machine1, machine);
            if (m != null)
            {
                m.TaskRunAndTerminate();
            }
        }

        void IMachineGroup<TIdentifier>.Assign(MachineInfo<TIdentifier> info)
        {
            this.Info = info;
        }

        Machine<TIdentifier> IMachineGroup<TIdentifier>.GetOrAddMachine(TIdentifier identifier, Machine<TIdentifier> machine)
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

        bool IMachineGroup<TIdentifier>.TryGetMachine(TIdentifier identifier, [MaybeNullWhen(false)] out Machine<TIdentifier> machine)
        {
            var m = Volatile.Read(ref this.machine1);
            if (m != null) // && EqualityComparer<TIdentifier>.Default.Equals(m.Identifier, identifier))
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

        bool IMachineGroup<TIdentifier>.RemoveFromGroup(Machine<TIdentifier> machine)
        {
            var m = Volatile.Read(ref this.machine1);
            if (m == machine)
            {
                Volatile.Write(ref this.machine1, null);
                return true;
            }
            else
            {
                return false;
            }
        }

        private Machine<TIdentifier>? machine1;
    }
}
