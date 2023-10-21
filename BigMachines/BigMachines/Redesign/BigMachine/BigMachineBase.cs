// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

namespace BigMachines;

public abstract class BigMachineBase
{
    /// <summary>
    /// Gets <see cref="IServiceProvider"/> used to create instances of <see cref="Machine{TIdentifier}"/>.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; }
}
