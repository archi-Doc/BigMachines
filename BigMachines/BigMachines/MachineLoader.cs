// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;

namespace BigMachines;

public static class MachineLoader
{
    public static void Add(Type loaderType)
    {
        if (loaderType.IsGenericTypeDefinition &&
            loaderType.GetInterfaces().Any(x => x.GetGenericTypeDefinition() == typeof(IMachineLoader<>)))
        {
            loaderTypes.TryAdd(loaderType, 0);
        }
    }

    public static void Load<TIdentifier>()
        where TIdentifier : notnull
    {
        var type = typeof(TIdentifier);
        if (!loadedTIdentifier.ContainsKey(type))
        {
            loadedTIdentifier.TryAdd(type, 0);

            foreach (var x in loaderTypes.Keys)
            {
                var loader = (IMachineLoader<TIdentifier>?)Activator.CreateInstance(x.MakeGenericType(type));
                if (loader != null)
                {
                    loader.Load();
                }
            }
        }
    }

    private static ConcurrentDictionary<Type, int> loaderTypes = new();
    private static ConcurrentDictionary<Type, int> loadedTIdentifier = new();
}
