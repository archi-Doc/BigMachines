// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using BigMachines.Control;
using Tinyhand;
using Tinyhand.IO;

namespace BigMachines;

/// <summary>
/// Represents an abstract class that serves as the base for the actual machine class.<br/>
/// <see cref="Machine{TIdentifier}"/> = <see cref="Machine"/>+<typeparamref name="TIdentifier"/>.
/// </summary>
/// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
[TinyhandObject(ReservedKeys = Machine.ReservedKeyNumber)]
public abstract partial class Machine<TIdentifier> : Machine
    where TIdentifier : notnull
{
    public Machine()
    {
        this.Identifier = default!;
    }

    /// <summary>
    /// Gets an instance of <see cref="MultiMachineControl{TIdentifier}"/>.
    /// </summary>
    public override MultiMachineControl<TIdentifier> Control { get; } = default!;

    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    [Key(0, IgnoreKeyReservation = true)]
    public TIdentifier Identifier { get; internal protected set; }
}
