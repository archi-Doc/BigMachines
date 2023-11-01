// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[MachineObject]
public partial class RecursiveMachine : Machine<int>
{
    public static async Task Test(BigMachine bigMachine)
    {
        var bm = (IBigMachine)bigMachine;
        bm.RecursiveDetectionMode = RecursiveDetectionMode.EnabledAndThrowException;

        var machine1 = bigMachine.RecursiveMachine.GetOrCreate(1);
        var machine2 = bigMachine.RecursiveMachine.GetOrCreate(2);

        // Case 1: Machine1 -> LoopMachine
        await machine1.Command.RelayInt(2);

        // Case 2: LoopMachine -> TestMachine -> LoopMachine
        // bigMachine.CreateOrGet<TestMachine.Interface>(3);
        // loopMachine.CommandAsync(Command.RelayString, "loop");

        // Case 3: LoopMachine -> LoopMachine2
        // loopMachine.CommandAsync(Command.RelayInt2, 2);
    }

    public RecursiveMachine()
    {
    }

    [CommandMethod]
    protected CommandResult RelayInt(int n)
    {// LoopMachine
        Console.WriteLine($"RelayInt: {n}");
        // this.BigMachine.(this.Identifier)?.CommandAsync(Command.RelayInt, n);
        return CommandResult.Success;
    }

    [CommandMethod]
    protected CommandResult RelayString(string st)
    {// LoopMachine -> TestMachine
        Console.WriteLine($"RelayString: {st}");
        // this.BigMachine.TryGet<TestMachine.Interface>(3)?.CommandAsync(TestMachine.Command.RelayString, st);
        return CommandResult.Success;
    }

    [CommandMethod]
    protected CommandResult RelayInt2(int n)
    {// LoopMachine -> LoopMachine n
        if (this.Identifier == 0)
        {
            Console.WriteLine($"RelayInt2: {n}");
            // this.BigMachine.TryGet<Interface>(n)?.CommandAsync(Command.RelayInt2, n);
        }
        else
        {
            n = 0;
            Console.WriteLine($"RelayInt2: {n}");
            // this.BigMachine.TryGet<Interface>(n)?.CommandAsync(Command.RelayInt2, n);
        }

        return CommandResult.Success;
    }
}
