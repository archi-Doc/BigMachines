// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1202
#pragma warning disable SA1204

namespace BigMachines.Redesign;

/// <summary>
/// Represents a class for managing single machine.<br/>
/// <see cref="SingleMachineControl{TInterface}"/> = <see cref="MachineControl"/>+<typeparamref name="TInterface"/>.
/// </summary>
/// <typeparam name="TInterface">The type of an interface.</typeparam>
[TinyhandObject(Structual = true)]
public partial class SingleMachineControl<TMachine, TInterface> : MachineControl, ITinyhandSerialize<SingleMachineControl<TMachine, TInterface>>, ITinyhandCustomJournal
    where TInterface : Machine.ManMachineInterface
{
    internal SingleMachineControl()
         : base(default!)
    {// This code has some issues, but since it's never called directly, it's probably fine.
        this.createInterface = default!;
    }

    public SingleMachineControl(BigMachineBase bigMachine, Func<SingleMachineControl<TMachine, TInterface>, TInterface> createInterface)
    : base(bigMachine)
    {
        this.createInterface = createInterface;
    }

    private Func<SingleMachineControl<TMachine, TInterface>, TInterface> createInterface; // MachineControl -> Machine.Interface
    private TInterface? @interface;

    public TInterface Get()
        => this.@interface ??= this.createInterface(this);

    internal override bool RemoveMachine(Machine machine)
    {
        if (this.@interface?.Machine == machine)
        {
            this.@interface = null;
            return true;
        }
        else
        {
            return false;
        }
    }

    static void ITinyhandSerialize<SingleMachineControl<TMachine, TInterface>>.Serialize(ref TinyhandWriter writer, scoped ref SingleMachineControl<TMachine, TInterface>? value, TinyhandSerializerOptions options)
    {
        if (value?.@interface?.Machine is TMachine machine)
        {
            options.Resolver.GetFormatter<TMachine>().Serialize(ref writer, machine, options);
        }
        else
        {
            writer.WriteNil();
        }
    }

    static void ITinyhandSerialize<SingleMachineControl<TMachine, TInterface>>.Deserialize(ref TinyhandReader reader, scoped ref SingleMachineControl<TMachine, TInterface>? value, TinyhandSerializerOptions options)
    {
        throw new NotImplementedException();
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
