// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;

namespace BigMachines.Redesign;

public static class MachineRegistry
{
    /*/// <summary>
    /// Gets or sets <see cref="IServiceProvider"/> used to create an instances of <see cref="Machine"/>.
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; set; }*/

    public static TMachine CreateMachine<TMachine>()
        where TMachine : Machine
    {
        var serviceProvider = TinyhandSerializer.ServiceProvider;
        MachineInformation information = default!;
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
