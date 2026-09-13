// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading.Tasks;

namespace BigMachines.Control;

/// <summary>
/// Defines a typed control for machines addressed by identifiers.
/// </summary>
/// <typeparam name="TIdentifier">The machine identifier type.</typeparam>
/// <typeparam name="THandle">The generated machine handle type.</typeparam>
public abstract class MultiMachineControl<TIdentifier, THandle> : MultiMachineControl<TIdentifier>
    where TIdentifier : notnull
    where THandle : Machine.MachineHandle
{
    public MultiMachineControl()
        : base()
    {
    }

    /// <summary>
    /// Runs all the machines managed by the control class.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RunAllAsync()
    {
        var machines = this.GetHandles();
        foreach (var x in machines)
        {
            await x.RunAsync().ConfigureAwait(false);
        }
    }

    public override abstract THandle[] GetHandles();
}
