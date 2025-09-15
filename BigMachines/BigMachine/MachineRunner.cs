// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace BigMachines;

internal class MachineRunner
{
    private DateTime utcNow;
    private TimeSpan elapsed;
    private List<Machine> list = new();

    public MachineRunner()
    {
    }

    public void Prepare(DateTime utcNow, TimeSpan elapsed)
    {
        this.utcNow = utcNow;
        this.elapsed = elapsed;
    }

    public void Add(Machine machine)
    {
        this.list.Add(machine);
    }

    public void RunAndClear()
    {
        foreach (var x in this.list)
        {
            x.Process(this.utcNow, this.elapsed);
        }

        this.list.Clear();
    }
}
