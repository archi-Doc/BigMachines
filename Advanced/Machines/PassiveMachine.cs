// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;

namespace Advanced;

[MachineObject(0xffd829b4)]
public partial class PassiveMachine : Machine<int>
{
    public static async Task Test(BigMachine<int> bigMachine)
    {
        var m = bigMachine.CreateOrGet<PassiveMachine.Interface>(0);

        await m.CommandAsync(Command.ReceiveString, "message 1"); // Send a command.

        await m.RunAsync(); // Manually run the machine.

        var result = await m.ChangeStateAsync(State.First); // Change the state from State.Initial to State.First
        Console.WriteLine(result.ToString());
        await m.RunAsync(); // Manually run the machine.

        result = await m.ChangeStateAsync(State.Second); // Change the state from State.First to State.Second (denied)
        Console.WriteLine(result.ToString());
        await m.RunAsync(); // Manually run the machine.

        result = await m.ChangeStateAsync(State.Second); // Change the state from State.First to State.Second (approved)
        Console.WriteLine(result.ToString());
        await m.RunAsync(); // Manually run the machine.
    }

    public PassiveMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"PassiveMachine: Initial - {this.Count++}");
        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected StateResult First(StateParameter parameter)
    {
        Console.WriteLine($"PassiveMachine: First - {this.Count++}");
        return StateResult.Continue;
    }

    protected bool FirstCanExit()
    {// State Name + "CanExit": Determines if it is possible to change from the state.
        return true;
    }

    [StateMethod(2)]
    protected StateResult Second(StateParameter parameter)
    {
        Console.WriteLine($"PassiveMachine: Second - {this.Count++}");
        return StateResult.Continue;
    }

    protected bool SecondCanEnter()
    {// State Name + "CanEnter": Determines if it is possible to change to the state.
        var result = this.Count > 2;
        var message = result ? "Approved" : "Denied";
        Console.WriteLine($"PassiveMachine: {this.GetCurrentState().ToString()} -> {State.Second.ToString()}: {message}");
        return result;
    }

    [CommandMethod(0)]
    protected void ReceiveString(string message)
    {
        Console.WriteLine($"PassiveMachine command: {message}");
    }
}
