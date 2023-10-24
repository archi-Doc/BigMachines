// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using BigMachines.Redesign;
using Tinyhand;
using Tinyhand.IO;

namespace BigMachines;

public abstract class BigMachineBase
{
    /// <summary>
    /// Gets <see cref="IServiceProvider"/> used to create instances of <see cref="Machine{TIdentifier}"/>.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; }
}

[TinyhandObject]
public partial class TestBigMachine : ITinyhandSerialize<TestBigMachine>
{
    public TestBigMachine()
    {
    }

    [IgnoreMember]
    public UnorderedMachineControl<int, TestMachine.Interface> TestMachine { get; private set; } = new((x, y) => new TestMachine(x, y).InterfaceInstance);

    static void ITinyhandSerialize<TestBigMachine>.Serialize(ref TinyhandWriter writer, scoped ref TestBigMachine? value, TinyhandSerializerOptions options)
    {
        throw new System.NotImplementedException();
    }

    static void ITinyhandSerialize<TestBigMachine>.Deserialize(ref TinyhandReader reader, scoped ref TestBigMachine? value, TinyhandSerializerOptions options)
    {
        throw new System.NotImplementedException();
    }
}
