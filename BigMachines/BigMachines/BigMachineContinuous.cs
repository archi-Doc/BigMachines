// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

        public class Info
        {
            internal Info(Item item)
            {
                this.Interface = item.Machine.InterfaceInstance!;
                this.Running = item.Core != null;
            }

            public ManMachineInterface<TIdentifier> Interface { get; }

            public bool Running { get; }
        }

        internal class Core : ThreadCore
        {
            public static void Process(object? parameter)
            {
                var core = (Core)parameter!;
                var item = core.Item;
                var machine = item.Machine;

                while (!core.IsTerminated)
                {
                    lock (machine.SyncMachine)
                    {
                        if (machine.RunMachine(null, RunType.Continuous, DateTime.UtcNow) == StateResult.Terminate)
                        {// Terminated
                            break;
                        }
                    }
                }
            }

            public Core(ThreadCoreBase parent, Item item)
                : base(parent, Process, false)
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
            // this.CoreGroup = new ThreadCoreGroup(this.BigMachine.Core);
            this.maxThreads = DefaultMaxThreads;
        }

        public Info[] GetInfo(bool running)
        {
            lock (this.syncContinuous)
            {
                var count = running ? this.items.Count(a => a.Core != null) : this.items.Count;
                var array = new Info[count];
                var e = running ? this.items.Where(a => a.Core != null) : this.items;
                var n = 0;
                foreach (var x in e)
                {
                    var info = new Info(x);
                    array[n++] = info;
                }

                return array;
            }
        }

        /* /// <summary>
        /// Sends a command to each continuous machine.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="running">Sends message to running machines only.</param>
        /// <param name="message">The message.</param>
        public void Command<TMessage>(bool running, TMessage message)
        {
            (var groups, var identifiers) = this.GetGroupsAndIdentifiers(running);
            this.BigMachine.CommandPost.SendGroupsAsync(groups, CommandPost<TIdentifier>.CommandType.Command, identifiers, message);
        }*/

        /// <summary>
        /// Sets the maximum number of threads used for continuous machines.<br/>
        /// The default value is 2 (<see cref="DefaultMaxThreads"/>).
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

        // public ThreadCoreGroup CoreGroup { get; }

        internal bool AddMachine(Machine<TIdentifier> machine)
        {
            lock (this.syncContinuous)
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
            lock (this.syncContinuous)
            {
                var item = this.items.FirstOrDefault(a => a.Machine == machine);
                if (item == null)
                {// Not found
                    return false;
                }

                this.items.Remove(item);

                if (item.Core != null)
                {
                    item.Core.Terminate(); // Terminate if the task is running.
                    this.cores.Remove(item.Core);
                    item.Core = null;
                }
            }

            return true;
        }

        internal void Process()
        {
            while (true)
            {
                lock (this.syncContinuous)
                {
                    if (this.cores.Count >= this.maxThreads)
                    {
                        return;
                    }

                    var i = this.items.FirstOrDefault(a => a.Core == null);
                    if (i == null)
                    {// No item
                        return;
                    }

                    i.Core = new Core(this.BigMachine.Core, i); // new Core(this.CoreGroup, i);
                    this.cores.Add(i.Core);
                    i.Core.Start();
                }
            }
        }

        private object syncContinuous = new();
        private int maxThreads;
        private List<Core> cores = new();
        private LinkedList<Item> items = new();

        private (IMachineGroup<TIdentifier>[], TIdentifier[]) GetGroupsAndIdentifiers(bool running)
        {
            lock (this.syncContinuous)
            {
                var e = running ? this.items.Where(a => a.Core != null) : this.items;
                var count = e.Count();

                var identifiers = new TIdentifier[count];
                var groups = new IMachineGroup<TIdentifier>[count];
                int n = 0;
                foreach (var x in e)
                {
                    identifiers[n] = x.Machine.Identifier;
                    groups[n] = x.Machine.Group;
                    n++;
                }

                return (groups, identifiers);
            }
        }
    }
}
