// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading.Tasks;

namespace BigMachines.Redesign;

public abstract class MachineControl
{
    public MachineControl(BigMachineBase bigMachine)
    {
        this.BigMachine = bigMachine;
    }

    public BigMachineBase BigMachine { get; }

    internal abstract bool RemoveMachine(Machine machine);
}

public abstract class MachineControl<TIdentifier> : MachineControl
    where TIdentifier : notnull
{
    public MachineControl(BigMachineBase bigMachine)
        : base(bigMachine)
    {
    }

    public abstract TIdentifier[] GetIdentifiers();
}

public abstract class MachineControl<TIdentifier, TInterface> : MachineControl<TIdentifier>
    where TIdentifier : notnull
    where TInterface : Machine.ManMachineInterface
{
    public MachineControl(BigMachineBase bigMachine)
        : base(bigMachine)
    {
    }

    public abstract TInterface[] GetMachines();

    public async Task RunAllAsync()
    {
        var machines = this.GetMachines();
        foreach (var x in machines)
        {
            await x.RunAsync().ConfigureAwait(false);
        }
    }
}
