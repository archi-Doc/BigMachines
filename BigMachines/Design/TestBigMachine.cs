// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BigMachines.Control;
using Tinyhand;
using Tinyhand.IO;

namespace BigMachines;

[TinyhandObject]
public partial class TestBigMachine : BigMachineBase, ITinyhandSerialize<TestBigMachine>, ITinyhandReconstruct<TestBigMachine>, IStructualObject
{
    private MachineControl[] controls = Array.Empty<MachineControl>();

    public TestBigMachine()
    {
        this.TestMachines.Prepare(this);
        this.SingleMachine.Prepare(this);

        this.controls = new MachineControl[] { this.TestMachines, this.SingleMachine, };
    }

    public override MachineControl[] GetArray()
        => this.controls;

    private UnorderedMachineControl<int, TestMachine, TestMachine.Interface> testMachines = new();

    public UnorderedMachineControl<int, TestMachine, TestMachine.Interface> TestMachines => this.testMachines;

    private SingleMachineControl<SingleMachine, SingleMachine.Interface> singleMachine = new();

    public SingleMachineControl<SingleMachine, SingleMachine.Interface> SingleMachine => this.singleMachine;

    static void ITinyhandSerialize<TestBigMachine>.Serialize(ref TinyhandWriter writer, scoped ref TestBigMachine? value, TinyhandSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }

        var count = value.controls.Count(x => x.MachineInformation.Serializable);
        writer.WriteMapHeader(count);

        if (value.TestMachines.MachineInformation.Serializable)
        {
            writer.Write(12);
            TinyhandSerializer.SerializeObject(ref writer, value.TestMachines, options);
        }

        if (value.SingleMachine.MachineInformation.Serializable)
        {
            writer.Write(23);
            TinyhandSerializer.SerializeObject(ref writer, value.SingleMachine, options);
        }
    }

    static void ITinyhandSerialize<TestBigMachine>.Deserialize(ref TinyhandReader reader, scoped ref TestBigMachine? value, TinyhandSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return;
        }

        value ??= new();
        var count = reader.ReadMapHeader2();
        while (count-- > 0)
        {
            var id = reader.ReadInt32();
            if (id == 0)
            {
                TinyhandSerializer.DeserializeObject(ref reader, ref value.singleMachine!, options);
            }
            else if (id == 1)
            {
                TinyhandSerializer.DeserializeObject(ref reader, ref value.testMachines!, options);
            }
            else
            {
                reader.Skip();
            }
        }
    }

    static void ITinyhandReconstruct<TestBigMachine>.Reconstruct([NotNull] scoped ref TestBigMachine? value, TinyhandSerializerOptions options)
        => value ??= new();

    IStructualRoot? IStructualObject.StructualRoot { get; set; }

    IStructualObject? IStructualObject.StructualParent { get; set; }

    int IStructualObject.StructualKey { get; set; }

    bool IStructualObject.ReadRecord(ref TinyhandReader reader)
    {
        if (!reader.TryRead(out JournalRecord record))
        {
            return false;
        }

        if (record == JournalRecord.Key)
        {
            var id = reader.ReadUInt32();
            if (id == 0)
            {
                return ((IStructualObject)this.TestMachines).ReadRecord(ref reader);
            }
        }

        return false;
    }
}
