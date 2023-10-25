// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading.Tasks;

namespace BigMachines.Redesign;

/// <summary>
/// Represents the abstract class for managing machines.
/// </summary>
public abstract class MachineControl
{
    public MachineControl(BigMachineBase bigMachine)
    {
        this.BigMachine = bigMachine;
    }

    /// <summary>
    /// Gets a BigMachine instance.
    /// </summary>
    public BigMachineBase BigMachine { get; }

    internal abstract bool RemoveMachine(Machine machine);
}

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

/// <summary>
/// Represents the abstract class for managing machines.<br/>
/// <see cref="MachineControl{TIdentifier, TInterface}"/> = <see cref="MachineControl{TIdentifier}"/>+<typeparamref name="TInterface"/>.
/// </summary>
/// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
/// <typeparam name="TInterface">The type of a machine interface.</typeparam>
public abstract class MachineControl<TIdentifier, TInterface> : MachineControl<TIdentifier>
    where TIdentifier : notnull
    where TInterface : Machine.ManMachineInterface
{
    public MachineControl(BigMachineBase bigMachine)
        : base(bigMachine)
    {
    }

    /// <summary>
    /// Gets an array of machine interfaces.
    /// </summary>
    /// <returns>An array of machine interfaces.</returns>
    public abstract TInterface[] GetArray();

    /// <summary>
    /// Runs all the machines managed by the control class.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RunAllAsync()
    {
        var machines = this.GetArray();
        foreach (var x in machines)
        {
            await x.RunAsync().ConfigureAwait(false);
        }
    }
}
