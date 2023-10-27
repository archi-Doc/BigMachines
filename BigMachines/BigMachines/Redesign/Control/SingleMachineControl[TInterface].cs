﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1202

namespace BigMachines.Redesign;

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
    }

    public void Prepare(BigMachineBase bigMachine)
    {
        this.BigMachine = bigMachine;
    }

    private TMachine? machine;

    private TMachine Machine
    {
        get
        {
            if (this.machine is not { } obj)
            {
                /*if (this.BigMachine is null)
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
                }*/

                obj = MachineRegistry.CreateMachine<TMachine>();
                obj.Prepare(this);
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

    static void ITinyhandSerialize<SingleMachineControl<TMachine, TInterface>>.Serialize(ref TinyhandWriter writer, scoped ref SingleMachineControl<TMachine, TInterface>? value, TinyhandSerializerOptions options)
    {
        TinyhandSerializer.Serialize(ref writer, value?.Machine, options);

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
        value.machine?.Prepare(value);

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