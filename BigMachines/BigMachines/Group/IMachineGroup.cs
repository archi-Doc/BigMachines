// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tinyhand;

namespace BigMachines;

/// <summary>
/// Represents a machine group (collection of machines).
/// </summary>
/// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
public interface IMachineGroup<TIdentifier>
    where TIdentifier : notnull
{
    /// <summary>
    /// Gets a machine interface associated with the identifier.
    /// </summary>
    /// <typeparam name="TMachineInterface"><see cref="ManMachineInterface{TIdentifier, TState, TCommand}"/>of the machine (e.g TestMachine.Interface).</typeparam>
    /// <param name="identifier">The identifier.</param>
    /// <returns>An instance of <see cref="ManMachineInterface{TIdentifier, TState, TCommand}"/>.</returns>
    public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
        where TMachineInterface : ManMachineInterface<TIdentifier>;

    /// <summary>
    /// Gets a collection of identifiers.
    /// </summary>
    /// <returns>A collection of identifiers.</returns>
    public IEnumerable<TIdentifier> GetIdentifiers();

    /// <summary>
    /// Serialize the machines to a byte array.
    /// </summary>
    /// <param name="options">Serializer options.</param>
    /// <returns>A byte array with the serialized value.</returns>
    public Task<byte[]?> SerializeAsync(TinyhandSerializerOptions? options = null)
        => this.BigMachine.CommandPost.BatchGroupAsync<byte[]>(CommandPost<TIdentifier>.BatchCommandType.Serialize, this, options);

    public Task<KeyValuePair<TIdentifier, TResponse?>[]> CommandAndReceiveAsync<TCommand, TMessage, TResponse>(TCommand command, TMessage message)
        where TCommand : struct
    {
        var data = Unsafe.As<TCommand, int>(ref command);
        return this.BigMachine.CommandPost.SendAndReceiveGroupAsync<TMessage, TResponse>(this, CommandPost<TIdentifier>.CommandType.Command, this.GetIdentifiers(), data, message);
    }

    public Task CommandAsync<TCommand, TMessage>(TCommand command, TMessage message)
        where TCommand : struct
    {
        var data = Unsafe.As<TCommand, int>(ref command);
        return this.BigMachine.CommandPost.SendGroupAsync<TMessage>(this, CommandPost<TIdentifier>.CommandType.Command, this.GetIdentifiers(), data, message);
    }

    /// <summary>
    /// Gets an instance of <see cref="BigMachine{TIdentifier}"/>.
    /// </summary>
    public BigMachine<TIdentifier> BigMachine { get; }

    /// <summary>
    /// Gets <see cref="MachineInfo{TIdentifier}"/>.
    /// </summary>
    public MachineInfo<TIdentifier> Info { get; }

    /// <summary>
    /// Gets the number of machines in the group.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Sets <see cref="MachineInfo{TIdentifier}"/> to this group.
    /// </summary>
    /// <param name="info">The machine info.</param>
    internal void Assign(MachineInfo<TIdentifier> info);

    /// <summary>
    /// Gets a collection of machines. Used internally.
    /// </summary>
    /// <returns>A collection of machines.</returns>
    internal IEnumerable<Machine<TIdentifier>> GetMachines();

    internal bool TryGetMachine(TIdentifier identifier, [MaybeNullWhen(false)] out Machine<TIdentifier> machine);

    internal Machine<TIdentifier> GetOrAddMachine(TIdentifier identifier, Machine<TIdentifier> machine);

    internal void AddMachine(TIdentifier identifier, Machine<TIdentifier> machine);

    internal bool RemoveFromGroup(TIdentifier identifier);
}

public interface IMachineGroup<TIdentifier, TState, TCommand> : IMachineGroup<TIdentifier>
    where TIdentifier : notnull
    where TState : struct
    where TCommand : struct
{
    /// <summary>
    /// Sends a command to each machine in the group.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="command">The commmand.</param>
    /// <param name="message">The message.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public Task CommandAsync<TMessage>(TCommand command, TMessage message);

    /// <summary>
    /// Sends a message to each machine in the group and receives the result.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="command">The commmand.</param>
    /// <param name="message">The message.</param>
    /// <returns>The response.</returns>
    public Task<KeyValuePair<TIdentifier, TResponse?>[]> CommandAndReceiveAsync<TMessage, TResponse>(TCommand command, TMessage message);
}
