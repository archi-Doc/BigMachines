// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[MachineObject]
public partial class DerivedMachine : IntermittentMachine
{
    public static void Test2(BigMachine bigMachine)
    {
        var machine = bigMachine.DerivedMachine.GetOrCreate(0);
    }

    public DerivedMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
        this.Lifespan = TimeSpan.FromSeconds(5); // The time until the machine automatically terminates.
    }

    // [StateMethod(0)]
    protected new StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"DerivedMachine: Initial - {this.Count++}");
        if (this.Count > 2)
        {
            this.ChangeState(State.First);
        }

        return StateResult.Continue;
    }
}

public class EmptyMachineBase : Machine<int>
{
    public EmptyMachineBase()
    {
    }

    public int Count { get; set; }

    public string Text { get; set; } = "EmptyMachine";
}

[MachineObject]
public partial class DerivedMachine2 : EmptyMachineBase
{
    public static void Test(BigMachine bigMachine)
    {
        var m = bigMachine.DerivedMachine2.GetOrCreate(0);
    }

    public DerivedMachine2()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
        this.Lifespan = TimeSpan.FromSeconds(3); // The time until the machine automatically terminates.
    }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"{this.Text} - DerivedMachine2: Initial - {this.Count++}");

        return StateResult.Continue;
    }
}
