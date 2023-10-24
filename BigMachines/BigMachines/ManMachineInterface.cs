// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Tinyhand;

namespace BigMachines;

/// <summary>
/// Class for operating a machine.<br/>
/// To achieve lock-free operation, you need to use <see cref="ManMachineInterface{TIdentifier, TState, TCommand}"/> instead of using machines directly.
/// </summary>
/// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
public abstract class ManMachineInterface<TIdentifier> // MANMACHINE INTERFACE by Shirow.
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
    public OperationalState? GetOperationalState()
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
    public bool SetOperationalState(OperationalState status)
    {
        if (this.Group.TryGetMachine(this.Identifier, out var machine))
        {
            machine.Status = status;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Indicates whether the machine is running (in a Run method).
    /// </summary>
    /// <returns><see langword="true"/>: The machine is running (in a Run method).</returns>
    public bool IsRunning()
    {
        if (this.Group.TryGetMachine(this.Identifier, out var machine))
        {
            return machine.RunType != RunType.NotRunning;
        }

        return false;
    }

    /// <summary>
    /// Indicates whether the machine is active (in a Run method or waiting to execute).
    /// </summary>
    /// <returns><see langword="true"/>: The machine is active (in a Run method or waiting to execute).</returns>
    public bool IsActive()
    {
        if (this.Group.TryGetMachine(this.Identifier, out var machine))
        {
            return machine.RunType != RunType.NotRunning ||
                (machine.DefaultTimeout is TimeSpan ts && ts > TimeSpan.Zero);
        }

        return false;
    }

    /// <summary>
    /// Indicates whether the machine is terminated.
    /// </summary>
    /// <returns><see langword="true"/>: The machine is terminated.</returns>
    public bool IsTerminated()
    {
        if (this.Group.TryGetMachine(this.Identifier, out var machine))
        {
            return machine.Status == OperationalState.Terminated;
        }

        return true;
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
    /// Set the timeout of the machine.<br/>
    /// The time decreases while the program is running, and the machine will run when it reaches zero.
    /// </summary>
    /// <param name="timeout">The timeout.</param>
    /// <param name="absoluteDateTime">Set <see langword="true"></see> to specify the next execution time by adding the current time and timeout.</param>
    /// <returns><see langword="true"/>:  the timeout is successfully set.</returns>
    public bool SetTimeout(TimeSpan timeout, bool absoluteDateTime = false)
    {
        if (this.Group.TryGetMachine(this.Identifier, out var machine))
        {
            machine.SetTimeout(timeout, absoluteDateTime);
            return true;
        }

        return false;
    }

    public bool SetTimeout(TimeSpan timeSpan)
    {
        if (this.Group.TryGetMachine(this.Identifier, out var machine))
        {
            Volatile.Write(ref machine.Timeout, timeSpan.Ticks);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Runs the machine manually.<br/>
    /// This method does not change <see cref="Machine{TIdentifier}.Timeout"/> or <see cref="Machine{TIdentifier}.NextRun"/>.
    /// </summary>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public Task RunAsync() => this.BigMachine.CommandPost.SendAsync(this.Group, CommandPost<TIdentifier>.CommandType.Run, this.Identifier, 0);

    /// <summary>
    /// Serialize the machines to a byte array.
    /// </summary>
    /// <param name="options">Serializer options.</param>
    /// <returns>A byte array with the serialized value.</returns>
    public Task<byte[]?> SerializeAsync(TinyhandSerializerOptions? options = null)
        => this.BigMachine.CommandPost.BatchSingleAsync<byte[]>(CommandPost<TIdentifier>.BatchCommandType.Serialize, this.Group, this.Identifier, options);
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
    /// <param name="state">The state of the machine.</param>
    /// <returns>
    /// <see langword="true"/>: the state is successfully retrieved; otherwise <see langword="false"/>.</returns>
    public bool TryGetState(out TState state)
    {
        if (this.Group.TryGetMachine(this.Identifier, out var machine))
        {
            if (machine is Machine<TIdentifier> m && m.Status != OperationalState.Terminated)
            {
                state = Unsafe.As<int, TState>(ref m.CurrentState);
                return true;
            }
        }

        state = default;
        return false;
    }

    /// <summary>
    /// Changes the state of the machine.
    /// </summary>
    /// <param name="state">The next machine state.</param>
    /// <returns><see langword="true"/> if the state is successfully changed.</returns>
    public Task<ChangeStateResult> ChangeStateAsync(TState state)
    {
        var i = Unsafe.As<TState, int>(ref state);
        return this.BigMachine.CommandPost.SendAndReceiveAsync<ChangeStateResult>(this.Group, CommandPost<TIdentifier>.CommandType.ChangeState, this.Identifier, i);
    }

    public Task CommandAsync(TCommand command)
    {
        var data = Unsafe.As<TCommand, int>(ref command);
        return this.BigMachine.CommandPost.SendAsync(this.Group, CommandPost<TIdentifier>.CommandType.Command, this.Identifier, data);
    }

    public Task CommandAsync<TMessage>(TCommand command, TMessage message)
    {
        var data = Unsafe.As<TCommand, int>(ref command);
        return this.BigMachine.CommandPost.SendAsync<TMessage>(this.Group, CommandPost<TIdentifier>.CommandType.Command, this.Identifier, data, message);
    }

    public Task<TResponse?> CommandAndReceiveAsync<TResponse>(TCommand command)
    {
        var data = Unsafe.As<TCommand, int>(ref command);
        return this.BigMachine.CommandPost.SendAndReceiveAsync<TResponse>(this.Group, CommandPost<TIdentifier>.CommandType.Command, this.Identifier, data);
    }

    public Task<TResponse?> CommandAndReceiveAsync<TMessage, TResponse>(TCommand command, TMessage message)
    {
        var data = Unsafe.As<TCommand, int>(ref command);
        return this.BigMachine.CommandPost.SendAndReceiveAsync<TMessage, TResponse>(this.Group, CommandPost<TIdentifier>.CommandType.Command, this.Identifier, data, message);
    }
}
