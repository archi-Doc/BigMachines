// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BigMachines;

public abstract class MachineControl<TIdentifier>
    where TIdentifier : notnull
{
    public MachineControl(BigMachineBase bigMachine, MachineInfo<TIdentifier> info)
    {
        this.BigMachine = bigMachine;
        this.Info = info;
    }

    #region FieldAndProperty

    public BigMachineBase BigMachine { get; private set; }

    public MachineInfo<TIdentifier> Info { get; private set; }

    #endregion

    #region Abstract

    public abstract Machine<TIdentifier> GetOrAddMachine(TIdentifier identifier, Machine<TIdentifier> machine);

    public abstract void AddMachine(TIdentifier identifier, Machine<TIdentifier> machine);

    public abstract bool TryGetMachine(TIdentifier identifier, [MaybeNullWhen(false)] out Machine<TIdentifier> machine);

    public abstract bool RemoveFromGroup(Machine<TIdentifier> machine);

    public abstract IEnumerable<TIdentifier> GetIdentifiers();

    public abstract IEnumerable<Machine<TIdentifier>> GetMachines();

    #endregion
}
