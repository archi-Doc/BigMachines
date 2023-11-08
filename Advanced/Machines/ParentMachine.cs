// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[MachineObject(Internal = true)]
public partial class ChildMachine : Machine
{// A machine without an identifier is derived from the Machine class.
    public ChildMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"Child machine: {this.Count++}");

        if (this.Count >= 5)
        {
            return StateResult.Terminate;
        }

        return StateResult.Continue;
    }
}

[MachineObject]
public partial class ParentMachine : Machine
{// A machine without an identifier is derived from the Machine class.
    public static void Test(BigMachine bigMachine)
    {
        bigMachine.ParentMachine.Get(); // Only one machine is created.
    }

    public ParentMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    protected override void OnPreparation()
    {
        var machine = this.BigMachine.ManualControl.GetOrCreate<ChildMachine>();
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"Parent machine: {this.Count++}");

        if (this.Count >= 2)
        {
            return StateResult.Terminate;
        }

        return StateResult.Continue;
    }
}
