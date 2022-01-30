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
        if (machine.SyncObjectOrSemaphore is SemaphoreSlim semaphore)
        {// Semaphore does not have thread affinity.
            try
            {
                semaphore.Release();
                await task.ConfigureAwait(false);
            }
            finally
            {
                semaphore.Wait();
            }
        }
        else
        {// lock() is not supported.
            await task.ConfigureAwait(false);
        }
    }
}
