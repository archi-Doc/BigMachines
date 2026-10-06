// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[MachineObject]
public partial class RecursiveMachine : Machine<int>
{
    public static async Task Test(BigMachine bigMachine)
    {
        var machine1 = bigMachine.RecursiveMachine.GetOrCreate(1); // Lock(Control)
        bigMachine.RecursiveMachine.GetOrCreate(2);

        // Case 1: Machine1 -> Machine1
        // await machine1.Command.RelayInt(1);
        await machine1.Command.RelayInt(1);

        // Relay to another machine; the target completes without re-entering its own command.
        await machine1.Command.RelayInt(2);

        // Case 2: LoopMachine -> TestMachine -> LoopMachine
        // bigMachine.CreateOrGet<TestMachine.Handle>(3);
        // loopMachine.CommandAsync(Command.RelayString, "loop");

        // Case 3: LoopMachine -> LoopMachine2
        // loopMachine.CommandAsync(Command.RelayInt2, 2);
    }

    public RecursiveMachine()
    {
    }

    [CommandMethod]
    protected async Task<CommandStatus> RelayInt(int n)
    {// LoopMachine: Lock(Machine) -> Lock(Control)
        Console.WriteLine($"RelayInt: {n}");
        if (n == this.Identifier)
        {
            return CommandStatus.Success;
        }

        if (((BigMachine)this.BigMachine).RecursiveMachine.TryGet(n, out var machine))
        {
            return await machine.Command.RelayInt(n).ConfigureAwait(false);
        }

        return CommandStatus.Failure;
    }

    [CommandMethod]
    protected CommandStatus RelayInt2(int n)
    {// LoopMachine: Lock(Machine) -> Lock(Control)
        Console.WriteLine($"RelayInt2: {n}");
        var result = CommandStatus.Success; // this.HandleInstance.Command.RelayInt(n).Result;

        return result;
    }
}
