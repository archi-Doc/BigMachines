// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Tinyhand;
using Tinyhand.IO;

namespace BigMachines;

/// <summary>
/// Represents an abstract class that serves as the base for the actual machine class.<br/>
/// <see cref="Machine{TIdentifier}"/> = <see cref="Machine"/>+<typeparamref name="TIdentifier"/>.
/// </summary>
/// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
[TinyhandObject(ReservedKeys = Machine.ReservedKeyNumber)]
public abstract partial class Machine<TIdentifier> : Machine // , ITinyhandSerialize<Machine<TIdentifier>>
    where TIdentifier : notnull
{
    public Machine()
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
    public TIdentifier Identifier { get; internal protected set; }

    /*static void ITinyhandSerialize<Machine<TIdentifier>>.Deserialize(ref TinyhandReader reader, scoped ref Machine<TIdentifier>? value, TinyhandSerializerOptions options)
    {
        value ??= MachineRegistry.CreateMachine<Machine<TIdentifier>>();
        if (value is ITinyhandSerialize obj)
        {
            obj.Deserialize(ref reader, options);
        }
    }

    static void ITinyhandSerialize<Machine<TIdentifier>>.Serialize(ref TinyhandWriter writer, scoped ref Machine<TIdentifier>? value, TinyhandSerializerOptions options)
    {
        if (value is ITinyhandSerialize obj)
        {
            obj.Serialize(ref writer, options);
        }
        else
        {
            writer.WriteNil();
        }
    }*/
}
