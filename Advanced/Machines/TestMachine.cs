// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

#pragma warning disable SA1602 // Enumeration items should be documented

public enum TestMachineKind : byte
{
    Alpha,
    Beta,
    Gamma,
}

[TinyhandObject] // Annotate TinyhandObject attribute to enable serialization (and set UseServiceProvider to true to skip default constructor check).
[MachineObject] // Annotate MachineObject and set Machine type id (unique number).
public partial class TestMachine : Machine<int> // Inherit Machine<TIdentifier> class.
{
    public static void Test(BigMachine bigMachine)
    {
        bigMachine.TestMachine.GetOrCreate(3);
        bigMachine.TestMachine.TryGet(3, out var testMachine); // Get the created machine.
        // bigMachine.TestMachine.CreateAlways(3);

        var testMachine2 = bigMachine.TestMachine.GetOrCreate(10);
        // testMachine2.GetOperationalState(OperationalFlag.Paused);

        // bigMachine.TestMachine
        // testGroup.CommandAsync(TestMachine.Command.PrintText, "group message").Wait();
        Console.WriteLine();
    }

    public TestMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine processing.
        this.Lifespan = TimeSpan.FromSeconds(10); // The time until the machine automatically terminates.
    }

    [Key(10)]
    public int Count { get; set; }

    [StateMethod(0)] // Annotate StateMethod attribute and set state method id (0 for default state).
    protected StateResult Initial(StateParameter parameter)
    {// This code is inside 'lock (this.Machine)' statement.
        Console.WriteLine($"TestMachine {this.Identifier}: Initial");

        this.ChangeState(TestMachine.State.One); // Change to state One (The method name becomes the state name).
        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected StateResult One(StateParameter parameter)
    {
        Console.WriteLine($"TestMachine {this.Identifier}: One - {this.Count}");

        this.ChangeState(State.Two); // Try to change to state Two.

        return StateResult.Continue;
    }

    protected bool TwoCanEnter()
    {// StateName + CanEnter() or CanExit() method controls the state transitions.
        var result = this.Count++ > 2;
        if (result)
        {
            Console.WriteLine("One -> Two approved.");
        }
        else
        {
            Console.WriteLine("One -> Two denied.");
        }

        return result;
    }

    [StateMethod(2)]
    protected StateResult Two(StateParameter parameter)
    {
        Console.WriteLine($"TestMachine {this.Identifier}: Two - {this.Count++}");
        this.TimeUntilRun = TimeSpan.FromSeconds(0.5); // Change the time until the next run.

        if (this.Count > 10)
        {// Terminate the machine.
            return StateResult.Terminate;
        }

        return StateResult.Continue; // Continue
    }

    [CommandMethod]
    protected CommandResult<int> GetCount(int n)
    {
        if (n == 0)
        {
            // this.BigMachine.TryGet<TerminatorMachine<int>.Interface>(0)?.CommandAndReceiveAsync(TerminatorMachine<int>.Command)<int, int>(0);
            return new(this.Count);
        }

        return new(0);
    }

    /*[CommandMethod]
    protected void RelayString(string st)
    {// LoopMachine -> TestMachine -> LoopMachine
        this.BigMachine.TryGet<LoopMachine.Interface>(0)?.CommandAsync(LoopMachine.Command.RelayString, st);
    }*/

    [CommandMethod]
    protected CommandResult PrintText(string text)
    {
        Console.WriteLine($"TestMachine {this.Identifier} PrintText : {text}");
        return CommandResult.Success;
    }

    protected override void OnTerminate()
    {
        Console.WriteLine($"TestMachine {this.Identifier}: Terminated");

        // ThreadCore.Root.Terminate(); // Send a termination signal to the root.
    }
}
