// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
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
        public void Run() => this.BigMachine.CommandPost.Send(this.Group, CommandPost<TIdentifier>.CommandType.Run, this.Identifier, 0);

        /// <summary>
        /// Runs the machine manually and receives the result.<br/>
        /// This function does not change <see cref="Machine{TIdentifier}.Timeout"/> or <see cref="Machine{TIdentifier}.NextRun"/>.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">Message.</param>
        /// <param name="millisecondTimeout">Timeout in milliseconds.</param>
        /// <returns>The result.</returns>
        public StateResult? RunTwoWay<TMessage>(TMessage message, int millisecondTimeout = 100) => this.BigMachine.CommandPost.SendTwoWay<TMessage, StateResult>(this.Group, CommandPost<TIdentifier>.CommandType.RunTwoWay, this.Identifier, message, millisecondTimeout);

        /// <summary>
        /// Sends a command to the machine.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="message">Message.</param>
        public void Command<TMessage>(TMessage message) => this.BigMachine.CommandPost.Send(this.Group, CommandPost<TIdentifier>.CommandType.Command, this.Identifier, message);

        /// <summary>
        /// Sends a command to the machine and receives the result.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="message">Message.</param>
        /// <param name="millisecondTimeout">Timeout in milliseconds.</param>
        /// <returns>The response.</returns>
        public TResponse? CommandTwoWay<TMessage, TResponse>(TMessage message, int millisecondTimeout = 100) => this.BigMachine.CommandPost.SendTwoWay<TMessage, TResponse>(this.Group, CommandPost<TIdentifier>.CommandType.CommandTwoWay, this.Identifier, message, millisecondTimeout);

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
        public void ChangeState(TState state) => this.BigMachine.CommandPost.Send(this.Group, CommandPost<TIdentifier>.CommandType.State, this.Identifier, Unsafe.As<TState, int>(ref state));

        /// <summary>
        /// Changes the state of the machine and receives the result.
        /// </summary>
        /// <param name="state">The next machine state.</param>
        /// <param name="millisecondTimeout">Timeout in milliseconds.</param>
        /// <returns><see langword="true"/>: The state is successfully changed.<br/>
        /// <see langword="false"/>: Not changed (change denied or the machine is not available.)</returns>
        public bool ChangeStateTwoWay(TState state, int millisecondTimeout = 100) => this.BigMachine.CommandPost.SendTwoWay<int, bool>(this.Group, CommandPost<TIdentifier>.CommandType.StateTwoWay, this.Identifier, Unsafe.As<TState, int>(ref state), millisecondTimeout);

        public void Command<TMessage>(TCommand command, TMessage message)
        {
            this.BigMachine.CommandPost.Send<TMessage>(this.Group, CommandPost<TIdentifier>.CommandType.Command, this.Identifier, message);
        }
    }
}
