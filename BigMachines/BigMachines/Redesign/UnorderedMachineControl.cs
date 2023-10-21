// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tinyhand;
using ValueLink;

namespace BigMachines;

public sealed class UnorderedMachineControl<TIdentifier, TMachine, TInterface, TState, TCommand>
    where TIdentifier : notnull
    where TMachine : ITinyhandSerialize<TMachine>
    where TInterface : ManMachineInterface<TIdentifier, TState, TCommand>
    where TState : struct
    where TCommand : struct
{
    public UnorderedMachineControl(BigMachineBase bigMachine)
    {
        this.bigMachine = bigMachine;
        this.Info = default!; // Must call Assign()
    }

    #region Item

    [TinyhandObject(Tree = true)]
    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private partial record Item
    {
        public Item()
        {
            this.Identifier = default!;
            this.Machine = default!;
        }

        public Item(TIdentifier identifier, TMachine machine)
        {
            this.Identifier = identifier;
            this.Machine = machine;
        }

#pragma warning disable SA1401 // Fields should be private

        [Key(0)]
        [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
        public TIdentifier Identifier;

        [Key(1)]
        public TMachine Machine;

#pragma warning restore SA1401 // Fields should be private
    }

    #endregion

    #region Interface

    public TInterface? TryGet(TIdentifier identifier)
    {
        lock (this.items.SyncObject)
        {
            if (this.items.IdentifierChain.FindFirst(identifier) is { } machine)
            {
                return machine.InterfaceInstance as TInterface;
            }
        }

        return null;
    }

    public Task CommandAsync<TMessage>(TCommand command, TMessage message)
    {
        return this.bigMachine.CommandPost.SendGroupAsync(this, CommandPost<TIdentifier>.CommandType.Command, this.IdentificationToMachine.Keys, Unsafe.As<TCommand, int>(ref command), message);
    }

    public Task<KeyValuePair<TIdentifier, TResponse?>[]> CommandAndReceiveAsync<TCommand, TMessage, TResponse>(TCommand command, TMessage message)
        where TCommand : struct
    {
        return this.bigMachine.CommandPost.SendAndReceiveGroupAsync<TMessage, TResponse>(this, CommandPost<TIdentifier>.CommandType.Command, this.IdentificationToMachine.Keys, Unsafe.As<TCommand, int>(ref command), message);
    }

    #endregion

    #region FieldAndProperty

    private readonly BigMachineBase bigMachine;
    private Item.GoshujinClass items = new();

    public MachineInfo<TIdentifier> Info { get; private set; }

    public int Count => this.IdentificationToMachine.Count;

    #endregion

    public IEnumerable<TIdentifier> GetIdentifiers() => this.IdentificationToMachine.Keys;

    void IMachineGroup<TIdentifier>.Assign(MachineInfo<TIdentifier> info)
    {
        this.Info = info;
    }

    Machine<TIdentifier> IMachineGroup<TIdentifier>.GetOrAddMachine(TIdentifier identifier, Machine<TIdentifier> machine) => this.IdentificationToMachine.GetOrAdd(identifier, machine);

    void IMachineGroup<TIdentifier>.AddMachine(TIdentifier identifier, Machine<TIdentifier> machine)
    {
        Machine<TIdentifier>? machineToRemove = null;
        this.IdentificationToMachine.AddOrUpdate(identifier, x => machine, (i, m) =>
        {
            machineToRemove = m;
            return machine;
        });

        if (machineToRemove != null)
        {
            machineToRemove.TaskRunAndTerminate();
        }
    }

    bool IMachineGroup<TIdentifier>.TryGetMachine(TIdentifier identifier, [MaybeNullWhen(false)] out Machine<TIdentifier> machine) => this.IdentificationToMachine.TryGetValue(identifier, out machine);

    bool IMachineGroup<TIdentifier>.RemoveFromGroup(Machine<TIdentifier> machine)
    {
        if (this.IdentificationToMachine.TryRemove(new(machine.Identifier, machine)))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    IEnumerable<Machine<TIdentifier>> IMachineGroup<TIdentifier>.GetMachines() => this.IdentificationToMachine.Values;

}
