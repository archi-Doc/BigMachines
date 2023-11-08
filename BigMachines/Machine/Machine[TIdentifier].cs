// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using BigMachines.Control;
using Tinyhand;
using Tinyhand.IO;

#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1401

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
        this.__identifier__ = default!;
    }

    /// <summary>
    /// Gets an instance of <see cref="MultiMachineControl{TIdentifier}"/>.
    /// </summary>
    public override MultiMachineControl<TIdentifier>? MachineControl { get; } = default!;

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    [Key(0, IgnoreKeyReservation = true)]
    protected TIdentifier __identifier__;

    [IgnoreMember]
    protected internal TIdentifier Identifier
    {
        get => this.__identifier__;
        internal set => this.__identifier__ = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetIdentifier(TIdentifier identifier)
        => this.__identifier__ = identifier;
}
