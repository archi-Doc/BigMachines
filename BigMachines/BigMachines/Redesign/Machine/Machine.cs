// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace BigMachines.Redesign;

public class Machine
{
    public Machine(MachineControl control)
    {
        this.Control = control;
    }

    public MachineControl Control { get; }

#pragma warning disable SA1401
    protected object? interfaceInstance;
#pragma warning restore SA1401
}

public class Machine<TIdentifier> : Machine
{
    public Machine(MachineControl<TIdentifier> control, TIdentifier identifier)
        : base(control)
    {
        this.Control = control;
        this.Identifier = identifier;
    }

    public new MachineControl<TIdentifier> Control { get; }

    public TIdentifier Identifier { get; }
}
