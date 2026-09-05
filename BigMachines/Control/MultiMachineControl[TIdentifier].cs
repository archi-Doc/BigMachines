// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace BigMachines.Control;

/// <summary>
/// Defines a machine control whose machines are addressed by identifiers.
/// </summary>
/// <typeparam name="TIdentifier">The machine identifier type.</typeparam>
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
