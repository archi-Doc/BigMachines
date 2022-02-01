// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Arc.Collections;

#pragma warning disable SA1401

namespace BigMachines;

public class MachineQueued<TIdentifier> : IMachineGroup<TIdentifier>
    where TIdentifier : notnull
{
    private class Item
    {
        public Item(Machine<TIdentifier> machine)
        {
            this.Machine = machine;
        }

        internal void Clear()
        {
            this.Machine = null!;
            this.Node = null!;
        }

        internal Machine<TIdentifier> Machine;
        internal LinkedListNode<Item> Node = default!;
    }

    internal protected MachineQueued(BigMachine<TIdentifier> bigMachine)
    {
        this.BigMachine = bigMachine;
        this.Info = default!; // Must call Assign()
    }

    public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
        where TMachineInterface : ManMachineInterface<TIdentifier>
    {
        if (((IMachineGroup<TIdentifier>)this).TryGetMachine(identifier, out var machine))
        {
            return machine.InterfaceInstance as TMachineInterface;
        }

        return null;
    }

    public Task CommandAsync<TCommand, TMessage>(TCommand command, TMessage message)
        where TCommand : struct
    {
        return this.BigMachine.CommandPost.SendGroupAsync(this, CommandPost<TIdentifier>.CommandType.Command, this.GetKeys(), Unsafe.As<TCommand, int>(ref command), message);
    }

    public Task<KeyValuePair<TIdentifier, TResponse?>[]> CommandAndReceiveAsync<TCommand, TMessage, TResponse>(TCommand command, TMessage message)
        where TCommand : struct
    {
        return this.BigMachine.CommandPost.SendAndReceiveGroupAsync<TMessage, TResponse>(this, CommandPost<TIdentifier>.CommandType.Command, this.GetKeys(), Unsafe.As<TCommand, int>(ref command), message);
    }

    public BigMachine<TIdentifier> BigMachine { get; }

    public MachineInfo<TIdentifier> Info { get; private set; }

    public int Count
    {
        get
        {
            lock (this.identifierToItem)
            {
                return this.identifierToItem.Count;
            }
        }
    }

    public IEnumerable<TIdentifier> GetIdentifiers() => this.GetKeys();

    void IMachineGroup<TIdentifier>.Assign(MachineInfo<TIdentifier> info)
    {
        this.Info = info;
    }

    Machine<TIdentifier> IMachineGroup<TIdentifier>.GetOrAddMachine(TIdentifier identifier, Machine<TIdentifier> machine)
    {
        lock (this.identifierToItem)
        {
            if (this.identifierToItem.TryGetValue(identifier, out var item))
            {// Get
                return item.Machine;
            }
            else
            {// Add
                item = new Item(machine);
                item.Node = this.list.AddLast(item);
                this.identifierToItem.TryAdd(identifier, item);
                return machine;
            }
        }
    }

    void IMachineGroup<TIdentifier>.AddMachine(TIdentifier identifier, Machine<TIdentifier> machine)
    {
        Machine<TIdentifier>? machineToRemove = null;
        lock (this.identifierToItem)
        {
            if (this.identifierToItem.Remove(identifier, out var item))
            {// Remove
                machineToRemove = item.Machine;
                this.list.Remove(item.Node);
                item.Machine = machine;
            }
            else
            {
                item = new Item(machine);
            }

            item.Node = this.list.AddLast(item);
            this.identifierToItem.TryAdd(identifier, item);
        }

        if (machineToRemove != null)
        {
            machineToRemove.TaskRunAndTerminate();
        }
    }

    bool IMachineGroup<TIdentifier>.TryGetMachine(TIdentifier identifier, [MaybeNullWhen(false)] out Machine<TIdentifier> machine)
    {
        lock (this.identifierToItem)
        {
            if (this.identifierToItem.TryGetValue(identifier, out var item))
            {
                machine = item.Machine;
                return true;
            }
        }

        machine = default;
        return false;
    }

    bool IMachineGroup<TIdentifier>.RemoveFromGroup(TIdentifier identifier)
    {
        lock (this.identifierToItem)
        {
            if (this.identifierToItem.Remove(identifier, out var item))
            {// Remove
                this.list.Remove(item.Node);
                item.Clear();
                return true;
            }
        }

        return false;
    }

    IEnumerable<Machine<TIdentifier>> IMachineGroup<TIdentifier>.GetMachines()
    {
        lock (this.identifierToItem)
        {
            return this.list.Select(a => a.Machine).ToArray();
        }
    }

    private TIdentifier[] GetKeys()
    {
        lock (this.identifierToItem)
        {
            return this.identifierToItem.Keys.ToArray();
        }
    }

    private Dictionary<TIdentifier, Item> identifierToItem = new();
    private LinkedList<Item> list = new();
}
