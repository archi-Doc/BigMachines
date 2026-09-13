// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Arc.Threading;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1202

namespace BigMachines.Control;

/// <summary>
/// Manages at most one instance of a machine type.
/// </summary>
/// <typeparam name="TMachine">The machine type.</typeparam>
/// <typeparam name="THandle">The generated machine handle type.</typeparam>
[TinyhandObject]
public partial class SingleMachineControl<TMachine, THandle> : MachineControl, ITinyhandSerializable<SingleMachineControl<TMachine, THandle>>, ITinyhandCustomJournal, ITinyhandSingleLayoutSerializable
    where TMachine : Machine
    where THandle : Machine.MachineHandle
{
    #region FieldAndProperty

    private readonly Lock lockObject = new();
    private TMachine? machine;

    public override int Count
        => Volatile.Read(ref this.machine) is null ? 0 : 1;

    #endregion

    public SingleMachineControl()
    {
        this.MachineInformation = MachineRegistry.GetInformation<TMachine>();
    }

    public void Attach(BigMachineBase bigMachine)
    {
        this.BigMachine = bigMachine;
    }

    public override MachineInformation MachineInformation { get; }

    protected override void RestoreStructure()
    {
        if (Volatile.Read(ref this.machine) is IStructuralObject child)
        {
            child.SetupStructure(this);
        }
    }

    /// <summary>
    /// Attempts to retrieve the machine handle if a machine exists.
    /// </summary>
    /// <param name="handle">
    /// When this method returns, contains the machine handle of type <typeparamref name="THandle"/> if a machine exists; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a machine exists and the handle was successfully retrieved; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGet([MaybeNullWhen(false)] out THandle handle)
    {
        handle = Volatile.Read(ref this.machine)?.HandleInstance as THandle;
        return handle is not null;
    }

    /// <summary>
    /// Gets an existing machine handle or creates a new machine with the specified creation parameters.
    /// </summary>
    /// <param name="createParameter">The parameters to pass to <see cref="Machine.OnCreate(object?)"/> when creating a new machine.</param>
    /// <returns>The machine handle of type <typeparamref name="THandle"/>.</returns>
    public THandle GetOrCreate(object? createParameter = null)
        => (THandle)this.GetOrCreateMachine(createParameter).HandleInstance;

    /// <summary>
    /// Gets an existing machine handle or creates a new machine without creation parameters.
    /// </summary>
    /// <returns>The machine handle of type <typeparamref name="THandle"/>.</returns>
    public THandle GetOrCreate()
        => (THandle)this.GetOrCreateMachine().HandleInstance;

    /// <summary>
    /// Terminates any existing machine and creates a new machine with the specified creation parameters.
    /// </summary>
    /// <param name="createParameter">The parameters to pass to <see cref="Machine.OnCreate(object?)"/> when creating the new machine.</param>
    /// <returns>The machine handle of type <typeparamref name="THandle"/> for the newly created machine.</returns>
    public THandle CreateOrReplace(object? createParameter = null)
        => (THandle)this.CreateOrReplaceMachine(createParameter).HandleInstance;

    /// <summary>
    /// Terminates any existing machine and creates a new machine without creation parameters.
    /// </summary>
    /// <returns>The machine handle of type <typeparamref name="THandle"/> for the newly created machine.</returns>
    public THandle CreateOrReplace()
        => (THandle)this.CreateOrReplaceMachine().HandleInstance;

    public override bool ContainsActiveMachine()
    {
        if (Volatile.Read(ref this.machine)?.IsActive == true)
        {
            return true;
        }

        return false;
    }

    public override Machine.MachineHandle[] GetHandles()
    {
        if (Volatile.Read(ref this.machine)?.HandleInstance is { } obj)
        {
            return new Machine.MachineHandle[] { obj, };
        }
        else
        {
            return Array.Empty<Machine.MachineHandle>();
        }
    }

    internal override Machine[] GetMachines()
    {
        if (Volatile.Read(ref this.machine) is { } obj)
        {
            return new Machine[] { obj, };
        }
        else
        {
            return Array.Empty<Machine>();
        }
    }

    internal override bool RemoveMachine(Machine machine)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.machine == machine)
            {
                this.WriteJournal(null);
                Volatile.Write(ref this.machine, null);
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
        if (Volatile.Read(ref this.machine) is { } machine)
        {
            runner.Add(machine);
        }
    }

    private TMachine GetOrCreateMachine(object? createParameter)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.machine is null)
            {
                var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
                machine.PrepareCreateStart(this, createParameter);
                this.WriteJournal(machine);
                Volatile.Write(ref this.machine, machine);
            }

            return this.machine;
        }
    }

    private TMachine GetOrCreateMachine()
        => this.GetOrCreateMachine(null);

    private TMachine CreateOrReplaceMachine(object? createParameter)
    {
        Machine.MachineHandle? handle = default;

Loop:
        if (handle is not null)
        {
            handle.Terminate();
        }

        using (this.lockObject.EnterScope())
        {
            handle = this.machine?.HandleInstance;
            if (handle is not null)
            {
                goto Loop;
            }

            var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
            machine.PrepareCreateStart(this, createParameter);
            this.WriteJournal(machine);
            Volatile.Write(ref this.machine, machine);
            return machine;
        }
    }

    private TMachine CreateOrReplaceMachine()
        => this.CreateOrReplaceMachine(null);

    static void ITinyhandSerializable<SingleMachineControl<TMachine, THandle>>.Serialize(ref TinyhandWriter writer, scoped ref SingleMachineControl<TMachine, THandle>? value, TinyhandSerializerOptions options)
    {
        var machine = value is null ? null : Volatile.Read(ref value.machine);
        TinyhandSerializer.Serialize(ref writer, machine, options);

        /*if (value?.machine is ITinyhandSerializable obj)
        {
            obj.Serialize(ref writer, options);
        }
        else
        {
            writer.WriteNil();
        }*/
    }

    static void ITinyhandSerializable<SingleMachineControl<TMachine, THandle>>.Deserialize(ref TinyhandReader reader, scoped ref SingleMachineControl<TMachine, THandle>? value, TinyhandSerializerOptions options)
    {
        value ??= new();
        var restored = TinyhandSerializer.Deserialize<TMachine>(ref reader, options);
        restored?.PrepareStart(value);
        Volatile.Write(ref value.machine, restored);
        value.RestoreStructure();

        /*value ??= new();
        if (value.BigMachine is not null &&
            value.MachineInformation is not null)
        {
            var machine = value.BigMachine.CreateMachine(value.MachineInformation);
            if (machine is ITinyhandSerializable obj)
            {
                obj.Deserialize(ref reader, options);
                value.machine = machine;
            }
        }*/
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        var fork = reader.Fork();
        if (fork.TryReadJournalRecord(out var record))
        {
            if (record == JournalRecordType.AddItem)
            {
                var restored = TinyhandSerializer.Deserialize<TMachine>(ref fork);
                if (restored is null)
                {
                    return false;
                }

                restored.PrepareStart(this);
                Volatile.Write(ref this.machine, restored);
                reader = fork;
                return true;
            }
            else if (record == JournalRecordType.DeleteItem)
            {
                Volatile.Write(ref this.machine, null);
                reader = fork;
                return true;
            }
        }

        TMachine machine;
        using (this.lockObject.EnterScope())
        {
            machine = this.machine!;
            if (machine is null)
            {
                machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
                machine.PrepareStart(this);
                Volatile.Write(ref this.machine, machine);
            }
        }

        if (machine is IStructuralObject obj)
        {
            return obj.ProcessJournalRecord(ref reader);
        }

        return false;
    }

    private void WriteJournal(TMachine? machine)
    {
        var structural = (IStructuralObject)this;
        if (structural.TryGetJournalWriter(out var root, out var writer, true))
        {
            writer.Write(machine is null ? JournalRecordType.DeleteItem : JournalRecordType.AddItem);
            if (machine is not null)
            {
                TinyhandSerializer.Serialize(ref writer, machine);
            }

            root.AddJournalAndDispose(ref writer);
        }

        structural.StructuralRoot?.AddToSaveQueue();
    }
}
