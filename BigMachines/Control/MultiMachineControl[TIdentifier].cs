// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace BigMachines.Control;

/// <summary>
/// Represents the abstract class for managing multiple machines.<br/>
/// <see cref="MultiMachineControl{TIdentifier}"/> = <see cref="MachineControl"/>+<typeparamref name="TIdentifier"/>.
/// </summary>
/// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
public abstract class MultiMachineControl<TIdentifier> : MachineControl
    where TIdentifier : notnull
{
    public MultiMachineControl()
        : base()
    {
    }

    /// <summary>
    /// Gets an array of machine identifiers.
    /// </summary>
    /// <returns>An array of machine identifiers.</returns>
    public abstract TIdentifier[] GetIdentifiers();
}
