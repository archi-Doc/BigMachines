// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

#pragma warning disable SA1202
#pragma warning disable SA1204

namespace BigMachines.Redesign;

[TinyhandObject(Structual = true)]
public sealed partial class UnorderedMachineControl<TIdentifier, TInterface, TCommandAll> : MultiMachineControl<TIdentifier, TInterface>, ITinyhandSerialize<UnorderedMachineControl<TIdentifier, TInterface, TCommandAll>>, ITinyhandCustomJournal
    where TIdentifier : notnull
    where TInterface : Machine.ManMachineInterface
{
    internal UnorderedMachineControl()
        : base()
    {
    }

    public void Prepare(BigMachineBase bigMachine, MachineInformation machineInformation)
    {
        this.BigMachine = bigMachine;
        this.machineInformation = machineInformation;
        if (this.machineInformation.CommandAllConstructor is { })
        {
            this.CommandAll = (TCommandAll)this.machineInformation.CommandAllConstructor(this);
        }
    }

    private MachineInformation? machineInformation;
    private Func<UnorderedMachineControl<TIdentifier, TInterface, TCommandAll>, TIdentifier, TInterface>? createInterface; // MachineControl + Identifier -> Machine.Interface
    private Func<UnorderedMachineControl<TIdentifier, TInterface, TCommandAll>, TCommandAll>? createCommandAll; // MachineControl -> Machine.Interface.CommandAll

    [TinyhandObject(Structual = true)]
    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private partial class Item
    {
        public Item()
        {
            this.Identifier = default!;
            this.Interface = default!;
        }

        public Item(TIdentifier identifier, TInterface @interface)
        {
            this.Identifier = identifier;
            this.Interface = @interface;
        }

#pragma warning disable SA1401 // Fields should be private

        [Key(0)]
        [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
        public TIdentifier Identifier;

        [Key(1)]
        public TInterface Interface;

#pragma warning restore SA1401 // Fields should be private
    }

    private Item.GoshujinClass items = new();

    public TCommandAll CommandAll { get; private set; } = default!;

    #region Abstract

    public override TIdentifier[] GetIdentifiers()
    {
        lock (this.items.SyncObject)
        {
            return this.items.Select(x => x.Identifier).ToArray();
        }
    }

    public override TInterface[] GetArray()
    {
        lock (this.items.SyncObject)
        {
            return this.items.Select(x => x.Interface).ToArray();
        }
    }

    internal override bool RemoveMachine(Machine machine)
    {
        if (machine is not Machine<TIdentifier> m)
        {
            return false;
        }

        lock (this.items.SyncObject)
        {
            if (this.items.IdentifierChain.TryGetValue(m.Identifier, out var item))
            {
                item.Goshujin = null;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    #endregion

    #region Main

    public TInterface? TryGet(TIdentifier identifier)
    {
        lock (this.items.SyncObject)
        {
            if (this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                return item.Interface;
            }
            else
            {
                return default;
            }
        }
    }

    public TInterface GetOrCreate(TIdentifier identifier)
    {
        lock (this.items.SyncObject)
        {
            if (!this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                item = new(identifier, this.createInterface(this, identifier));
            }

            return item.Interface;
        }
    }

    #endregion

    #region Tinyhand

    static void ITinyhandSerialize<UnorderedMachineControl<TIdentifier, TInterface, TCommandAll>>.Serialize(ref TinyhandWriter writer, scoped ref UnorderedMachineControl<TIdentifier, TInterface, TCommandAll>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        TinyhandSerializer.SerializeObject(ref writer, value.items, options);
    }

    static void ITinyhandSerialize<UnorderedMachineControl<TIdentifier, TInterface, TCommandAll>>.Deserialize(ref TinyhandReader reader, scoped ref UnorderedMachineControl<TIdentifier, TInterface, TCommandAll>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        value ??= new();
        value.items = TinyhandSerializer.DeserializeObject<Item.GoshujinClass>(ref reader, options) ?? new();
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        if (this.items is IStructualObject obj)
        {
            return obj.ReadRecord(ref reader);
        }

        return false;
    }

    #endregion
}
