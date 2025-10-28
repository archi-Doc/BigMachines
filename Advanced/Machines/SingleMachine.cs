// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[MachineObject]
public partial class SingleMachine : Machine
{// A machine without an identifier is derived from the Machine class.
    public static void Test(BigMachine bigMachine)
    {
        bigMachine.SingleMachine.GetOrCreate("test"); // Only one machine is created.
        bigMachine.SingleMachine.CreateAlways("test2"); // Terminate the previous machine and create a new one.
        bigMachine.SingleMachine.CreateAlways("test3"); // Terminate the previous machine and create a new one.
    }

    public SingleMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public int Count { get; set; }

    protected override void OnCreate(object? createParam)
    {
        Console.WriteLine($"Single machine: Created('{createParam?.ToString()}')");
    }

    protected override void OnTerminate()
    {
        Console.WriteLine($"Single machine: Terminated");
    }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"Single machine: {this.Count++}");

        if (this.Count >= 5)
        {
            throw new Exception("Exception thrown by Single machine");
            // return StateResult.Terminate;
        }

        return StateResult.Continue;
    }
}
