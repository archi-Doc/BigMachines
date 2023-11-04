// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using BigMachines;

namespace Advanced;

/*[MachineObject(0xb579a7d8, Continuous = true)] // Set the Continuous property to true.
public partial class ContinuousMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        bigMachine.Continuous.SetMaxThreads(2); // Set the maximum number of threads used for continuous machines.
        var m = bigMachine.CreateOrGet<ContinuousMachine.Interface>(0);

        // The machine will run until the task is complete.
    }

    public ContinuousMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        Console.WriteLine($"ContinuousMachine: Initial - {this.Count++}");
        if (this.Count > 10)
        {
            Console.WriteLine($"ContinuousMachine: Done");
            return StateResult.Terminate;
        }

        await Task.Delay(100); // Some heavy task
        // await Task.Delay(100).WithoutLock(this); // You can also release the lock temporarily to improve the response (the machine state may change in the meantime).

        return StateResult.Continue;
    }
}*/
