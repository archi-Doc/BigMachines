// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
        this.machineInformation = machineInformation;
    }

    private MachineInformation? machineInformation;
    private TInterface? interfaceInstance;

    public TInterface Get()
    {
        if (this.interfaceInstance is not { } obj)
        {
            if (this.BigMachine is null)
            {
                throw new InvalidOperationException("Unable to create an instance of the machine.");
            }
            obj = this.BigMachine?.CreateMachine(this.machineInformation) as TInterface;
            if (obj is null)
            {
                throw new InvalidOperationException("Unable to create an instance of the machine.");
            }

            this.interfaceInstance = obj;
        }

        return obj;
    }

    internal override bool RemoveMachine(Machine machine)
    {
        if (this.interfaceInstance?.Machine == machine)
        {
            this.interfaceInstance = null;
            return true;
        }
        else
        {
            return false;
        }
    }

    static void ITinyhandSerialize<SingleMachineControl<TInterface>>.Serialize(ref TinyhandWriter writer, scoped ref SingleMachineControl<TInterface>? value, TinyhandSerializerOptions options)
    {
        if (value?.interfaceInstance?.Machine is ITinyhandSerialize obj)
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
        if (value.machineInformation?.Constructor is { } constructor)
        {
            var machine = constructor();
            if (machine is ITinyhandSerialize obj)
            {
                obj.Deserialize(ref reader, options);
            }
        }
    }

    bool ITinyhandCustomJournal.ReadCustomRecord(ref TinyhandReader reader)
    {
        if (this.Get().Machine is IStructualObject obj)
        {
            return obj.ReadRecord(ref reader);
        }
        else
        {
            return false;
        }
    }
}
