// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace BigMachines;

internal class MachineRunner
{
    private readonly List<Machine> list = new();
    private readonly List<Machine> lifespanList = new();
    private DateTime utcNow;
    private TimeSpan elapsed;

    public void Prepare(DateTime utcNow, TimeSpan elapsed)
    {
        this.utcNow = utcNow;
        this.elapsed = elapsed;
    }

    public void Add(Machine machine)
    {
        this.list.Add(machine);
    }

    public void AddLifespan(Machine machine)
    {
        this.lifespanList.Add(machine);
    }

    public void RunAndClear()
    {
        foreach (var x in this.list)
        {
            x.Process(this.utcNow, this.elapsed);
        }

        foreach (var x in this.lifespanList)
        {
            x.ProcessLifespan(this.utcNow, this.elapsed);
        }

        this.list.Clear();
        this.lifespanList.Clear();
    }
}
