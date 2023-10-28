// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace BigMachines;

/// <summary>
/// A base interface for <see cref="IMachineLoader{T}"/> so that all generic implementations can be detected by a common base type.
/// </summary>
public interface IMachineLoader
{
}

/// <summary>
/// The contract for loading machine of some specific identifier type.
/// </summary>
/// <typeparam name="TIdentifier">The type of identifier.</typeparam>
public interface IMachineLoader<TIdentifier> : IMachineLoader
    where TIdentifier : notnull
{
    void Load();
}
