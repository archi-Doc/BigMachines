// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[MachineObject]
public partial class IntermittentMachine : Machine<int>
{
    public static void Test(BigMachine bigMachine)
    {
        // The machine will run at regular intervals (1 second).
        var machine = bigMachine.IntermittentMachine.GetOrCreate(0);
    }

    public IntermittentMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
        this.Lifespan = TimeSpan.FromSeconds(5); // The time until the machine automatically terminates.
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"IntermittentMachine: Initial - {this.Count++}");
        if (this.Count > 2)
        {
            this.ChangeState(State.First);
        }

        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected StateResult First(StateParameter parameter)
    {
        Console.WriteLine($"IntermittentMachine: First - {this.Count++}");
        this.TimeUntilRun = TimeSpan.FromSeconds(0.5); // Change the timeout of the machine.
        return StateResult.Continue;
    }
}
