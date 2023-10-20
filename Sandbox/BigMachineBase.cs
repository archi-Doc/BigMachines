// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sandbox;
using Tinyhand;
using Tinyhand.IO;
using static SimpleCommandLine.SimpleParser;

namespace BigMachines;

[TinyhandObject(Tree = true, ExplicitKeyOnly = true)]
public partial class BigMachineA
{
    public BigMachineA()
    {
    }

    [KeyAsName]
    private TestMachine machine1 = default!;
    public ManMachineInterface<int, Sandbox.TestMachine.State, Sandbox.TestMachine.Command> Machine1 => this.machine1.InterfaceInstance;

    [KeyAsName]
    private MachineDictionary<int, Sandbox.TestMachine> machines = default!;
    public MachineGroupInterface<int, Sandbox.TestMachine.Interface, Sandbox.TestMachine.State, Sandbox.TestMachine.Command> Machines => default!;
}
