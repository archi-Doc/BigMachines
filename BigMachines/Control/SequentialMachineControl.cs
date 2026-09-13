// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Arc.Threading;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

#pragma warning disable SA1202
#pragma warning disable SA1204

namespace BigMachines.Control;

/// <summary>
/// Provides non-generic operations for a sequential machine control.
/// </summary>
public interface ISequentialMachineControl
{
    /// <summary>
    /// Starts dedicated workers and wakes them for existing queued machines.
    /// </summary>
    void Start();

    /// <summary>
    /// Gets the first queued machine, including a paused or running machine.
    /// </summary>
    /// <returns>The first handle, or <see langword="null"/> when empty.</returns>
    Machine.MachineHandle? PeekFirst();
}

/// <summary>
/// Manages identified machines in a FIFO queue with optional dedicated workers.
/// </summary>
/// <typeparam name="TIdentifier">The machine identifier type.</typeparam>
/// <typeparam name="TMachine">The machine type.</typeparam>
/// <typeparam name="THandle">The generated machine handle type.</typeparam>
[TinyhandObject]
public sealed partial class SequentialMachineControl<TIdentifier, TMachine, THandle> : MultiMachineControl<TIdentifier, THandle>, ISequentialMachineControl, ITinyhandSerializable<SequentialMachineControl<TIdentifier, TMachine, THandle>>, ITinyhandCustomJournal, ITinyhandSingleLayoutSerializable
    where TIdentifier : notnull
    where TMachine : Machine<TIdentifier>
    where THandle : Machine.MachineHandle
{
    public SequentialMachineControl()
        : base()
    {
        this.MachineInformation = MachineRegistry.GetInformation<TMachine>();
        this.cores = [];
        this.items = new();
    }

    public void Attach(BigMachineBase bigMachine)
    {
        this.BigMachine = bigMachine;
        this.cores = new SequentialCore[this.MachineInformation.WorkerCount];
        for (var i = 0; i < this.MachineInformation.WorkerCount; i++)
        {
            this.cores[i] = new(this.BigMachine.ExecutionGroup, this);
        }

        this.RestoreStructure();
    }

    /// <summary>
    /// Registers the formatter for the closed generic item type.
    /// </summary>
    public static void RegisterTinyhandFormatter()
        => Tinyhand.Resolvers.GeneratedResolver.RegisterObject<Item>();

    protected override void RestoreStructure()
    {
        using (this.items.LockObject.EnterScope())
        {
            ((IStructuralObject)this.items).SetupStructure(this);
            foreach (var item in this.items)
            {
                item.RestoreStructure();
            }
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

        public void RestoreStructure()
        {
            if (this.Machine is IStructuralObject child)
            {
                child.SetupStructure(this, 1);
            }
        }

        public bool ReadMachineRecord(ref TinyhandReader reader)
        {
            return reader.TryReadJournalRecord(out var record) &&
                record == JournalRecordType.Key &&
                reader.ReadInt32() == 1 &&
                this.Machine is IStructuralObject child &&
                child.ProcessJournalRecord(ref reader);
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
    {
        get
        {
            using (this.items.LockObject.EnterScope())
            {
                return this.items.Count;
            }
        }
    }

    /// <inheritdoc/>
    public void Start()
    {
        foreach (var x in this.cores)
        {
            x.Start();
        }

        this.PulseCore();
    }

    /// <inheritdoc/>
    public Machine.MachineHandle? PeekFirst()
    {
        using (this.items.LockObject.EnterScope())
        {
            if (this.items.SequentialChain.TryPeek(out var item))
            {
                return item.Machine.HandleInstance;
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
            var result = this.items.Count == 0 ? Array.Empty<TIdentifier>() : new TIdentifier[this.items.Count];
            var index = 0;
            foreach (var item in this.items)
            {
                result[index++] = item.Identifier;
            }

            return result;
        }
    }

    public override THandle[] GetHandles()
    {
        using (this.items.LockObject.EnterScope())
        {
            var result = this.items.Count == 0 ? Array.Empty<THandle>() : new THandle[this.items.Count];
            var index = 0;
            foreach (var item in this.items)
            {
                result[index++] = (THandle)item.Machine.HandleInstance;
            }

            return result;
        }
    }

    internal override TMachine[] GetMachines()
    {
        using (this.items.LockObject.EnterScope())
        {
            var result = this.items.Count == 0 ? Array.Empty<TMachine>() : new TMachine[this.items.Count];
            var index = 0;
            foreach (var item in this.items)
            {
                result[index++] = item.Machine;
            }

            return result;
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
            if (this.items.IdentifierChain.TryGetValue(m.Identifier, out var item) && ReferenceEquals(item.Machine, machine))
            {
                item.Goshujin = null;
                result = true;
            }

            /*if (this.MachineInformation.WorkerCount <= 0)
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
            if (this.MachineInformation.WorkerCount > 0)
            {// Have dedicated tasks
                foreach (var x in this.items)
                {
                    runner.AddLifespan(x.Machine);
                }
            }
            else
            {
                if (!this.items.SequentialChain.TryPeek(out var first))
                {
                    return;
                }

                foreach (var item in this.items)
                {
                    if (ReferenceEquals(item, first) && item.Machine.OperationalState == 0)
                    {
                        runner.Add(item.Machine);
                    }
                    else
                    {
                        runner.AddLifespan(item.Machine);
                    }
                }
            }
        }
    }

    #endregion

    #region Main

    /// <summary>
    /// Gets the handle associated with an identifier.
    /// </summary>
    /// <param name="identifier">The machine identifier.</param>
    /// <returns>The handle, or <see langword="null"/> when absent.</returns>
    public THandle? Find(TIdentifier identifier)
    {
        using (this.items.LockObject.EnterScope())
        {
            if (this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                return (THandle)item.Machine.HandleInstance;
            }
            else
            {
                return default;
            }
        }
    }

    /// <summary>
    /// Creates and queues a machine if the identifier is unused.
    /// </summary>
    /// <param name="identifier">The machine identifier.</param>
    /// <param name="createParameter">The value passed to the creation callback.</param>
    /// <returns>The new handle, or <see langword="null"/> if the identifier exists.</returns>
    public THandle? TryCreate(TIdentifier identifier, object? createParameter = null)
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
                machine.PrepareCreateStart(this, createParameter);
                item = new(identifier, machine);
                item.Goshujin = this.items;
                item.RestoreStructure();
                this.PulseCore();
            }

            return (THandle)item.Machine.HandleInstance;
        }
    }

    /// <summary>
    /// Gets the existing handle or creates and queues a machine atomically.
    /// </summary>
    /// <param name="identifier">The machine identifier.</param>
    /// <param name="createParameter">The value passed to the creation callback when creating a machine.</param>
    /// <returns>The existing or new handle.</returns>
    public THandle GetOrCreate(TIdentifier identifier, object? createParameter = null)
    {
        using (this.items.LockObject.EnterScope())
        {
            if (!this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
                machine.Identifier = identifier;
                machine.PrepareCreateStart(this, createParameter);
                item = new(identifier, machine);
                item.Goshujin = this.items;
                item.RestoreStructure();
                this.PulseCore();
            }

            return (THandle)item.Machine.HandleInstance;
        }
    }

    #endregion

    internal override void OnMachineResumed()
        => this.PulseCore();

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

            while (item.Machine.OperationalState != 0 || !item.Machine.TryReserveRun())
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

    static void ITinyhandSerializable<SequentialMachineControl<TIdentifier, TMachine, THandle>>.Serialize(ref TinyhandWriter writer, scoped ref SequentialMachineControl<TIdentifier, TMachine, THandle>? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        TinyhandSerializer.SerializeObject(ref writer, value.items, options);
    }

    static void ITinyhandSerializable<SequentialMachineControl<TIdentifier, TMachine, THandle>>.Deserialize(ref TinyhandReader reader, scoped ref SequentialMachineControl<TIdentifier, TMachine, THandle>? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            if (value is not null)
            {
                value.items = new();
                value.RestoreStructure();
            }

            return;
        }

        value ??= new();
        var restored = TinyhandSerializer.DeserializeObject<Item.GoshujinClass>(ref reader, options) ?? new();
        foreach (var x in restored)
        {
            if (x.Machine is null || !System.Collections.Generic.EqualityComparer<TIdentifier>.Default.Equals(x.Identifier, x.Machine.Identifier))
            {
                throw new TinyhandException("The stored machine and control identifiers do not match.");
            }
        }

        foreach (var item in restored)
        {
            item.Machine.PrepareStart(value);
        }

        value.items = restored;
        value.RestoreStructure();
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        using (this.items.LockObject.EnterScope())
        {
            var fork = reader.Fork();
            if (fork.TryReadJournalRecord(out var record) && record == JournalRecordType.Locator)
            {
                var identifier = TinyhandSerializer.Deserialize<TIdentifier>(ref fork);
                if (identifier is null || !this.items.IdentifierChain.TryGetValue(identifier, out var item) ||
                    !item.ReadMachineRecord(ref fork))
                {
                    return false;
                }

                reader = fork;
                return true;
            }

            if (!((IStructuralObject)this.items).ProcessJournalRecord(ref reader))
            {
                return false;
            }

            foreach (var item in this.items)
            {
                if (!item.Machine.IsPreparedFor(this))
                {
                    item.Machine.PrepareStart(this);
                }

                item.RestoreStructure();
            }
        }

        return true;
    }

    #endregion
}
