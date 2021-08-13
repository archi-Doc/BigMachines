﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arc.Threading;

namespace BigMachines
{
    /// <summary>
    /// Represents a manager class for continuous machines.
    /// </summary>
    /// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
    public partial class BigMachineContinuous<TIdentifier>
        where TIdentifier : notnull
    {
        public const int DefaultMaxThreads = 2;

        internal class Core : ThreadCore
        {
            static public void Process(object? parameter)
            {
                var core = (Core)parameter!;
                var item = core.Item;
                var machine = item.Machine;

                try
                {
                    var stateParameter = new StateParameter(RunType.Continuous);
                    while (!core.IsTerminated)
                    {
                        if (machine.RunInternal(stateParameter) == StateResult.Terminate)
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    item.Core = null;
                    machine.Group.TryRemoveMachine(machine.Identifier);
                    item.Continuous.RemoveCore(core);
                }
            }

            public Core(ThreadCoreBase parent, Item item)
                : base(parent, Process)
            {
                this.Item = item;
            }

            public Item Item { get; }
        }

        internal class Item
        {
            public Item(BigMachineContinuous<TIdentifier> continuous, Machine<TIdentifier> machine)
            {
                this.Continuous = continuous;
                this.Machine = machine;
            }

            public BigMachineContinuous<TIdentifier> Continuous { get; }

            public Machine<TIdentifier> Machine { get; }

            public Core? Core { get; set; }
        }

        public BigMachineContinuous(BigMachine<TIdentifier> bigMachine)
        {
            this.BigMachine = bigMachine;
            this.CoreGroup = new ThreadCoreGroup(this.BigMachine.Core);
            this.maxThreads = DefaultMaxThreads;
        }

        /// <summary>
        /// Sets the maximum number of threads used for continuous machines.
        /// </summary>
        /// <param name="numberOfThreads">The maximum number of threads.</param>
        public void SetMaxThreads(int numberOfThreads)
        {
            if (numberOfThreads <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfThreads));
            }

            this.maxThreads = numberOfThreads;
        }

        public BigMachine<TIdentifier> BigMachine { get; }

        public ThreadCoreGroup CoreGroup { get; }

        internal bool AddMachine(Machine<TIdentifier> machine)
        {
            lock (this.items)
            {
                if (this.items.Any(a => a.Machine == machine))
                {// Already exists.
                    return false;
                }

                var item = new Item(this, machine);
                this.items.AddLast(item);
            }

            return true;
        }

        internal bool RemoveMachine(Machine<TIdentifier> machine)
        {
            lock (this.items)
            {
                var first = this.items.FirstOrDefault(a => a.Machine == machine);
                if (first == null)
                {// Not found
                    return false;
                }

                first.Core?.Terminate(); // Terminate if the task is running.
                this.items.Remove(first);
            }

            return true;
        }

        internal bool RemoveCore(Core core)
        {
            lock (this.cores)
            {
                var first = this.cores.FirstOrDefault(a => a == core);
                if (first == null)
                {// Not found
                    return false;
                }

                this.cores.Remove(first);
            }

            return true;
        }

        internal void Process()
        {
            while (true)
            {
                lock (this.cores)
                {
                    if (this.cores.Count >= this.maxThreads)
                    {
                        return;
                    }

                    lock (this.items)
                    {
                        var i = this.items.FirstOrDefault(a => a.Core == null);
                        if (i != null)
                        {
                            i.Core = new Core(this.CoreGroup, i);
                            this.cores.Add(i.Core);
                        }
                    }
                }
            }
        }

        private int maxThreads;
        private List<Core> cores = new();
        private LinkedList<Item> items = new();
    }
}