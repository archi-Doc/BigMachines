// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

namespace BigMachines;

[TinyhandObject(Tree = true)]
public sealed partial class UnorderedMachineControl<TIdentifier, TMachine, TInterface, TState, TCommand>
    : ITinyhandSerialize<UnorderedMachineControl<TIdentifier, TMachine, TInterface, TState, TCommand>>, ITinyhandCustomJournal
    where TIdentifier : notnull
    where TMachine : ITinyhandSerialize<TMachine>, IMachine
    where TInterface : ManMachineInterface<TIdentifier, TState, TCommand>
    where TState : struct
    where TCommand : struct
{
    public UnorderedMachineControl()
    {
    }

    public UnorderedMachineControl(BigMachineBase bigMachine)
    {
        this.bigMachine = bigMachine;
        this.Info = default!; // Must call Assign()
    }

    #region Item

    [TinyhandObject(Tree = true)]
    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private partial class Item
    {
        public Item()
        {
            this.Identifier = default!;
            this.Machine = default!;
        }

        public Item(TIdentifier identifier, TMachine machine)
        {
            this.Identifier = identifier;
            this.Machine = machine;
        }

#pragma warning disable SA1401 // Fields should be private

        [Key(0)]
        [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
        public TIdentifier Identifier;

        [Key(1)]
        public TMachine Machine;

#pragma warning restore SA1401 // Fields should be private
    }

    #endregion

    #region Unique

    public TInterface? TryGet(TIdentifier identifier)
    {
        lock (this.items.SyncObject)
        {
            if (this.items.IdentifierChain.FindFirst(identifier) is { } item)
            {
                return item.Machine.InterfaceInstance as TInterface;
            }
        }

        return null;
    }

    public Task CommandAsync<TMessage>(TCommand command, TMessage message)
    {
        return this.bigMachine.CommandPost.SendGroupAsync(this, CommandPost<TIdentifier>.CommandType.Command, this.IdentificationToMachine.Keys, Unsafe.As<TCommand, int>(ref command), message);
    }

    public Task<KeyValuePair<TIdentifier, TResponse?>[]> CommandAndReceiveAsync<TCommand, TMessage, TResponse>(TCommand command, TMessage message)
        where TCommand : struct
    {
        return this.bigMachine.CommandPost.SendAndReceiveGroupAsync<TMessage, TResponse>(this, CommandPost<TIdentifier>.CommandType.Command, this.IdentificationToMachine.Keys, Unsafe.As<TCommand, int>(ref command), message);
    }

    static void ITinyhandSerialize<UnorderedMachineControl<TIdentifier, TMachine, TInterface, TState, TCommand>>.Serialize(ref TinyhandWriter writer, scoped ref UnorderedMachineControl<TIdentifier, TMachine, TInterface, TState, TCommand>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        TinyhandSerializer.SerializeObject(ref writer, value.items, options);
    }

    static void ITinyhandSerialize<UnorderedMachineControl<TIdentifier, TMachine, TInterface, TState, TCommand>>.Deserialize(ref TinyhandReader reader, scoped ref UnorderedMachineControl<TIdentifier, TMachine, TInterface, TState, TCommand>? value, TinyhandSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
        => ((ITreeObject)this.items).ReadRecord(ref reader);

    ITreeObject

    #endregion

    #region FieldAndProperty

    private readonly BigMachineBase bigMachine;
    private readonly CommandPost<TIdentifier> commandPost = new();
    private Item.GoshujinClass items = new();

    public MachineInfo<TIdentifier> Info { get; private set; }

    public int Count => this.IdentificationToMachine.Count;

    #endregion

}
