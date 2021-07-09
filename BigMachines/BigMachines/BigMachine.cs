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
        public class MachineGroup
        {
            public MachineGroup(Type machineType, int typeId, Func<BigMachine<TIdentifier>, MachineBase<TIdentifier>>? constructor)
            {
                this.MachineType = machineType;
                this.TypeId = typeId;
                this.Constructor = constructor;
            }

            public Type MachineType { get; }

            public int TypeId { get; }

            public Func<BigMachine<TIdentifier>, MachineBase<TIdentifier>>? Constructor { get; }

            internal ConcurrentDictionary<TIdentifier, MachineBase<TIdentifier>> IdentificationToMachine { get; } = new();
        }

        public BigMachine(ThreadCoreBase parent, IServiceProvider? serviceProvider = null)
        {
            this.Core = new ThreadCore(parent, this.MainLoop);
            this.CommandPost = new(parent);
            this.CommandPost.Open(this.DistributeCommand);
            this.ServiceProvider = serviceProvider;

            this.groupArray = StaticInfo.Values.ToArray();
            foreach (var x in StaticInfo)
            {
                if (!this.typeIdToInfo.ContainsKey(x.Value.TypeId))
                {
                    this.typeIdToInfo.Add(x.Value.TypeId, x.Value);
                }

                this.interfaceTypeToGroup.TryAdd(x.Key, x.Value);
            }
        }

        public static Dictionary<Type, MachineGroup> StaticInfo { get; } = new(); // typeof(Machine.Interface), MachineGroup

        public ThreadCore Core { get; }

        public CommandPost<TIdentifier> CommandPost { get; }

        public IServiceProvider? ServiceProvider { get; }

        public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
            where TMachineInterface : ManMachineInterface
        {
            this.GetMachineGroup(typeof(TMachineInterface), out var info);

            if (info.IdentificationToMachine.TryGetValue(identifier, out var machine))
            {
                return machine.InterfaceInstance as TMachineInterface;
            }

            return null;
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

            void SerializeGroup(ref Tinyhand.IO.TinyhandWriter writer, MachineGroup info)
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
                if (this.typeIdToInfo.TryGetValue(key, out var info))
                {
                    var machine = this.CreateMachine(info);
                    if (machine is ITinyhandSerialize serializer)
                    {
                        serializer.Deserialize(ref reader, options);
                        machine.CreateInterface(machine.Identifier);
                        info.IdentificationToMachine[machine.Identifier] = machine;
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

        private bool RemoveMachine(MachineGroup group, TIdentifier identifier, MachineBase<TIdentifier> machine)
        {
            lock (machine)
            {
                machine.Status = MachineStatus.Terminated;
                return group.IdentificationToMachine.TryRemove(identifier, out _);
            }
        }

        private void DistributeCommand(CommandPost<TIdentifier>.Command command)
        {
            if (this.IdentificationToMachine.TryGetValue(command.Identifier, out var machine))
            {
                lock (machine)
                {
                    machine.DistributeCommand(command);
                }
            }
        }

        private MachineBase<TIdentifier> CreateMachine(MachineGroup group)
        {
            MachineBase<TIdentifier>? machine = null;

            if (this.ServiceProvider != null)
            {
                machine = this.ServiceProvider.GetService(group.MachineType) as MachineBase<TIdentifier>;
            }

            if (machine == null)
            {
                if (group.Constructor != null)
                {
                    machine = group.Constructor(this);
                }
                else
                {
                    throw new InvalidOperationException("IServiceProvider is required to create an instance of class which does not have default constructor.");
                }
            }

            machine.TypeId = group.TypeId;
            return machine;
        }

        private void MainLoop(object? parameter)
        {
            var core = (ThreadCore)parameter!;

            while (!core.IsTerminated)
            {
                Thread.Sleep(100);
            }

            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetMachineGroup(Type interfaceType, out MachineGroup info)
        {
            if (!this.interfaceTypeToGroup.TryGetValue(interfaceType, out info!))
            {
                throw new InvalidOperationException($"Machine interface {interfaceType.FullName} is not registered.");
            }
        }

        private ThreadsafeTypeKeyHashTable<MachineGroup> interfaceTypeToGroup = new();

        private MachineGroup[] groupArray;

        private Dictionary<int, MachineGroup> typeIdToInfo = new();

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
