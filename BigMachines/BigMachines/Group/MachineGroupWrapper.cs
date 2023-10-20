// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BigMachines;

public class MachineGroupWrapper<TIdentifier, TState, TCommand> : IMachineGroup<TIdentifier, TState, TCommand>
    where TIdentifier : notnull
    where TState : struct
    where TCommand : struct
{
    public MachineGroupWrapper(IMachineGroup<TIdentifier> group)
    {
        this.Group = group;
    }

    public IEnumerable<TIdentifier> GetIdentifiers() => this.Group.GetIdentifiers();

    public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
        where TMachineInterface : ManMachineInterface<TIdentifier>
        => this.Group.TryGet<TMachineInterface>(identifier);

    public IMachineGroup<TIdentifier> Group { get; }

    public BigMachine<TIdentifier> BigMachine => this.Group.BigMachine;

    public MachineInfo<TIdentifier> Info => this.Group.Info;

    public int Count => this.Group.Count;

    public Task<KeyValuePair<TIdentifier, TResponse?>[]> CommandAndReceiveAsync<TMessage, TResponse>(TCommand command, TMessage message)
    {
        return this.BigMachine.CommandPost.SendAndReceiveGroupAsync<TMessage, TResponse>(this.Group, CommandPost<TIdentifier>.CommandType.Command, this.Group.GetIdentifiers(), Unsafe.As<TCommand, int>(ref command), message);
    }

    public Task CommandAsync<TMessage>(TCommand command, TMessage message)
    {
        return this.BigMachine.CommandPost.SendGroupAsync<TMessage>(this.Group, CommandPost<TIdentifier>.CommandType.Command, this.Group.GetIdentifiers(), Unsafe.As<TCommand, int>(ref command), message);
    }

    void IMachineGroup<TIdentifier>.AddMachine(TIdentifier identifier, Machine<TIdentifier> machine) => this.Group.AddMachine(identifier, machine);

    void IMachineGroup<TIdentifier>.Assign(MachineInfo<TIdentifier> info) => this.Group.Assign(info);

    IEnumerable<Machine<TIdentifier>> IMachineGroup<TIdentifier>.GetMachines() => this.Group.GetMachines();

    Machine<TIdentifier> IMachineGroup<TIdentifier>.GetOrAddMachine(TIdentifier identifier, Machine<TIdentifier> machine) => this.Group.GetOrAddMachine(identifier, machine);

    bool IMachineGroup<TIdentifier>.RemoveFromGroup(Machine<TIdentifier> machine) => this.Group.RemoveFromGroup(machine);

    bool IMachineGroup<TIdentifier>.TryGetMachine(TIdentifier identifier, [MaybeNullWhen(false)] out Machine<TIdentifier> machine) => this.Group.TryGetMachine(identifier, out machine);
}
