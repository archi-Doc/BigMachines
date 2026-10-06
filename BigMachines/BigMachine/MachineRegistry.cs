// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Tinyhand;

namespace BigMachines;

/// <summary>
/// Stores generated machine metadata and creates registered machine instances.
/// </summary>
public static class MachineRegistry
{
    /*/// <summary>
    /// Gets or sets <see cref="IServiceProvider"/> used to create an instances of <see cref="Machine"/>.
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; set; }*/

    private static readonly ConcurrentDictionary<Type, MachineInformation> TypeToInformation = new();

    /// <summary>
    /// Registers metadata for a machine type. The first registration is retained.
    /// </summary>
    /// <param name="information">The generated machine metadata.</param>
    public static void Register(MachineInformation information)
    {
        TypeToInformation.TryAdd(information.MachineType, information);
    }

    /// <summary>
    /// Gets the registered metadata for a machine type.
    /// </summary>
    /// <typeparam name="TMachine">The registered machine type.</typeparam>
    /// <returns>The machine metadata.</returns>
    /// <exception cref="InvalidOperationException">The type has not been registered.</exception>
    public static MachineInformation GetInformation<TMachine>()
    {
        if (TypeToInformation.TryGetValue(typeof(TMachine), out var information))
        {
            return information;
        }
        else
        {
            throw new InvalidOperationException($"MachineInformation for type {typeof(TMachine).FullName} has not been registered.");
        }
    }

    /// <summary>
    /// Attempts to get metadata for a machine type without creating an instance.
    /// </summary>
    /// <typeparam name="TMachine">The machine type to look up.</typeparam>
    /// <param name="information">The metadata, or <see langword="null"/> if not registered.</param>
    /// <returns>Whether the type is registered.</returns>
    public static bool TryGetInformation<TMachine>([MaybeNullWhen(false)] out MachineInformation information)
    {
        return TypeToInformation.TryGetValue(typeof(TMachine), out information);
    }

    /// <summary>
    /// Creates a detached machine using its registered constructor or the Tinyhand service provider.
    /// </summary>
    /// <typeparam name="TMachine">The registered machine type.</typeparam>
    /// <returns>The new machine. A control must attach it before runtime use.</returns>
    public static TMachine CreateMachine<TMachine>()
        where TMachine : Machine
    {
        var information = GetInformation<TMachine>();
        return CreateMachine<TMachine>(information);
    }

    /// <summary>
    /// Creates a detached machine using the supplied metadata.
    /// </summary>
    /// <typeparam name="TMachine">The machine type to create.</typeparam>
    /// <param name="information">The metadata for <typeparamref name="TMachine"/>.</param>
    /// <returns>The new machine. A control must attach it before runtime use.</returns>
    /// <exception cref="InvalidOperationException">The service provider cannot resolve the machine.</exception>
    public static TMachine CreateMachine<TMachine>(MachineInformation information)
        where TMachine : Machine
    {
        TMachine? machine = default;
        if (information.Constructor is not null)
        {
            machine = (TMachine)information.Constructor();
        }
        else
        {
            machine = TinyhandSerializer.ServiceProvider.GetService(information.MachineType) as TMachine;
            if (machine is null)
            {
                throw new InvalidOperationException("Service provider was unable to create an instance of the machine.");
            }
        }

        return machine;
    }
}
