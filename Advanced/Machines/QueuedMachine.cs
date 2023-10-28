// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using BigMachines;

namespace Advanced;

/*[MachineObject(0xe5cff489, Group = typeof(QueueGroup<>))] // Change groups from MachineGroup<> to QueueGroup<>.
public partial class QueuedMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        var queuedMachine = bigMachine.CreateOrGet<QueuedMachine.Interface>(0);
        queuedMachine.SetTimeout(TimeSpan.Zero);
        bigMachine.CreateOrGet<QueuedMachine.Interface>(2);
        bigMachine.CreateOrGet<QueuedMachine.Interface>(1);

        var group = queuedMachine.Group;
        Console.WriteLine(string.Join(",", group.GetIdentifiers().Select(a => a.ToString())));
    }

    public QueuedMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
        // this.DefaultTimeout = TimeSpan.FromSeconds(1);
        this.Timeout = 0;
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        Console.WriteLine($"Queued machine: ({this.Identifier.ToString()}) Start");
        await Task.Delay(1500).WithoutLock(this);
        Console.WriteLine($"Queued machine: ({this.Identifier.ToString()}) End");
        return StateResult.Terminate;
    }
}*/
