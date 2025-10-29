// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

#pragma warning disable SA1202
#pragma warning disable SA1204

namespace BigMachines.Control;

[TinyhandObject(Structural = true)]
public sealed partial class UnorderedMachineControl<TIdentifier, TMachine, TInterface> : MultiMachineControl<TIdentifier, TInterface>, ITinyhandSerializable<UnorderedMachineControl<TIdentifier, TMachine, TInterface>>, ITinyhandCustomJournal
    where TIdentifier : notnull
    where TMachine : Machine<TIdentifier>
    where TInterface : Machine.ManMachineInterface
{
    #region FieldAndProperty

    public override MachineInformation MachineInformation { get; }

    private Item.GoshujinClass items;

    #endregion

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
            this.BigMachine is IStructuralObject obj)
        {
            ((IStructuralObject)this.items).SetupStructure(obj);
        }
    }

    [TinyhandObject(Structural = true)]
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

    public override bool ContainsActiveMachine()
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

    internal override void Process(MachineRunner runner)
    {
        using (this.items.LockObject.EnterScope())
        {
            foreach (var x in this.items)
            {
                runner.Add(x.Machine);
            }
        }
    }

    #endregion

    #region Main

    /// <summary>
    /// Attempts to retrieve a machine interface by its identifier.
    /// </summary>
    /// <param name="identifier">The identifier of the machine to retrieve.</param>
    /// <param name="machineInterface">When this method returns, contains the machine interface associated with the specified identifier, if found; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if the machine was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGet(TIdentifier identifier, [MaybeNullWhen(false)] out TInterface machineInterface)
    {
        using (this.items.LockObject.EnterScope())
        {
            if (this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                machineInterface = (TInterface)item.Machine.InterfaceInstance;
                return true;
            }
            else
            {
                machineInterface = default;
                return false;
            }
        }
    }

    /// <summary>
    /// Gets an existing machine or creates a new one if it doesn't exist, using the specified creation parameter.
    /// </summary>
    /// <param name="identifier">The identifier of the machine.</param>
    /// <param name="createParam">The parameter to pass to the machine's creation process if a new machine is created.</param>
    /// <returns>The machine interface for the existing or newly created machine.</returns>
    public TInterface GetOrCreate(TIdentifier identifier, object? createParam)
    {
        using (this.items.LockObject.EnterScope())
        {
            if (this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                return (TInterface)item.Machine.InterfaceInstance;
            }
            else
            {
                var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
                machine.Identifier = identifier;
                machine.PrepareCreateStart(this, createParam);
                item = new(identifier, machine);
                item.Goshujin = this.items;
                return (TInterface)item.Machine.InterfaceInstance;
            }
        }
    }

    /// <summary>
    /// Gets an existing machine or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="identifier">The identifier of the machine.</param>
    /// <returns>The machine interface for the existing or newly created machine.</returns>
    public TInterface GetOrCreate(TIdentifier identifier)
    {
        using (this.items.LockObject.EnterScope())
        {
            if (this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                return (TInterface)item.Machine.InterfaceInstance;
            }
            else
            {
                var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
                machine.Identifier = identifier;
                machine.PrepareStart(this);
                item = new(identifier, machine);
                item.Goshujin = this.items;
                return (TInterface)item.Machine.InterfaceInstance;
            }
        }
    }

    /// <summary>
    /// Creates a new machine with the specified identifier, terminating any existing machine with the same identifier first, using the specified creation parameter.
    /// </summary>
    /// <param name="identifier">The identifier of the machine.</param>
    /// <param name="createParam">The parameter to pass to the machine's creation process.</param>
    /// <returns>The machine interface for the newly created machine.</returns>
    public TInterface CreateAlways(TIdentifier identifier, object? createParam)
    {
        Machine.ManMachineInterface? machineInterface = default;

Loop:
        if (machineInterface is not null)
        {
            machineInterface.TerminateMachine();
        }

        using (this.items.LockObject.EnterScope())
        {
            if (this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                machineInterface = (TInterface)item.Machine.InterfaceInstance;
                goto Loop;
            }

            var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
            machine.Identifier = identifier;
            machine.PrepareCreateStart(this, createParam);
            item = new(identifier, machine);
            item.Goshujin = this.items;
            return (TInterface)item.Machine.InterfaceInstance;
        }
    }

    /// <summary>
    /// Creates a new machine with the specified identifier, terminating any existing machine with the same identifier first.
    /// </summary>
    /// <param name="identifier">The identifier of the machine.</param>
    /// <returns>The machine interface for the newly created machine.</returns>
    public TInterface CreateAlways(TIdentifier identifier)
    {
        Machine.ManMachineInterface? interfaceInstance = default;

Loop:
        if (interfaceInstance is not null)
        {
            interfaceInstance.TerminateMachine();
        }

        using (this.items.LockObject.EnterScope())
        {
            if (this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                interfaceInstance = (TInterface)item.Machine.InterfaceInstance;
                goto Loop;
            }

            var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
            machine.Identifier = identifier;
            machine.PrepareStart(this);
            item = new(identifier, machine);
            item.Goshujin = this.items;
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
        if (this.items is IStructuralObject obj)
        {
            return obj.ProcessJournalRecord(ref reader);
        }

        return false;
    }

    #endregion
}
