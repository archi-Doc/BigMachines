// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

#pragma warning disable SA1202
#pragma warning disable SA1204

namespace BigMachines.Control;

[TinyhandObject]
public sealed partial class ManualMachineControl : MachineControl // , ITinyhandSerializable<ManualMachineControl>
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

    private Lock lockObject = new();
    private Dictionary<Type, Machine> typeToMachine = new();
    // private Item.GoshujinClass items = new();

    #region Abstract

    public override int Count
        => this.typeToMachine.Count;

    public override bool CheckActiveMachine()
    {
        using (this.lockObject.EnterScope())
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
        using (this.lockObject.EnterScope())
        {
            return this.typeToMachine.Values.Select(x => x.InterfaceInstance).ToArray();
        }
    }

    internal override Machine[] GetMachines()
    {
        using (this.lockObject.EnterScope())
        {
            return this.typeToMachine.Values.ToArray();
        }
    }

    internal override bool RemoveMachine(Machine machine)
    {
        using (this.lockObject.EnterScope())
        {
            return this.typeToMachine.Remove(machine.GetType());
        }
    }

    internal override void Process(DateTime utcNow, TimeSpan elapsed)
    {
        using (this.lockObject.EnterScope())
        {
            foreach (var x in this.typeToMachine.Values)
            {
                x.Process(utcNow, elapsed);
            }
        }
    }

    #endregion

    #region Main

    public Machine.ManMachineInterface? TryGet<TMachine>()
        where TMachine : Machine
    {
        using (this.lockObject.EnterScope())
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
        using (this.lockObject.EnterScope())
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
        using (this.lockObject.EnterScope())
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

    /*static void ITinyhandSerializable<ManualMachineControl>.Serialize(ref TinyhandWriter writer, scoped ref ManualMachineControl? value, TinyhandSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        TinyhandSerializer.SerializeObject(ref writer, value.items, options);
    }

    static void ITinyhandSerializable<ManualMachineControl>.Deserialize(ref TinyhandReader reader, scoped ref ManualMachineControl? value, TinyhandSerializerOptions options)
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
