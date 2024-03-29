﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using BigMachines.Control;
using Tinyhand;

#pragma warning disable SA1401
#pragma warning disable SA1602

namespace BigMachines;

internal static class TestMachineExtension
{
    public static async Task<IdentifierAndCommandResult<int>[]> AllCommand1(this MultiMachineControl<int, TestMachine.Interface> control)
    {
        var machines = control.GetArray();
        var results = new IdentifierAndCommandResult<int>[machines.Length];
        for (var i = 0; i < machines.Length; i++)
        {
            results[i] = new(machines[i].Identifier, await machines[i].Command.Command1().ConfigureAwait(false));
        }

        return results;
    }
}

// [MachineObject] // ulong id = FarmHash.Hash64(Type.FullName)
[TinyhandObject(UseServiceProvider = true, Structual = true)]
internal partial class TestMachine : Machine<int>
{
    public TestMachine()
    {
    }

    public static async Task Test1()
    {
        // MachineRegistry.Register(new(0, typeof(TestMachine), typeof(UnorderedMachineControl<,,>)) { Constructor = () => new TestMachine(), IdentifierType = typeof(int), });

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
        await testMachines.AllRunAsync();

        bigMachine.TestMachines.GetArray();
    }

    public enum State
    {
        Initial = 0,
        First = 1,
    }

    /*public override ManMachineInterface InterfaceInstance
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
    }*/

    public override UnorderedMachineControl<int, TestMachine, Interface> MachineControl => (UnorderedMachineControl<int, TestMachine, Interface>)this.__machineControl__;

    public class Interface : ManMachineInterface<int, State>
    {// int: this.IdentifierObject!.FullName, State
        public Interface(TestMachine machine)
            : base(machine)
        {
        }

        public CommandList Command => new((TestMachine)this.Machine);

        // private new TestMachine Machine => (TestMachine)((ManMachineInterface)this).Machine;

        public readonly struct CommandList
        {
            public CommandList(TestMachine machine)
            {
                this.machine = machine;
            }

            private readonly TestMachine machine;

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

            public async Task<CommandResult<int>> Command2()
            {
                await this.machine.Semaphore.EnterAsync().ConfigureAwait(false);
                try
                {
                    if (this.machine.__operationalState__ == OperationalFlag.Terminated)
                    {
                        return new CommandResult<int>(CommandResult.Terminated, default);
                    }

                    return await this.machine.Command2().ConfigureAwait(false);
                }
                finally
                {
                    this.machine.Semaphore.Exit();
                }
            }

            public async Task<CommandResult> Command3()
            {
                if (this.machine.__operationalState__ == OperationalFlag.Terminated)
                {
                    return CommandResult.Terminated;
                }

                return this.machine.Command3();
            }
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

    protected async Task<CommandResult<int>> Command2()
    {
        return new(CommandResult.Success, 1);
    }

    protected CommandResult Command3()
    {
        return CommandResult.Success;
    }
}
