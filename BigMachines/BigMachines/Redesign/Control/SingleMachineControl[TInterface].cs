// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1202

namespace BigMachines.Redesign;

/// <summary>
/// Represents a class for managing single machine.<br/>
/// <see cref="SingleMachineControl{TInterface}"/> = <see cref="MachineControl"/>+<typeparamref name="TInterface"/>.
/// </summary>
/// <typeparam name="TInterface">The type of an interface.</typeparam>
[TinyhandObject(Structual = true)]
public partial class SingleMachineControl<TInterface> : MachineControl, ITinyhandSerialize<SingleMachineControl<TInterface>>, ITinyhandCustomJournal
    where TInterface : Machine.ManMachineInterface
{
    public SingleMachineControl()
    {
    }

    public void Prepare(BigMachineBase bigMachine, MachineInformation machineInformation)
    {
        this.BigMachine = bigMachine;
        this.MachineInformation = machineInformation;
    }

    private Machine? machine;

    private Machine Machine
    {
        get
        {
            if (this.machine is not { } obj)
            {
                if (this.BigMachine is null)
                {
                    throw new InvalidOperationException("Call Prepare() function to specify a valid BigMachine object.");
                }
                else if (this.MachineInformation is null)
                {
                    throw new InvalidOperationException("Call Prepare() function to specify a valid machine information.");
                }

                obj = this.BigMachine.CreateMachine(this.MachineInformation);
                if (obj is null)
                {
                    throw new InvalidOperationException("Unable to create an instance of the machine.");
                }

                this.machine = obj;
            }

            return obj;
        }
    }

    public TInterface Get()
        => (TInterface)this.Machine.InterfaceInstance;

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

    static void ITinyhandSerialize<SingleMachineControl<TInterface>>.Serialize(ref TinyhandWriter writer, scoped ref SingleMachineControl<TInterface>? value, TinyhandSerializerOptions options)
    {
        if (value?.machine is ITinyhandSerialize obj)
        {
            obj.Serialize(ref writer, options);
        }
        else
        {
            writer.WriteNil();
        }
    }

    static void ITinyhandSerialize<SingleMachineControl<TInterface>>.Deserialize(ref TinyhandReader reader, scoped ref SingleMachineControl<TInterface>? value, TinyhandSerializerOptions options)
    {
        value ??= new();
        if (value.BigMachine is not null &&
            value.MachineInformation is not null)
        {
            var machine = value.BigMachine.CreateMachine(value.MachineInformation);
            if (machine is ITinyhandSerialize obj)
            {
                obj.Deserialize(ref reader, options);
                value.machine = machine;
            }
        }
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        if (this.Machine is IStructualObject obj)
        {
            return obj.ReadRecord(ref reader);
        }
        else
        {
            return false;
        }
    }
}
