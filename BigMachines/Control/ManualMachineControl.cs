// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

#pragma warning disable SA1202
#pragma warning disable SA1204

namespace BigMachines.Control;

[TinyhandObject]
public sealed partial class ManualMachineControl : MachineControl // , ITinyhandSerialize<ManualMachineControl>
{
    public ManualMachineControl()
        : base()
    {
    }

    public void Prepare(BigMachineBase bigMachine)
    {
        this.BigMachine = bigMachine;
    }

    /*[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private partial class Item
    {
        public Item()
        {
        }

        public Item(Type machineType, Machine machine)
        {
            this.MachineType = machineType;
            this.Machine = machine;
        }

#pragma warning disable SA1401 // Fields should be private

        [Link(Primary = true, Type = ChainType.Unordered)]
        public Type MachineType = default!;

        public Machine Machine = default!;

#pragma warning restore SA1401 // Fields should be private
    }*/

    public override MachineInformation MachineInformation => MachineInformation.Default;

    private object syncObject = new();
    private Dictionary<Type, Machine> typeToMachine = new();
    // private Item.GoshujinClass items = new();

    #region Abstract

    public override int Count
        => this.typeToMachine.Count;

    public override bool CheckActiveMachine()
    {
        lock (this.syncObject)
        {
            foreach (var x in this.typeToMachine.Values)
            {
                if (x.IsActive)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public override Machine.ManMachineInterface[] GetArray()
    {
        lock (this.syncObject)
        {
            return this.typeToMachine.Values.Select(x => x.InterfaceInstance).ToArray();
        }
    }

    internal override Machine[] GetMachines()
    {
        lock (this.syncObject)
        {
            return this.typeToMachine.Values.ToArray();
        }
    }

    internal override bool RemoveMachine(Machine machine)
    {
        lock (this.syncObject)
        {
            return this.typeToMachine.Remove(machine.GetType());
        }
    }

    internal override void Process(DateTime now, TimeSpan elapsed)
    {
        lock (this.syncObject)
        {
            foreach (var x in this.typeToMachine.Values)
            {
                x.Process(now, elapsed);
            }
        }
    }

    #endregion

    #region Main

    public Machine.ManMachineInterface? TryGet<TMachine>()
        where TMachine : Machine
    {
        lock (this.syncObject)
        {
            if (this.typeToMachine.TryGetValue(typeof(TMachine), out var item))
            {
                return item.InterfaceInstance;
            }
            else
            {
                return default;
            }
        }
    }

    public Machine.ManMachineInterface? TryCreate<TMachine>(object? createParam = null)
        where TMachine : Machine
    {
        lock (this.syncObject)
        {
            if (this.typeToMachine.TryGetValue(typeof(TMachine), out var machine))
            {
                return default;
            }
            else
            {
                machine = MachineRegistry.CreateMachine<TMachine>();
                if (machine is null)
                {
                    return default;
                }

                machine.PrepareCreateStart(this, createParam);
                this.typeToMachine.TryAdd(typeof(TMachine), machine);
            }

            return machine.InterfaceInstance;
        }
    }

    public Machine.ManMachineInterface GetOrCreate<TMachine>(object? createParam = null)
        where TMachine : Machine
    {
        lock (this.syncObject)
        {
            if (!this.typeToMachine.TryGetValue(typeof(TMachine), out var machine))
            {
                machine = MachineRegistry.CreateMachine<TMachine>();
                machine.PrepareCreateStart(this, createParam);
                this.typeToMachine.TryAdd(typeof(TMachine), machine);
            }

            return machine.InterfaceInstance;
        }
    }

    #endregion

    #region Tinyhand

    /*static void ITinyhandSerialize<ManualMachineControl>.Serialize(ref TinyhandWriter writer, scoped ref ManualMachineControl? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        TinyhandSerializer.SerializeObject(ref writer, value.items, options);
    }

    static void ITinyhandSerialize<ManualMachineControl>.Deserialize(ref TinyhandReader reader, scoped ref ManualMachineControl? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        value ??= new();
        value.items = TinyhandSerializer.DeserializeObject<Item.GoshujinClass>(ref reader, options) ?? new();
        foreach (var x in value.items)
        {
            x.Machine.Prepare(value);
        }
    }*/

    #endregion
}
