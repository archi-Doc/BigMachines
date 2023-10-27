// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using BigMachines.Redesign;
using Tinyhand;
using Tinyhand.IO;

namespace BigMachines;

[TinyhandObject]
public partial class TestBigMachine : BigMachineBase, ITinyhandSerialize<TestBigMachine>, IStructualObject
{
    public TestBigMachine()
    {
        this.TestMachines = new();
        this.TestMachines.Prepare(this);
        this.SingleMachine = new();
        this.SingleMachine.Prepare(this);
    }

    public UnorderedMachineControl<int, TestMachine, TestMachine.Interface> TestMachines { get; private set; }

    public SingleMachineControl<SingleMachine, SingleMachine.Interface> SingleMachine { get; private set; }

    static void ITinyhandSerialize<TestBigMachine>.Serialize(ref TinyhandWriter writer, scoped ref TestBigMachine? value, TinyhandSerializerOptions options)
    {
        throw new System.NotImplementedException();
    }

    static void ITinyhandSerialize<TestBigMachine>.Deserialize(ref TinyhandReader reader, scoped ref TestBigMachine? value, TinyhandSerializerOptions options)
    {
        throw new System.NotImplementedException();
    }

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
