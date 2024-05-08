// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[MachineObject]
public partial class SingleMachine : Machine
{// A machine without an identifier is derived from the Machine class.
    public static void Test(BigMachine bigMachine)
    {
        bigMachine.SingleMachine.GetOrCreate(); // Only one machine is created.
    }

    public SingleMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public int Count { get; set; }

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
