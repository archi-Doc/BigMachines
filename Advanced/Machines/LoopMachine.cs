// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;

namespace Advanced;

// Loop Machine
[MachineObject(0xb7196ebc)]
public partial class LoopMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        bigMachine.EnableLoopChecker = true;
        var loopMachine = bigMachine.CreateOrGet<LoopMachine.Interface>(0);

        // Case 1: LoopMachine -> LoopMachine
        loopMachine.CommandAsync(Command.RelayInt, 1);

        // Case 2: LoopMachine -> TestMachine -> LoopMachine
        /*bigMachine.CreateOrGet<TestMachine.Interface>(3);
        loopMachine.CommandAsync(Command.RelayString, "loop");*/
    }

    public LoopMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
    }

    [CommandMethod(0)]
    protected void RelayInt(CommandPost<int>.Command command)
    {
        if (command.Message is int n)
        {// LoopMachine
            this.BigMachine.TryGet<Interface>(this.Identifier)?.CommandAsync(Command.RelayInt, n);
        }
    }

    [CommandMethod(1)]
    protected void RelayString(CommandPost<int>.Command command)
    {
        if (command.Message is string st)
        {// LoopMachine -> TestMachine
            this.BigMachine.TryGet<TestMachine.Interface>(3)?.CommandAsync(TestMachine.Command.RelayString, st);
        }
    }
}
