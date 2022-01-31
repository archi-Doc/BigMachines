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
        bigMachine.LoopCheckerMode = LoopCheckerMode.EnabledAndThrowException;
        var loopMachine = bigMachine.CreateOrGet<LoopMachine.Interface>(0);
        var loopMachine2 = bigMachine.CreateOrGet<LoopMachine.Interface>(2);

        // Case 1: LoopMachine -> LoopMachine
        loopMachine.CommandAsync(Command.RelayInt, 1);

        // Case 2: LoopMachine -> TestMachine -> LoopMachine
        // bigMachine.CreateOrGet<TestMachine.Interface>(3);
        // loopMachine.CommandAsync(Command.RelayString, "loop");

        // Case 3: LoopMachine -> LoopMachine2
        // loopMachine.CommandAsync(Command.RelayInt2, 2);
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
            Console.WriteLine($"RelayInt: {n}");
            this.BigMachine.TryGet<Interface>(this.Identifier)?.CommandAsync(Command.RelayInt, n);
        }
    }

    [CommandMethod(1)]
    protected void RelayString(CommandPost<int>.Command command)
    {
        if (command.Message is string st)
        {// LoopMachine -> TestMachine
            Console.WriteLine($"RelayString: {st}");
            this.BigMachine.TryGet<TestMachine.Interface>(3)?.CommandAsync(TestMachine.Command.RelayString, st);
        }
    }

    [CommandMethod(2)]
    protected void RelayInt2(CommandPost<int>.Command command)
    {
        if (command.Message is int n)
        {// LoopMachine -> LoopMachine n
            if (this.Identifier == 0)
            {
                Console.WriteLine($"RelayInt2: {n}");
                this.BigMachine.TryGet<Interface>(n)?.CommandAsync(Command.RelayInt2, n);
            }
            else
            {
                n = 0;
                Console.WriteLine($"RelayInt2: {n}");
                this.BigMachine.TryGet<Interface>(n)?.CommandAsync(Command.RelayInt2, n);
            }
        }
    }
}
