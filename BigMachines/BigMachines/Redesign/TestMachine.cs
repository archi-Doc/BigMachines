﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Tinyhand;

#pragma warning disable SA1401
#pragma warning disable SA1602

namespace BigMachines.Redesign;

// [MachineObject] // ulong id = FarmHash.Hash64(Type.FullName)
[TinyhandObject(UseServiceProvider = true, Structual = true)]
public partial class TestMachine : Machine<int>
{
    public TestMachine()
    {
    }

    public static async Task Test1()
    {
        MachineRegistry.Register(new(typeof(TestMachine), typeof(int), false, () => new TestMachine()));

        var bigMachine = new TestBigMachine();
        var machine = bigMachine.TestMachines.TryGet(0);
        if (machine is not null)
        {
            await machine.Command.Command1();
            await machine.RunAsync();
            machine.ChangeState(State.Initial);
            var id = machine.Identifier;
        }

        var testMachines = bigMachine.TestMachines;
        await testMachines.RunAllAsync();

        bigMachine.TestMachines.GetArray();
    }

    public enum State
    {
        Initial = 0,
        First = 1,
    }

    internal override object InterfaceInstance
    {
        get
        {
            if (this.interfaceInstance is not Interface obj)
            {
                obj = new(this);
                this.interfaceInstance = obj;
            }

            return obj;
        }
    }

    public class Interface : ManMachineInterface<int, State>
    {
        public Interface(TestMachine machine)
            : base(machine)
        {
        }

        public CommandList Command => new(this.Machine);

        private new TestMachine Machine => (TestMachine)((ManMachineInterface)this).Machine;

        public readonly struct CommandList
        {
            public CommandList(TestMachine machine)
            {
                this.machine = machine;
            }

            public async Task<CommandResult> Command1()
            {
                await this.machine.Semaphore.EnterAsync().ConfigureAwait(false);
                try
                {
                    if (this.machine.operationalState == OperationalFlag.Terminated)
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

            private readonly TestMachine machine;
        }

        /*public readonly struct CommandAll
        {
            public CommandAll(MultiMachineControl<int, Interface> control)
            {
                this.control = control;
            }

            private readonly MultiMachineControl<int, Interface> control;

            public async Task<IdentifierAndCommandResult<int>[]> Command1()
            {
                var machines = this.control.GetArray();
                var results = new IdentifierAndCommandResult<int>[machines.Length];
                for (var i = 0; i < machines.Length; i++)
                {
                    results[i] = new(machines[i].Machine.Identifier, await machines[i].Command.Command1().ConfigureAwait(false));
                }

                return results;
            }
        }*/
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
