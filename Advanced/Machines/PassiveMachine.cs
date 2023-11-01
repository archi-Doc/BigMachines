// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[MachineObject]
public partial class PassiveMachine : Machine<int>
{
    public static async Task Test(BigMachine bigMachine)
    {
        var machine = bigMachine.PassiveMachine.GetOrCreate(0);

        await machine.Command.ReceiveString("message 1"); // Send a command.

        await machine.RunAsync(); // Manually run the machine.

        var result = machine.ChangeState(State.First); // Change the state from State.Initial to State.First
        Console.WriteLine(result.ToString());
        await machine.RunAsync(); // Manually run the machine.

        result = machine.ChangeState(State.Second); // Change the state from State.First to State.Second (denied)
        Console.WriteLine(result.ToString());
        await machine.RunAsync(); // Manually run the machine.

        result = machine.ChangeState(State.Second); // Change the state from State.First to State.Second (approved)
        Console.WriteLine(result.ToString());
        await machine.RunAsync(); // Manually run the machine.
    }

    public PassiveMachine()
    {
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"PassiveMachine: Initial - {this.Count++}");
        return StateResult.Continue;
    }

    [StateMethod]
    protected StateResult First(StateParameter parameter)
    {
        Console.WriteLine($"PassiveMachine: First - {this.Count++}");
        return StateResult.Continue;
    }

    protected bool FirstCanExit()
    {// State Name + "CanExit": Determines if it is possible to change from the state.
        return true;
    }

    [StateMethod]
    protected StateResult Second(StateParameter parameter)
    {
        Console.WriteLine($"PassiveMachine: Second - {this.Count++}");
        return StateResult.Continue;
    }

    protected bool SecondCanEnter()
    {// State Name + "CanEnter": Determines if it is possible to change to the state.
        var result = this.Count > 2;
        var message = result ? "Approved" : "Denied";
        Console.WriteLine($"PassiveMachine: {this.GetState().ToString()} -> {State.Second.ToString()}: {message}");
        return result;
    }

    [CommandMethod]
    protected CommandResult ReceiveString(string message)
    {
        Console.WriteLine($"PassiveMachine command: {message}");
        return CommandResult.Success;
    }
}
