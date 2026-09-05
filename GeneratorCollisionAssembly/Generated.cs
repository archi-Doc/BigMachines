// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BigMachines.Tests")]

namespace BigMachines.Generator;

// Models the fixed-name helper emitted by older BigMachinesGenerator versions.
internal static class Generated
{
    internal static void RegisterBM()
    {
        throw new System.InvalidOperationException("The referenced legacy helper must not be called.");
    }
}
