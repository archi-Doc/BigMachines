// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

#pragma warning disable SA1202
#pragma warning disable SA1204

namespace BigMachines.Control;

[TinyhandObject(Structual = true)]
public sealed partial class UnorderedMachineControl<TIdentifier, TMachine, TInterface> : MultiMachineControl<TIdentifier, TInterface>, ITinyhandSerializable<UnorderedMachineControl<TIdentifier, TMachine, TInterface>>, ITinyhandCustomJournal
    where TIdentifier : notnull
    where TMachine : Machine<TIdentifier>
    where TInterface : Machine.ManMachineInterface
{
    public UnorderedMachineControl()
        : base()
    {
        this.MachineInformation = MachineRegistry.Get<TMachine>();
        this.items = new();
    }

    public void Prepare(BigMachineBase bigMachine)
    {
        this.BigMachine = bigMachine;
        if (this.MachineInformation.Serializable &&
            this.BigMachine is IStructualObject obj)
        {
            ((IStructualObject)this.items).SetupStructure(obj);
        }
    }

    [TinyhandObject(Structual = true)]
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

    public override MachineInformation MachineInformation { get; }

    private Item.GoshujinClass items;

    // public TCommandAll CommandAll { get; private set; } = default!;

    #region Abstract

    public override int Count
        => this.items.Count;

    public override TIdentifier[] GetIdentifiers()
    {
        using (this.items.LockObject.EnterScope())
        {
            return this.items.Select(x => x.Identifier).ToArray();
        }
    }

    public override bool CheckActiveMachine()
    {
        using (this.items.LockObject.EnterScope())
        {
            foreach (var x in this.items)
            {
                if (x.Machine.IsActive)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public override TInterface[] GetArray()
    {
        using (this.items.LockObject.EnterScope())
        {
            return this.items.Select(x => (TInterface)x.Machine.InterfaceInstance).ToArray();
        }
    }

    internal override TMachine[] GetMachines()
    {
        using (this.items.LockObject.EnterScope())
        {
            return this.items.Select(x => x.Machine).ToArray();
        }
    }

    internal override bool RemoveMachine(Machine machine)
    {
        if (machine is not Machine<TIdentifier> m)
        {
            return false;
        }

        using (this.items.LockObject.EnterScope())
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

    internal override void Process(DateTime now, TimeSpan elapsed)
    {
        using (this.items.LockObject.EnterScope())
        {
            foreach (var x in this.items)
            {
                x.Machine.Process(now, elapsed);
            }
        }
    }

    #endregion

    #region Main

    public TInterface? TryGet(TIdentifier identifier)
    {
        using (this.items.LockObject.EnterScope())
        {
            if (this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                return (TInterface)item.Machine.InterfaceInstance;
            }
            else
            {
                return default;
            }
        }
    }

    public TInterface? TryCreate(TIdentifier identifier, object? createParam = null)
    {
        using (this.items.LockObject.EnterScope())
        {
            if (this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                return default;
            }
            else
            {
                var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
                machine.Identifier = identifier;
                machine.PrepareCreateStart(this, createParam);
                item = new(identifier, machine);
                item.Goshujin = this.items;
            }

            return (TInterface)item.Machine.InterfaceInstance;
        }
    }

    public TInterface GetOrCreate(TIdentifier identifier, object? createParam = null)
    {
        using (this.items.LockObject.EnterScope())
        {
            if (!this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
                machine.Identifier = identifier;
                machine.PrepareCreateStart(this, createParam);
                item = new(identifier, machine);
                item.Goshujin = this.items;
            }

            return (TInterface)item.Machine.InterfaceInstance;
        }
    }

    #endregion

    #region Tinyhand

    static void ITinyhandSerializable<UnorderedMachineControl<TIdentifier, TMachine, TInterface>>.Serialize(ref TinyhandWriter writer, scoped ref UnorderedMachineControl<TIdentifier, TMachine, TInterface>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        TinyhandSerializer.SerializeObject(ref writer, value.items, options);
    }

    static void ITinyhandSerializable<UnorderedMachineControl<TIdentifier, TMachine, TInterface>>.Deserialize(ref TinyhandReader reader, scoped ref UnorderedMachineControl<TIdentifier, TMachine, TInterface>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        value ??= new();
        value.items = TinyhandSerializer.DeserializeObject<Item.GoshujinClass>(ref reader, options) ?? new();
        foreach (var x in value.items)
        {
            x.Machine.PrepareStart(value);
        }
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
