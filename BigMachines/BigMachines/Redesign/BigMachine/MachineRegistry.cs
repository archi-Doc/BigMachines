// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Tinyhand;

namespace BigMachines.Redesign;

public static class MachineRegistry
{
    /*/// <summary>
    /// Gets or sets <see cref="IServiceProvider"/> used to create an instances of <see cref="Machine"/>.
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; set; }*/

    private static ConcurrentDictionary<Type, MachineInformation> typeToInformation = new();

    public static void Register(MachineInformation information)
    {
        typeToInformation.TryAdd(information.MachineType, information);
    }

    public static MachineInformation Get<TMachine>()
    {
        if (typeToInformation.TryGetValue(typeof(TMachine), out var information))
        {
            return information;
        }
        else
        {
            throw new InvalidOperationException($"MachineInformation for type {typeof(TMachine).FullName} has not been registered.");
        }
    }

    public static bool TryGet<TMachine>([MaybeNullWhen(false)] out MachineInformation information)
    {
        return typeToInformation.TryGetValue(typeof(TMachine), out information);
    }

    public static TMachine CreateMachine<TMachine>(MachineInformation information)
        where TMachine : Machine
    {
        var serviceProvider = TinyhandSerializer.ServiceProvider;
        TMachine? machine = default;

        if (serviceProvider is not null)
        {
            machine = serviceProvider.GetService(information.MachineType) as TMachine;
        }

        if (machine is null)
        {
            if (information.Constructor is not null)
            {
                machine = (TMachine)information.Constructor();
            }
            else
            {
                if (serviceProvider is null)
                {
                    throw new InvalidOperationException("Specify a service provider to create an instance of a machine that does not have a default constructor.");
                }
                else
                {
                    throw new InvalidOperationException("Service provider was unable to create an instance of the machine.");
                }
            }
        }

        return machine;
    }
}
