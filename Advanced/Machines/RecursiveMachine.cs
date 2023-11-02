// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[MachineObject]
public partial class RecursiveMachine : Machine<int>
{
    public static async Task Test(BigMachine bigMachine)
    {
        var bm = (IBigMachine)bigMachine;
        bm.RecursiveDetectionMode = RecursiveDetectionMode.EnabledAndThrowException;

        var machine1 = bigMachine.RecursiveMachine.GetOrCreate(1); // Lock(Control)
        var machine2 = bigMachine.RecursiveMachine.GetOrCreate(2);

        // Case 1: Machine1 -> Machine1
        // await machine1.Command.RelayInt(1);
        await machine1.Command.RelayInt2(1);

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
    {// LoopMachine: Lock(Machine) -> Lock(Control)
        Console.WriteLine($"RelayInt: {n}");
        CommandResult result;
        if (((BigMachine)this.BigMachine).RecursiveMachine.TryGet(this.Identifier) is { } machine)
        {
            result = machine.Command.RelayInt(n).Result;
        }
        else
        {
            result = CommandResult.Failure;
        }

        return result;
    }

    [CommandMethod]
    protected CommandResult RelayInt2(int n)
    {// LoopMachine: Lock(Machine) -> Lock(Control)
        Console.WriteLine($"RelayInt2: {n}");
        var result = this.InterfaceInstance.Command.RelayInt(n).Result;

        return result;
    }
}
