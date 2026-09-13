// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Tinyhand;
using Tinyhand.IO;

namespace BigMachines.Control;

/// <summary>
/// Defines common operations for a generated machine control.
/// </summary>
public abstract class MachineControl : IStructuralObject
{
    IStructuralRoot? IStructuralObject.StructuralRoot { get; set; }

    IStructuralObject? IStructuralObject.StructuralParent { get; set; }

    int IStructuralObject.StructuralKey { get; set; } = -1;

    void IStructuralObject.SetupStructure(IStructuralObject? parent, int key)
    {
        ((IStructuralObject)this).SetParentAndKey(parent, key);
        this.RestoreStructure();
    }

    void IStructuralObject.WriteLocator(ref TinyhandWriter writer)
    {
        writer.WriteKeyRecord();
        writer.Write(((IStructuralObject)this).StructuralKey);
    }

    bool IStructuralObject.ProcessJournalRecord(ref TinyhandReader reader)
        => this is ITinyhandCustomJournal custom && custom.ReadCustomRecord(ref reader);

    /// <summary>
    /// Reattaches persisted children after the structural parent or contents change.
    /// </summary>
    protected virtual void RestoreStructure()
    {
    }

    public MachineControl()
    {
    }

    /// <summary>
    /// Gets or sets a <see cref="BigMachineBase"/> instance.
    /// </summary>
    [IgnoreMember]
    public BigMachineBase BigMachine { get; protected set; } = default!;

    /// <summary>
    /// Gets the number of machines.
    /// </summary>
    public abstract int Count { get; }

    /// <summary>
    /// Gets the metadata for the managed machine type.
    /// </summary>
    [IgnoreMember]
    public abstract MachineInformation MachineInformation { get; }

    /// <summary>
    /// Returns a snapshot of the current machine handles. The machines themselves remain shared.
    /// </summary>
    /// <returns>A snapshot of the current handles.</returns>
    public abstract Machine.MachineHandle[] GetHandles();

    /// <summary>
    /// Determines whether this control contains any active machines.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if at least one active machine is present; otherwise, <see langword="false"/>.
    /// </returns>
    public abstract bool ContainsActiveMachine();

    /// <summary>
    /// Retrieves all machines currently managed by this control.
    /// </summary>
    /// <returns>An array of <see cref="Machine"/> instances managed by this control.</returns>
    internal abstract Machine[] GetMachines();

    /// <summary>
    /// Removes the specified machine from this control.
    /// </summary>
    /// <param name="machine">The <see cref="Machine"/> instance to remove.</param>
    /// <returns><see langword="true"/> if the machine was successfully removed; otherwise, <see langword="false"/>.</returns>
    internal abstract bool RemoveMachine(Machine machine);

    /// <summary>
    /// Processes all machines managed by this control using the specified runner.
    /// </summary>
    /// <param name="runner">The <see cref="MachineRunner"/> instance used to execute machine processing.</param>
    internal abstract void Process(MachineRunner runner);

    internal virtual void OnMachineResumed()
    {
    }
}
