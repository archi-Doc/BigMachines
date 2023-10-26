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
    {
        this.Identifier = default!;
    }

    /// <summary>
    /// Gets an instance of <see cref="MultiMachineControl{TIdentifier}"/>.
    /// </summary>
    public new MultiMachineControl<TIdentifier>? Control { get; private set; }

    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    [Key(0, IgnoreKeyReservation = true)]
    public TIdentifier Identifier { get; protected set; }
}
