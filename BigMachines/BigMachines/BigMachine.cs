// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        public class MachineInfo
        {
            public MachineInfo(Type machineType, int typeId, Func<BigMachine<TIdentifier>, MachineBase<TIdentifier>>? constructor)
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
            this.Core = new ThreadCore(parent, this.MainLoop);
            this.CommandPost = new(parent);
            this.CommandPost.Open(this.DistributeCommand);
            this.ServiceProvider = serviceProvider;

            foreach (var x in InterfaceTypeToInfo.Values)
            {
                if (!this.typeIdToMachineInfo.Contains(x.TypeId))
                {
                    this.typeIdToMachineInfo.Add(x.TypeId, x);
                }
            }
        }

        // public static Dictionary<Type, MachineInfo> InterfaceTypeToInfo { get; } = new();
        public static Dictionary<Type, MachineInfo> InterfaceTypeToInfo { get; } = new();

        public ThreadCore Core { get; }

        public CommandPost<TIdentifier> CommandPost { get; }

        public IServiceProvider? ServiceProvider { get; }

        public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
            where TMachineInterface : ManMachineInterface
        {
            if (this.IdentificationToMachine.TryGetValue(identifier, out var machine))
            {
                return machine.InterfaceInstance as TMachineInterface;
            }

            return null;
        }

        public TMachineInterface? TryCreate<TMachineInterface>(TIdentifier identifier, object? parameter = null)
            where TMachineInterface : ManMachineInterface
        {
            MachineBase<TIdentifier>? machine = null;
            if (this.IdentificationToMachine.TryGetValue(identifier, out machine))
            {
                return machine.InterfaceInstance as TMachineInterface;
            }

            if (InterfaceTypeToInfo.TryGetValue(typeof(TMachineInterface), out var info))
            {
                machine = this.CreateMachine(info);

                var clone = TinyhandSerializer.Clone(identifier);
                machine.CreateInterface(clone);
                machine.SetParameter(parameter);

                machine = this.IdentificationToMachine.GetOrAdd(clone, machine);
                return machine.InterfaceInstance as TMachineInterface;
            }

            throw new InvalidOperationException("Not registered.");
        }

        public TMachineInterface Create<TMachineInterface>(TIdentifier identifier, object? parameter = null)
            where TMachineInterface : ManMachineInterface
        {
            if (InterfaceTypeToInfo.TryGetValue(typeof(TMachineInterface), out var info))
            {
                var machine = this.CreateMachine(info);

                var clone = TinyhandSerializer.Clone(identifier);
                machine.CreateInterface(clone);
                machine.SetParameter(parameter);

                this.IdentificationToMachine[clone] = machine;
                MachineBase<TIdentifier>? machineToRemove = null;
                this.IdentificationToMachine.AddOrUpdate(clone, x => machine, (i, m) =>
                {
                    machineToRemove = m;
                    return machine;
                });

                if (machineToRemove != null)
                {
                    this.RemoveMachine(identifier, machineToRemove);
                }

                return (TMachineInterface)machine.InterfaceInstance!;
            }

            throw new InvalidOperationException("Not registered.");
        }

        public bool Remove(TIdentifier identifier)
        {
            if (this.IdentificationToMachine.TryGetValue(identifier, out var machine))
            {
                return this.RemoveMachine(identifier, machine);
            }

            return false;
        }

        public byte[] Serialize()
        {
            var writer = default(Tinyhand.IO.TinyhandWriter);
            var options = TinyhandSerializer.DefaultOptions;

            foreach (var machine in this.IdentificationToMachine.Values.Where(a => a.IsSerializable))
            {
                if (machine is ITinyhandSerialize serializer)
                {
                    lock (machine)
                    {
                        writer.WriteArrayHeader(2); // Header
                        writer.Write(machine.TypeId); // Id
                        serializer.Serialize(ref writer, options); // Data
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
                var info = (MachineInfo?)this.typeIdToMachineInfo[key];
                if (info != null)
                {
                    var machine = this.CreateMachine(info);
                    if (machine is ITinyhandSerialize serializer)
                    {
                        serializer.Deserialize(ref reader, options);
                        machine.CreateInterface(machine.Identifier);
                        this.IdentificationToMachine[machine.Identifier] = machine;
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

        private bool RemoveMachine(TIdentifier identifier, MachineBase<TIdentifier> machine)
        {
            lock (machine)
            {
                machine.Status = MachineStatus.Terminated;
                return this.IdentificationToMachine.TryRemove(identifier, out _);
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

        private MachineBase<TIdentifier> CreateMachine(MachineInfo info)
        {
            MachineBase<TIdentifier>? machine = null;

            if (this.ServiceProvider != null)
            {
                machine = this.ServiceProvider.GetService(info.MachineType) as MachineBase<TIdentifier>;
            }

            if (machine == null)
            {
                if (info.Constructor != null)
                {
                    machine = info.Constructor(this);
                }
                else
                {
                    throw new InvalidOperationException("Requires IServiceProvider.");
                }
            }

            machine.TypeId = info.TypeId;
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

        internal ConcurrentDictionary<TIdentifier, MachineBase<TIdentifier>> IdentificationToMachine { get; } = new();

        private Hashtable typeIdToMachineInfo = new();

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
