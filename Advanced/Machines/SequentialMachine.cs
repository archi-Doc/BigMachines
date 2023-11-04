// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[TinyhandObject]
[MachineObject(Control = MachineControlKind.Sequential, NumberOfTasks = 1)]
public partial class SequentialMachine : Machine<int>
{// SequentialMachine executes one at a time, in the order of their creation.
    public static void Test(BigMachine bigMachine)
    {
        bigMachine.SequentialMachine.TryCreate(1);
        bigMachine.SequentialMachine.TryCreate(2);
        bigMachine.SequentialMachine.TryCreate(3);
    }

    public SequentialMachine()
    {
        this.Lifespan = TimeSpan.FromSeconds(5);
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [Key(10)]
    public int Count { get; set; }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        Console.WriteLine($"SequentialMachine machine[{this.Identifier}]: {this.Count++}");

        await Task.Delay(500).ConfigureAwait(false); // Some heavy task

        if (this.Count >= 3)
        {
            return StateResult.Terminate;
        }

        return StateResult.Continue;
    }
}
