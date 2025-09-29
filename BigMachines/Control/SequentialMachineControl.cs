// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Arc.Threading;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

#pragma warning disable SA1202
#pragma warning disable SA1204

namespace BigMachines.Control;

public interface ISequentialMachineControl
{
    void Start();

    Machine.ManMachineInterface? GetFirst();
}

[TinyhandObject(Structural = true)]
public sealed partial class SequentialMachineControl<TIdentifier, TMachine, TInterface> : MultiMachineControl<TIdentifier, TInterface>, ISequentialMachineControl, ITinyhandSerializable<SequentialMachineControl<TIdentifier, TMachine, TInterface>>, ITinyhandCustomJournal
    where TIdentifier : notnull
    where TMachine : Machine<TIdentifier>
    where TInterface : Machine.ManMachineInterface
{
    public SequentialMachineControl()
        : base()
    {
        this.MachineInformation = MachineRegistry.Get<TMachine>();
        this.cores = new SequentialCore[this.MachineInformation.NumberOfTasks];
        for (var i = 0; i < this.MachineInformation.NumberOfTasks; i++)
        {
            this.cores[i] = new(this);
        }

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
        [Link(Primary = true, Name = "Sequential", Type = ChainType.QueueList)]
        [Link(Unique = true, Type = ChainType.Unordered)]
        public TIdentifier Identifier;

        [Key(1)]
        public TMachine Machine;

#pragma warning restore SA1401 // Fields should be private
    }

    public override MachineInformation MachineInformation { get; }

    private SequentialCore[] cores;
    private Item.GoshujinClass items;

    #region Abstract

    public override int Count
        => this.items.Count;

    public void Start()
    {
        var parent = ((IBigMachine)this.BigMachine).Core.GetParent();
        foreach (var x in this.cores)
        {
            x.Start(parent);
        }
    }

    public Machine.ManMachineInterface? GetFirst()
    {
        using (this.items.LockObject.EnterScope())
        {
            if (this.items.SequentialChain.TryPeek(out var item))
            {
                return item.Machine.InterfaceInstance;
            }

            return default;
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

    public override TIdentifier[] GetIdentifiers()
    {
        using (this.items.LockObject.EnterScope())
        {
            return this.items.Select(x => x.Identifier).ToArray();
        }
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

        var result = false;
        using (this.items.LockObject.EnterScope())
        {
            if (this.items.IdentifierChain.TryGetValue(m.Identifier, out var item))
            {
                item.Goshujin = null;
                result = true;
            }

            /*if (this.MachineInformation.NumberOfTasks <= 0)
            {// No dedicated tasks
                if (this.items.SequentialChain.TryPeek(out var first))
                {
                    var next = first.Machine;
                    if (next.InternalLifespan > 0 &&
                        next.OperationalState == 0 &&
                        next.InternalTimeUntilRun == 0)
                    {// Stand-by
                        next.RunAndForget(DateTime.UtcNow);
                    }
                }
            }*/
        }

        return result;
    }

    internal override void Process(MachineRunner runner)
    {
        using (this.items.LockObject.EnterScope())
        {
            if (this.MachineInformation.NumberOfTasks > 0)
            {// Have dedicated tasks
                foreach (var x in this.items)
                {
                    runner.Add(x.Machine);
                }
            }
            else
            {
                if (!this.items.SequentialChain.TryPeek(out var first))
                {
                    return;
                }

                var machine = first.Machine;
                if (machine.OperationalState == 0)
                {// Stand-by
                    runner.Add(machine);
                }
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
                this.PulseCore();
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
                this.PulseCore();
            }

            return (TInterface)item.Machine.InterfaceInstance;
        }
    }

    #endregion

    private void StartCore(ThreadCoreBase? parent)
    {
        foreach (var x in this.cores)
        {
            x.Start(parent);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PulseCore()
    {
        foreach (var x in this.cores)
        {
            x.Pulse();
        }
    }

    private TMachine? GetMachineToProcess()
    {
        using (this.items.LockObject.EnterScope())
        {
            if (!this.items.SequentialChain.TryPeek(out var item))
            {
                return default;
            }

            while (item.Machine.OperationalState.HasFlag(OperationalFlag.Running))
            {
                item = item.SequentialLink.Next;
                if (item is null)
                {
                    return default;
                }
            }

            return item.Machine;
        }
    }

    #region Tinyhand

    static void ITinyhandSerializable<SequentialMachineControl<TIdentifier, TMachine, TInterface>>.Serialize(ref TinyhandWriter writer, scoped ref SequentialMachineControl<TIdentifier, TMachine, TInterface>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        TinyhandSerializer.SerializeObject(ref writer, value.items, options);
    }

    static void ITinyhandSerializable<SequentialMachineControl<TIdentifier, TMachine, TInterface>>.Deserialize(ref TinyhandReader reader, scoped ref SequentialMachineControl<TIdentifier, TMachine, TInterface>? value, TinyhandSerializerOptions options)
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
