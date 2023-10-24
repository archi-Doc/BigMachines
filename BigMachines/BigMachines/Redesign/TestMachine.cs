// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

namespace BigMachines.Redesign;

// [MachineObject] // ulong id = FarmHash.Hash64(Type.FullName)
[TinyhandObject(UseServiceProvider = true, Tree = true)]
public partial class TestMachine : Machine<int>
{
    public TestMachine(MachineControl<int> control, int identifier)
        : base(control, identifier)
    {
    }

    public static async Task Test1()
    {
        var bigMachine = new TestBigMachine();
        var machine = bigMachine.TestMachines.TryGet(0);
        if (machine is not null)
        {
            await machine.Command.Command1();
            await machine.RunAsync();
        }

        var testMachines = bigMachine.TestMachines;
        var results = await testMachines.CommandAll.Command1();
        await testMachines.RunAllAsync();
    }

    public Interface InterfaceInstance
    {
        get
        {
            if (this.interfaceInstance is not Interface @interface)
            {
                @interface = new(this);
                this.interfaceInstance = @interface;
            }

            return @interface;
        }
    }

    public class Interface : ManMachineInterface
    {
        public Interface(TestMachine machine)
            : base(machine)
        {
            this.machine = machine;
        }

        public CommandList Command => new(this.machine);

        private readonly TestMachine machine;

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
                    if (this.machine.Status == MachineStatus.Terminated)
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

        public readonly struct CommandAll
        {
            public CommandAll(MachineControl<int, Interface> control)
            {
                this.control = control;
            }

            private readonly MachineControl<int, Interface> control;

            public async Task<IdentifierAndCommandResult<int>[]> Command1()
            {
                var machines = this.control.GetMachines();
                var results = new IdentifierAndCommandResult<int>[machines.Length];
                for (var i = 0; i < machines.Length; i++)
                {
                    results[i] = new(machines[i].machine.Identifier, await machines[i].Command.Command1().ConfigureAwait(false));
                }

                return results;
            }
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
