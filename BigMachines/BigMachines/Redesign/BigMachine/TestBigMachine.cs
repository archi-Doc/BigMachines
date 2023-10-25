// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using BigMachines.Redesign;
using Tinyhand;
using Tinyhand.IO;

namespace BigMachines;

[TinyhandObject]
public partial class TestBigMachine : BigMachineBase, ITinyhandSerialize<TestBigMachine>
{
    public TestBigMachine()
    {
        this.TestMachines = new(this, (x, y) => new TestMachine(x, y).InterfaceInstance, x => new TestMachine.Interface.CommandAll(x));
        this.SingleMachine = new(this, x => new SingleMachine(x).InterfaceInstance, default);
    }

    public UnorderedMachineControl<int, TestMachine.Interface, TestMachine.Interface.CommandAll> TestMachines { get; private set; }

    public SingleMachineControl<SingleMachine.Interface> SingleMachine { get; private set; }

    static void ITinyhandSerialize<TestBigMachine>.Serialize(ref TinyhandWriter writer, scoped ref TestBigMachine? value, TinyhandSerializerOptions options)
    {
        throw new System.NotImplementedException();
    }

    static void ITinyhandSerialize<TestBigMachine>.Deserialize(ref TinyhandReader reader, scoped ref TestBigMachine? value, TinyhandSerializerOptions options)
    {
        throw new System.NotImplementedException();
    }
}
