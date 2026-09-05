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
/// <typeparam name="TInterface">The generated machine interface type.</typeparam>
[TinyhandObject(Structural = true)]
public partial class SingleMachineControl<TMachine, TInterface> : MachineControl, ITinyhandSerializable<SingleMachineControl<TMachine, TInterface>>, ITinyhandCustomJournal, ITinyhandSingleLayoutSerializable
    where TMachine : Machine
    where TInterface : Machine.ManMachineInterface
{
    #region FieldAndProperty

    private readonly Lock lockObject = new();
    private TMachine? machine;

    public override int Count
        => Volatile.Read(ref this.machine) is null ? 0 : 1;

    #endregion

    public SingleMachineControl()
    {
        this.MachineInformation = MachineRegistry.Get<TMachine>();
    }

    public void Prepare(BigMachineBase bigMachine)
    {
        this.BigMachine = bigMachine;
    }

    public override MachineInformation MachineInformation { get; }

    /// <summary>
    /// Attempts to retrieve the machine interface if a machine exists.
    /// </summary>
    /// <param name="machineInterface">
    /// When this method returns, contains the machine interface of type <typeparamref name="TInterface"/> if a machine exists; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a machine exists and the interface was successfully retrieved; otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGet([MaybeNullWhen(false)] out TInterface machineInterface)
    {
        machineInterface = Volatile.Read(ref this.machine)?.InterfaceInstance as TInterface;
        return machineInterface is not null;
    }

    /// <summary>
    /// Gets an existing machine interface or creates a new machine with the specified creation parameters.
    /// </summary>
    /// <param name="createParam">The parameters to pass to <see cref="Machine.OnCreate(object?)"/> when creating a new machine.</param>
    /// <returns>The machine interface of type <typeparamref name="TInterface"/>.</returns>
    public TInterface GetOrCreate(object? createParam = null)
        => (TInterface)this.GetOrCreateMachine(createParam).InterfaceInstance;

    /// <summary>
    /// Gets an existing machine interface or creates a new machine without creation parameters.
    /// </summary>
    /// <returns>The machine interface of type <typeparamref name="TInterface"/>.</returns>
    public TInterface GetOrCreate()
        => (TInterface)this.GetOrCreateMachine().InterfaceInstance;

    /// <summary>
    /// Terminates any existing machine and creates a new machine with the specified creation parameters.
    /// </summary>
    /// <param name="createParam">The parameters to pass to <see cref="Machine.OnCreate(object?)"/> when creating the new machine.</param>
    /// <returns>The machine interface of type <typeparamref name="TInterface"/> for the newly created machine.</returns>
    public TInterface CreateAlways(object? createParam = null)
        => (TInterface)this.CreateAlwaysMachine(createParam).InterfaceInstance;

    /// <summary>
    /// Terminates any existing machine and creates a new machine without creation parameters.
    /// </summary>
    /// <returns>The machine interface of type <typeparamref name="TInterface"/> for the newly created machine.</returns>
    public TInterface CreateAlways()
        => (TInterface)this.CreateAlwaysMachine().InterfaceInstance;

    public override bool ContainsActiveMachine()
    {
        if (Volatile.Read(ref this.machine)?.IsActive == true)
        {
            return true;
        }

        return false;
    }

    public override Machine.ManMachineInterface[] GetArray()
    {
        if (Volatile.Read(ref this.machine)?.InterfaceInstance is { } obj)
        {
            return new Machine.ManMachineInterface[] { obj, };
        }
        else
        {
            return Array.Empty<Machine.ManMachineInterface>();
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

    private TMachine GetOrCreateMachine(object? createParam)
    {
        using (this.lockObject.EnterScope())
        {
            if (this.machine is null)
            {
                var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
                machine.PrepareCreateStart(this, createParam);
                Volatile.Write(ref this.machine, machine);
            }

            return this.machine;
        }
    }

    private TMachine GetOrCreateMachine()
    {
        using (this.lockObject.EnterScope())
        {
            if (this.machine is null)
            {
                var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
                machine.PrepareStart(this);
                Volatile.Write(ref this.machine, machine);
            }

            return this.machine;
        }
    }

    private TMachine CreateAlwaysMachine(object? createParam)
    {
        Machine.ManMachineInterface? machineInterface = default;

Loop:
        if (machineInterface is not null)
        {
            machineInterface.TerminateMachine();
        }

        using (this.lockObject.EnterScope())
        {
            machineInterface = this.machine?.InterfaceInstance;
            if (machineInterface is not null)
            {
                goto Loop;
            }

            var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
            machine.PrepareCreateStart(this, createParam);
            Volatile.Write(ref this.machine, machine);
            return machine;
        }
    }

    private TMachine CreateAlwaysMachine()
    {
        Machine.ManMachineInterface? machineInterface = default;

Loop:
        if (machineInterface is not null)
        {
            machineInterface.TerminateMachine();
        }

        using (this.lockObject.EnterScope())
        {
            machineInterface = this.machine?.InterfaceInstance;
            if (machineInterface is not null)
            {
                goto Loop;
            }

            var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
            machine.PrepareStart(this);
            Volatile.Write(ref this.machine, machine);
            return machine;
        }
    }

    static void ITinyhandSerializable<SingleMachineControl<TMachine, TInterface>>.Serialize(ref TinyhandWriter writer, scoped ref SingleMachineControl<TMachine, TInterface>? value, TinyhandSerializerOptions options)
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

    static void ITinyhandSerializable<SingleMachineControl<TMachine, TInterface>>.Deserialize(ref TinyhandReader reader, scoped ref SingleMachineControl<TMachine, TInterface>? value, TinyhandSerializerOptions options)
    {
        value ??= new();
        value.machine = TinyhandSerializer.Deserialize<TMachine>(ref reader, options);
        value.machine?.PrepareStart(value);

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
        if (this.GetOrCreateMachine() is IStructuralObject obj)
        {
            return obj.ProcessJournalRecord(ref reader);
        }

        return false;
    }
}
