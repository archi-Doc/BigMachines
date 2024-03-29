﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Tinyhand;

#pragma warning disable SA1401
#pragma warning disable SA1602

namespace BigMachines;

// [MachineObject] // ulong id = FarmHash.Hash64(Type.FullName)
[TinyhandObject(UseServiceProvider = true, Structual = true)]
internal partial class SingleMachine : Machine
{
    public SingleMachine()
    {
    }

    public static async Task Test1()
    {
    }

    public enum State
    {
        Initial = 0,
        First = 1,
    }

    public override ManMachineInterface InterfaceInstance
    {
        get
        {
            if (this.__interfaceInstance__ is not Interface obj)
            {
                obj = new(this);
                this.__interfaceInstance__ = obj;
            }

            return obj;
        }
    }

    public class Interface : ManMachineInterface<State>
    {
        public Interface(SingleMachine machine)
            : base(machine)
        {
            this.Machine = machine;
        }

        public CommandList Command => new(this.Machine);

        protected internal new readonly SingleMachine Machine;

        public readonly struct CommandList
        {
            public CommandList(SingleMachine machine)
            {
                this.machine = machine;
            }

            public async Task<CommandResult> Command1()
            {
                await this.machine.Semaphore.EnterAsync().ConfigureAwait(false);
                try
                {
                    if (this.machine.__operationalState__ == OperationalFlag.Terminated)
                    {
                        return CommandResult.Terminated;
                    }

                    return this.machine.Command1();
                }
                finally
                {
                    this.machine.Semaphore.Exit();
                }
            }

            private readonly SingleMachine machine;
        }
    }

    protected StateResult Initial()
    {
        return StateResult.Continue;
        // this.SetState(State.Second);
    }

    protected async Task Second()
    {
        // this.SetState(State.Second);
    }

    protected CommandResult Command1()
    {
        return CommandResult.Success;
    }
}
