// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading.Tasks;

namespace BigMachines.Redesign;

/// <summary>
/// Represents the abstract class for managing machines.<br/>
/// <see cref="MachineControl{TIdentifier}"/> = <see cref="MachineControl"/>+<typeparamref name="TIdentifier"/>.
/// </summary>
/// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
public abstract class MachineControl<TIdentifier> : MachineControl
    where TIdentifier : notnull
{
    public MachineControl(BigMachineBase bigMachine)
        : base(bigMachine)
    {
    }

    /// <summary>
    /// Gets an array of machine identifiers.
    /// </summary>
    /// <returns>An array of machine identifiers.</returns>
    public abstract TIdentifier[] GetIdentifiers();
}
