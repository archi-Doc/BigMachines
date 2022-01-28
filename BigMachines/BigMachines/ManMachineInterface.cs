// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tinyhand;

namespace BigMachines
{
    /// <summary>
    /// Class for operating a machine.<br/>
    /// To achieve lock-free operation, you need to use <see cref="ManMachineInterface{TIdentifier, TState, TCommand}"/> instead of using machines directly.
    /// </summary>
    /// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
    public abstract class ManMachineInterface<TIdentifier>
        where TIdentifier : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManMachineInterface{TIdentifier}"/> class.
        /// </summary>
        /// <param name="group">The group to which the machine belongs.</param>
        /// <param name="identifier">The identifier.</param>
        public ManMachineInterface(IMachineGroup<TIdentifier> group, TIdentifier identifier)
        {
            this.BigMachine = group.BigMachine;
            this.Group = group;
            this.Identifier = TinyhandSerializer.Clone(identifier);
        }

        /// <summary>
        /// Gets <see cref="BigMachine{TIdentifier}"/> instance.
        /// </summary>
        public BigMachine<TIdentifier> BigMachine { get; }

        /// <summary>
        /// Gets <see cref="MachineGroup{TIdentifier}"/> instance.
        /// </summary>
        public IMachineGroup<TIdentifier> Group { get; }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public TIdentifier Identifier { get; }

        /// <summary>
        /// Gets the status of the machine.
        /// </summary>
        /// <returns>The status of the machine.<br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public MachineStatus? GetMachineStatus()
        {
            if (this.Group.TryGetMachine(this.Identifier, out var machine))
            {
                return machine.Status;
            }

            return null;
        }

        /// <summary>
        /// Changes the status of the machine.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <returns><see langword="true"/>: The status is successfully changed.<br/>
        /// <see langword="false"/>: Machine is not available.</returns>
        public bool SetMachineStatus(MachineStatus status)
        {
            if (this.Group.TryGetMachine(this.Identifier, out var machine))
            {
                machine.Status = status;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the default timeout of the machine.
        /// </summary>
        /// <returns>The default timeout of the machine.<br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public TimeSpan? GetDefaultTimeout()
        {
            if (this.Group.TryGetMachine(this.Identifier, out var machine))
            {
                return machine.DefaultTimeout;
            }

            return null;
        }

        /// <summary>
        /// Runs the machine manually.<br/>
        /// This function does not change <see cref="Machine{TIdentifier}.Timeout"/> or <see cref="Machine{TIdentifier}.NextRun"/>.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task RunAsync() => this.BigMachine.CommandPost.SendAsync(this.Group, CommandPost<TIdentifier>.CommandType.Run, this.Identifier, 0, 0);

        /// <summary>
        /// Serialize the machines to a byte array.
        /// </summary>
        /// <param name="options">Serializer options.</param>
        /// <returns>A byte array with the serialized value.</returns>
        public byte[]? Serialize(TinyhandSerializerOptions? options = null)
        {
            if (!this.Group.TryGetMachine(this.Identifier, out var machine))
            {
                return null;
            }

            if (machine.IsSerializable && machine is ITinyhandSerialize serializer)
            {
                options ??= TinyhandSerializer.DefaultOptions;
                var writer = default(Tinyhand.IO.TinyhandWriter);

                lock (machine.SyncMachine)
                {
                    writer.WriteArrayHeader(2); // Header
                    writer.Write(machine.TypeId); // Id
                    serializer.Serialize(ref writer, options); // Data
                }

                return writer.FlushAndGetArray();
            }

            return null;
        }
    }

    /// <summary>
    /// Class for operating a machine.<br/>
    /// To achieve lock-free operation, you need to use <see cref="ManMachineInterface{TIdentifier, TState, TCommand}"/> instead of using machines directly.<br/>
    /// <see cref="ManMachineInterface{TIdentifier, TState, TCommand}"/> = <see cref="ManMachineInterface{TIdentifier}"/> + TState.
    /// </summary>
    /// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
    /// <typeparam name="TState">The type of machine state.</typeparam>
    /// <typeparam name="TCommand">The type of machine command.</typeparam>
    public abstract class ManMachineInterface<TIdentifier, TState, TCommand> : ManMachineInterface<TIdentifier>
        where TIdentifier : notnull
        where TState : struct
        where TCommand : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManMachineInterface{TIdentifier, TState, TCommand}"/> class.
        /// </summary>
        /// <param name="group">The group to which the machine belongs.</param>
        /// <param name="identifier">The identifier.</param>
        public ManMachineInterface(IMachineGroup<TIdentifier> group, TIdentifier identifier)
            : base(group, identifier)
        {
        }

        /// <summary>
        /// Gets the current state of the machine.
        /// </summary>
        /// <returns>The state of the machine..<br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public TState? GetCurrentState()
        {
            if (this.Group.TryGetMachine(this.Identifier, out var machine))
            {
                if (machine is Machine<TIdentifier> m && m.Status != MachineStatus.Terminated)
                {
                    return System.Runtime.CompilerServices.Unsafe.As<int, TState>(ref m.CurrentState);
                }
            }

            return null;
        }

        /// <summary>
        /// Changes the state of the machine.
        /// </summary>
        /// <param name="state">The next machine state.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public Task ChangeStateAsync(TState state)
        {
            var i = Unsafe.As<TState, int>(ref state);
            return this.BigMachine.CommandPost.SendAsync(this.Group, CommandPost<TIdentifier>.CommandType.ChangeState, this.Identifier, i, i);
        }

        public Task CommandAsync<TMessage>(TCommand command, TMessage message)
        {
            var data = Unsafe.As<TCommand, int>(ref command);
            return this.BigMachine.CommandPost.SendAsync<TMessage>(this.Group, CommandPost<TIdentifier>.CommandType.Command, this.Identifier, data, message);
        }

        public Task<TResponse?> CommandAndReceiveAsync<TMessage, TResponse>(TCommand command, TMessage message)
        {
            var data = Unsafe.As<TCommand, int>(ref command);
            return this.BigMachine.CommandPost.SendAndReceiveAsync<TMessage, TResponse>(this.Group, CommandPost<TIdentifier>.CommandType.Command, this.Identifier, data, message);
        }
    }
}
