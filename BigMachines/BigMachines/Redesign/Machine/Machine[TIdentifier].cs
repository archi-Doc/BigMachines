// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;

#pragma warning disable SA1202

namespace BigMachines.Redesign;

/// <summary>
/// Represents an abstract class that serves as the base for the actual machine class.<br/>
/// <see cref="Machine{TIdentifier}"/> = <see cref="Machine"/>+<typeparamref name="TIdentifier"/>.
/// </summary>
/// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
[TinyhandObject(ReservedKeys = Machine.ReservedKeyNumber)]
public abstract partial class Machine<TIdentifier> : Machine
    where TIdentifier : notnull
{
    internal Machine()
        : base(default!)
    {
        this.Control = default!;
        this.Identifier = default!;
    }

    public Machine(MultiMachineControl<TIdentifier> control, TIdentifier identifier)
        : base(control)
    {
        this.Control = control;
        this.Identifier = identifier;
    }

    /// <summary>
    /// Gets an instance of <see cref="MultiMachineControl{TIdentifier}"/>.
    /// </summary>
    public new MultiMachineControl<TIdentifier> Control { get; }

    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    [Key(0, IgnoreKeyReservation = true)]
    public TIdentifier Identifier { get; protected set; }
}
