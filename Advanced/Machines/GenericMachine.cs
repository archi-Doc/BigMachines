// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Advanced;

[MachineObject]
public partial class GenericMachine<TData> : Machine<int>
{
    public static void Test(BigMachine bigMachine)
    {
        // The machine will run at regular intervals (1 second).
        // var machine = bigMachine.GenericMachine.GetOrCreate(0);
    }

    public GenericMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
        this.Lifespan = TimeSpan.FromSeconds(5); // The time until the machine automatically terminates.
    }

    public int Count { get; set; }

    public TData Data { get; set; } = default!;

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"IntermittentMachine: Initial - {this.Count++}");
        return StateResult.Continue;
    }

    protected override void OnCreation(object? createParam)
    {
        this.Data = (TData)createParam!;
    }
}
