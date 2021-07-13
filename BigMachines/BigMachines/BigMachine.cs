// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines.Internal;
using Tinyhand;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines
{
    public class BigMachine<TIdentifier> : IDisposable
        where TIdentifier : notnull
    {
        public class Group
        {
            internal Group(BigMachine<TIdentifier> bigMachine, GroupInfo groupInfo)
            {
                this.BigMachine = bigMachine;
                this.Info = groupInfo;
            }

            public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
                where TMachineInterface : ManMachineInterface
            {
                if (this.IdentificationToMachine.TryGetValue(identifier, out var machine))
                {
                    return machine.InterfaceInstance as TMachineInterface;
                }

                return null;
            }

            public void CommandGroup<TMessage>(TMessage message) => this.BigMachine.CommandPost.SendGroup(CommandPost<TIdentifier>.CommandType.Command, this, this.IdentificationToMachine.Keys, message);

            public KeyValuePair<TIdentifier, TResponse>[] CommandGroupTwoWay<TMessage, TResponse>(TMessage message, int millisecondTimeout = 100) => this.BigMachine.CommandPost.SendGroupTwoWay<TMessage, TResponse>(CommandPost<TIdentifier>.CommandType.CommandTwoWay, this, this.IdentificationToMachine.Keys, message);

            public BigMachine<TIdentifier> BigMachine { get; }

            public GroupInfo Info { get; }

            internal ConcurrentDictionary<TIdentifier, MachineBase<TIdentifier>> IdentificationToMachine { get; } = new();
        }

        public class GroupInfo
        {
            public GroupInfo(Type machineType, int typeId, Func<BigMachine<TIdentifier>, MachineBase<TIdentifier>>? constructor)
            {
                this.MachineType = machineType;
                this.TypeId = typeId;
                this.Constructor = constructor;
            }

            public Type MachineType { get; }

            public int TypeId { get; }

            public Func<BigMachine<TIdentifier>, MachineBase<TIdentifier>>? Constructor { get; }
        }

        public BigMachine(ThreadCoreBase parent, IServiceProvider? serviceProvider = null)
        {
            this.Status = new();
            this.Core = new ThreadCore(parent, this.MainLoop);
            this.CommandPost = new(parent);
            this.CommandPost.Open(this.DistributeCommand);
            this.ServiceProvider = serviceProvider;

            this.groupArray = new Group[StaticInfo.Count];
            var n = 0;
            foreach (var x in StaticInfo)
            {
                this.groupArray[n] = new Group(this, x.Value);
                this.interfaceTypeToGroup.TryAdd(x.Key, this.groupArray[n]);
            }

            foreach (var x in this.groupArray)
            {
                if (!this.TypeIdToGroup.ContainsKey(x.Info.TypeId))
                {
                    this.TypeIdToGroup.Add(x.Info.TypeId, x);
                }

                if (!this.MachineTypeToGroup.ContainsKey(x.Info.MachineType))
                {
                    this.MachineTypeToGroup.Add(x.Info.MachineType, x);
                }
            }
        }

        public static Dictionary<Type, GroupInfo> StaticInfo { get; } = new(); // typeof(Machine.Interface), MachineGroup

        public BigMachineStatus Status { get; }

        public ThreadCore Core { get; }

        public CommandPost<TIdentifier> CommandPost { get; }

        public IServiceProvider? ServiceProvider { get; }

        public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
            where TMachineInterface : ManMachineInterface
        {
            this.GetMachineGroup(typeof(TMachineInterface), out var group);
            return group.TryGet<TMachineInterface>(identifier);
        }

        public Group GetGroup<TMachineInterface>()
            where TMachineInterface : ManMachineInterface
        {
            this.GetMachineGroup(typeof(TMachineInterface), out var group);
            return group;
        }

        public TMachineInterface? TryCreate<TMachineInterface>(TIdentifier identifier, object? parameter = null)
            where TMachineInterface : ManMachineInterface
        {
            this.GetMachineGroup(typeof(TMachineInterface), out var group);

            MachineBase<TIdentifier>? machine = null;
            if (group.IdentificationToMachine.TryGetValue(identifier, out machine))
            {
                return machine.InterfaceInstance as TMachineInterface;
            }

            machine = this.CreateMachine(group);

            var clone = TinyhandSerializer.Clone(identifier);
            machine.CreateInterface(clone);
            machine.SetParameter(parameter);

            machine = group.IdentificationToMachine.GetOrAdd(clone, machine);
            return machine.InterfaceInstance as TMachineInterface;
        }

        public TMachineInterface Create<TMachineInterface>(TIdentifier identifier, object? parameter = null)
            where TMachineInterface : ManMachineInterface
        {
            this.GetMachineGroup(typeof(TMachineInterface), out var group);

            var machine = this.CreateMachine(group);

            var clone = TinyhandSerializer.Clone(identifier);
            machine.CreateInterface(clone);
            machine.SetParameter(parameter);

            MachineBase<TIdentifier>? machineToRemove = null;
            group.IdentificationToMachine.AddOrUpdate(clone, x => machine, (i, m) =>
            {
                machineToRemove = m;
                return machine;
            });

            if (machineToRemove != null)
            {
                this.RemoveMachine(group, identifier, machineToRemove);
            }

            return (TMachineInterface)machine.InterfaceInstance!;
        }

        public bool Remove<TMachineInterface>(TIdentifier identifier)
        {
            this.GetMachineGroup(typeof(TMachineInterface), out var group);

            if (group.IdentificationToMachine.TryGetValue(identifier, out var machine))
            {
                return this.RemoveMachine(group, identifier, machine);
            }

            return false;
        }

        public byte[] Serialize(TinyhandSerializerOptions? options = null)
        {
            options ??= TinyhandSerializer.DefaultOptions;
            var writer = default(Tinyhand.IO.TinyhandWriter);

            foreach (var x in this.groupArray)
            {
                SerializeGroup(ref writer, x);
            }

            void SerializeGroup(ref Tinyhand.IO.TinyhandWriter writer, Group info)
            {
                foreach (var machine in info.IdentificationToMachine.Values.Where(a => a.IsSerializable))
                {
                    if (machine is ITinyhandSerialize serializer)
                    {
                        lock (machine)
                        {
                            writer.WriteArrayHeader(2); // Header
                            writer.Write(machine.TypeId); // Id
                            serializer.Serialize(ref writer, options!); // Data
                        }
                    }
                }
            }

            return writer.FlushAndGetArray();
        }

        public void Deserialize(byte[] byteArray)
        {
            var reader = new Tinyhand.IO.TinyhandReader(byteArray);
            var options = TinyhandSerializer.DefaultOptions;

            while (!reader.End)
            {
                if (reader.TryReadNil())
                {
                    break;
                }

                if (reader.ReadArrayHeader() != 2)
                {
                    throw new TinyhandException("Invalid Union data was detected.");
                }

                if (reader.TryReadNil())
                {
                    reader.ReadNil();
                    continue;
                }

                var key = reader.ReadInt32();
                if (this.TypeIdToGroup.TryGetValue(key, out var group))
                {
                    var machine = this.CreateMachine(group);
                    if (machine is ITinyhandSerialize serializer)
                    {
                        serializer.Deserialize(ref reader, options);
                        machine.CreateInterface(machine.Identifier);
                        group.IdentificationToMachine[machine.Identifier] = machine;
                    }
                    else
                    {
                        reader.Skip();
                        continue;
                    }
                }
                else
                {
                    reader.Skip();
                    continue;
                }
            }
        }

        public void SetTimerInterval(TimeSpan interval)
        {
            this.timerInterval = interval;
        }

        internal Dictionary<int, Group> TypeIdToGroup { get; } = new();

        internal Dictionary<Type, Group> MachineTypeToGroup { get; } = new();

        private bool RemoveMachine(Group group, TIdentifier identifier, MachineBase<TIdentifier> machine)
        {
            lock (machine)
            {
                machine.Status = MachineStatus.Terminated;
                return group.IdentificationToMachine.TryRemove(identifier, out _);
            }
        }

        private void DistributeCommand(CommandPost<TIdentifier>.Command command)
        {
            if (command.Channel is Group group &&
                group.IdentificationToMachine.TryGetValue(command.Identifier, out var machine))
            {
                lock (machine)
                {
                    if (machine.DistributeCommand(command))
                    {
                        group.IdentificationToMachine.TryRemove(machine.Identifier, out _);
                    }
                }
            }
        }

        private MachineBase<TIdentifier> CreateMachine(Group group)
        {
            MachineBase<TIdentifier>? machine = null;

            if (this.ServiceProvider != null)
            {
                machine = this.ServiceProvider.GetService(group.Info.MachineType) as MachineBase<TIdentifier>;
            }

            if (machine == null)
            {
                if (group.Info.Constructor != null)
                {
                    machine = group.Info.Constructor(this);
                }
                else
                {
                    throw new InvalidOperationException("ServiceProvider is required to create an instance of class which does not have default constructor.");
                }
            }

            if (machine.DefaultTimeout != TimeSpan.Zero && machine.Timeout == long.MaxValue)
            {
                Volatile.Write(ref machine.Timeout, 0);
            }

            return machine;
        }

        private void MainLoop(object? parameter)
        {
            var core = (ThreadCore)parameter!;

            while (!core.IsTerminated)
            {
                if (!core.Wait(this.timerInterval, TimeSpan.FromMilliseconds(10)))
                {// Terminated
                    break;
                }

                var now = DateTime.UtcNow;
                if (this.Status.LastRun == default)
                {
                    this.Status.LastRun = now;
                }

                var elapsed = now - this.Status.LastRun;
                if (elapsed.Ticks < 0)
                {
                    elapsed = default;
                }

                foreach (var x in this.groupArray)
                {
                    foreach (var y in x.IdentificationToMachine.Values)
                    {
                        Interlocked.Add(ref y.Timeout, -elapsed.Ticks);
                        Interlocked.Add(ref y.Lifespan, -elapsed.Ticks);

                        if (y.Lifespan <= 0 || y.TerminationDate <= now)
                        {// Terminate
                            lock (y)
                            {
                                x.IdentificationToMachine.TryRemove(y.Identifier, out _);
                            }
                        }
                        else if (y.Timeout <= 0 || y.NextRun >= now)
                        {// Screening
                            lock (y)
                            {
                                if (TryRun(y))
                                {// Terminated
                                    x.IdentificationToMachine.TryRemove(y.Identifier, out _);
                                }
                            }
                        }
                    }
                }

                this.Status.LastRun = now;

                bool TryRun(MachineBase<TIdentifier> machine)
                {// locked
                    var runFlag = false;
                    if (machine.Timeout <= 0)
                    {// Timeout
                        if (machine.DefaultTimeout <= TimeSpan.Zero)
                        {
                            Volatile.Write(ref machine.Timeout, long.MinValue);
                        }
                        else
                        {
                            Volatile.Write(ref machine.Timeout, machine.DefaultTimeout.Ticks);
                        }

                        runFlag = true;
                    }

                    if (machine.NextRun >= now)
                    {
                        machine.NextRun = default;
                        runFlag = true;
                    }

                    if (runFlag)
                    {
StateChangedLoop:
                        machine.StateChanged = false;
                        var result = machine.RunInternal(new(RunType.RunTimer));
                        if (result == StateResult.Terminate)
                        {
                            return true;
                        }
                        else if (machine.StateChanged)
                        {
                            goto StateChangedLoop;
                        }

                        machine.LastRun = now;
                    }

                    return false;
                }
            }

            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetMachineGroup(Type interfaceType, out Group info)
        {
            if (!this.interfaceTypeToGroup.TryGetValue(interfaceType, out info!))
            {
                throw new InvalidOperationException($"Machine interface {interfaceType.FullName} is not registered.");
            }
        }

        private ThreadsafeTypeKeyHashTable<Group> interfaceTypeToGroup = new();
        private Group[] groupArray;
        private TimeSpan timerInterval = TimeSpan.FromMilliseconds(500);

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls.

        /// <summary>
        /// Finalizes an instance of the <see cref="BigMachine{TIdentifier}"/> class.
        /// </summary>
        ~BigMachine()
        {
            this.Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// free managed/native resources.
        /// </summary>
        /// <param name="disposing">true: free managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // free managed resources.
                    this.CommandPost.Dispose();
                }

                // free native resources here if there are any.
                this.disposed = true;
            }
        }
        #endregion
    }
}
