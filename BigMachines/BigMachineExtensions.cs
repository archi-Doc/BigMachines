// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BigMachines;

public static class BigMachineTaskExtensions
{
    public static async Task WithoutLock<TIdentifier>(this Task task, Machine<TIdentifier> machine)
        where TIdentifier : notnull
    {
        // Monitor.Enter(machine.SyncMachine);
        var semaphore = new System.Threading.SemaphoreSlim(0, 1);
        try
        {
            Console.WriteLine("a");
            semaphore.Release();
            await task;
            Console.WriteLine("b");

        }
        finally
        {
            Console.WriteLine("c");

            // Monitor.Exit(machine.SyncMachine);
            semaphore.Wait();
        }
    }
}
