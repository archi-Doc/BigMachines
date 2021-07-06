// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arc.Threading;
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
        {//// typeof(TestMachine.Interface) => Constructor, TypeId, typeof(TestMachine)
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

        public static Dictionary<Type, MachineInfo> InterfaceTypeToInfo { get; } = new();

        public CommandPost<TIdentifier> CommandPost { get; }

        public IServiceProvider? ServiceProvider { get; }

        /*public ManMachineInterface<TIdentifier, TState>? GetMachine<TState>(TIdentifier identifier)
            where TState : struct
        {
            if (this.identificationToStateType.TryGetValue(identifier, out var type))
            {
                if (type != typeof(TState))
                {
                    throw new InvalidOperationException();
                }

                return new ManMachineInterface<TIdentifier, TState>(this, identifier);
            }

            return null;
        }

        public ManMachineInterface<TIdentifier, TState>? GetOrAdd<TState>(TIdentifier identifier, Func<TMachine> func)
            where TState : struct
            where TMachine : new()
        {
            if (this.identificationToStateType.TryGetValue(identifier, out var type))
            {
                if (type != typeof(TState))
                {
                    throw new InvalidOperationException();
                }

                return new ManMachineInterface<TIdentifier, TState>(this, identifier);
            }
            else
            {
                var machine = new TMachine(this);

            }

            return null;
        }*/

        /*public ManMachineInterface<TIdentifier, TState>? AddMachine<TState>(TIdentifier identifier, bool createNew = false, object? parameter = null)
        {
            if (this.identificationToStateType.TryGetValue(identifier, out var type))
            {
                if (type != typeof(TState))
                {
                    throw new InvalidOperationException();
                }

                return new ManMachineInterface<TIdentifier, TState>(this, identifier);
            }

            return null;
        }*/

        public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
            where TMachineInterface : ManMachineInterface
        {
            if (this.IdentificationToMachine.TryGetValue(identifier, out var machine))
            {
                return (TMachineInterface?)machine.InterfaceInstance;
            }

            return null;
        }

        public TMachineInterface? Create<TMachineInterface>(TIdentifier identifier, object? parameter = null, bool createNew = false)
            where TMachineInterface : ManMachineInterface
        {
            MachineBase<TIdentifier>? machine = null;
            if (!createNew && this.IdentificationToMachine.TryGetValue(identifier, out machine))
            {
                return (TMachineInterface?)machine.InterfaceInstance;
            }

            if (InterfaceTypeToInfo.TryGetValue(typeof(TMachineInterface), out var info))
            {
                machine = this.CreateMachine(info);

                var clone = TinyhandSerializer.Clone(identifier);
                machine.CreateInterface(clone);
                machine.InitializeAndIsolate(parameter);
                this.IdentificationToMachine[clone] = machine;
                return (TMachineInterface?)machine.InterfaceInstance;
            }

            throw new InvalidOperationException("Not registered.");
        }

        public byte[] Serialize()
        {
            var writer = default(Tinyhand.IO.TinyhandWriter);
            var array = this.IdentificationToMachine.ToArray();
            var options = TinyhandSerializer.DefaultOptions;

            // foreach (var machine in this.IdentificationToMachine.Values.Where(a => a.IsSerializable))
            foreach (var machine in this.IdentificationToMachine.Values)
            {
                if (machine is ITinyhandSerialize serializer)
                {
                    writer.WriteArrayHeader(2); // Header
                    writer.Write(0); // Id
                    serializer.Serialize(ref writer, options); // Data
                }

                /*lock (machine)
                {
                    TinyhandSerializer.Serialize(ref w, machine);
                }*/
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
                }
                else
                {
                    reader.Skip();
                    continue;
                }
            }
        }

        /*public ManMachineInterface<TIdentifier, TState>? GetMachine<TMachine, TState>(TIdentifier identifier)
            where TMachine : Machine<TIdentifier, TState>
            where TState : struct
        {
            if (this.identificationToStateType.TryGetValue(identifier, out var type))
            {
                if (type != typeof(TState))
                {
                    throw new InvalidOperationException();
                }

                return new ManMachineInterface<TIdentifier, TState>(this, identifier);
            }

            return null;
        }

        public void Add(MachineBase<TIdentifier> machine, TIdentifier identifier, object? parameter)
        {
            machine.InitializeAndIsolate(identifier, parameter);
        }

        public ManMachineInterface<TIdentifier, TState> AddMachine<TMachine, TState>(TIdentifier identifier, object? parameter)
            where TMachine : Machine<TIdentifier, TState>
        {
            return default!;
        }

        public bool TryAdd<TMachine>(TIdentifier identifier, object? parameter)
        where TMachine : MachineBase<TIdentifier>
        {
            var newlyAdded = false;
            this.identificationToMachine.GetOrAdd(identifier, x =>
            {
                newlyAdded = true;
                var machine = MachineBase<TIdentifier>.NewInstance(this);
                machine.InitializeAndIsolate(x, parameter);
                return machine;
            });

            return newlyAdded;
        }*/

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

            return machine;
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
