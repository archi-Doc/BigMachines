// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1202

namespace BigMachines.Control;

/// <summary>
/// Represents a class for managing single machine.<br/>
/// <see cref="SingleMachineControl{TMachine, TInterface}"/> = <see cref="MachineControl"/>+<typeparamref name="TMachine"/>+<typeparamref name="TInterface"/>.
/// </summary>
/// <typeparam name="TMachine">The type of a machine.</typeparam>
/// <typeparam name="TInterface">The type of an interface.</typeparam>
[TinyhandObject(Structual = true)]
public partial class SingleMachineControl<TMachine, TInterface> : MachineControl, ITinyhandSerialize<SingleMachineControl<TMachine, TInterface>>, ITinyhandCustomJournal
    where TMachine : Machine
    where TInterface : Machine.ManMachineInterface
{
    public SingleMachineControl()
    {
        this.MachineInformation = MachineRegistry.Get<TMachine>();
    }

    public void Prepare(BigMachineBase bigMachine)
    {
        this.BigMachine = bigMachine;
    }

    public override MachineInformation MachineInformation { get; }

    private TMachine? machine;

    /*private TMachine Machine
    {
        get
        {
            if (this.machine is not { } obj)
            {
                obj = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
                obj.PrepareStart(this);
                this.machine = obj;
            }

            return obj;
        }
    }*/

    public TInterface GetOrCreate(object? createParam = null)
        => (TInterface)this.GetOrCreateMachine(createParam).InterfaceInstance;

    public TInterface GetOrCreate()
        => (TInterface)this.GetOrCreateMachine().InterfaceInstance;

    public override int Count
        => this.machine is null ? 0 : 1;

    public override bool CheckActiveMachine()
    {
        if (this.machine?.IsActive == true)
        {
            return true;
        }

        return false;
    }

    public override Machine.ManMachineInterface[] GetArray()
    {
        if (this.machine?.InterfaceInstance is { } obj)
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
        if (this.machine is { } obj)
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
        if (this.machine == machine)
        {
            this.machine = null;
            return true;
        }
        else
        {
            return false;
        }
    }

    internal override void Process(DateTime now, TimeSpan elapsed)
    {
        this.machine?.Process(now, elapsed);
    }

    private TMachine GetOrCreateMachine(object? createParam)
    {
        if (this.machine is null)
        {
            var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
            machine.PrepareCreateStart(this, createParam);
            this.machine = machine;
        }

        return this.machine;
    }

    private TMachine GetOrCreateMachine()
    {
        if (this.machine is null)
        {
            var machine = MachineRegistry.CreateMachine<TMachine>(this.MachineInformation);
            machine.PrepareStart(this);
            this.machine = machine;
        }

        return this.machine;
    }

    static void ITinyhandSerialize<SingleMachineControl<TMachine, TInterface>>.Serialize(ref TinyhandWriter writer, scoped ref SingleMachineControl<TMachine, TInterface>? value, TinyhandSerializerOptions options)
    {
        TinyhandSerializer.Serialize(ref writer, value?.GetOrCreateMachine().InterfaceInstance, options);

        /*if (value?.machine is ITinyhandSerialize obj)
        {
            obj.Serialize(ref writer, options);
        }
        else
        {
            writer.WriteNil();
        }*/
    }

    static void ITinyhandSerialize<SingleMachineControl<TMachine, TInterface>>.Deserialize(ref TinyhandReader reader, scoped ref SingleMachineControl<TMachine, TInterface>? value, TinyhandSerializerOptions options)
    {
        value ??= new();
        value.machine = TinyhandSerializer.Deserialize<TMachine>(ref reader, options);
        value.machine?.PrepareStart(value);

        /*value ??= new();
        if (value.BigMachine is not null &&
            value.MachineInformation is not null)
        {
            var machine = value.BigMachine.CreateMachine(value.MachineInformation);
            if (machine is ITinyhandSerialize obj)
            {
                obj.Deserialize(ref reader, options);
                value.machine = machine;
            }
        }*/
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        if (this.GetOrCreateMachine() is IStructualObject obj)
        {
            return obj.ReadRecord(ref reader);
        }

        return false;
    }
}
