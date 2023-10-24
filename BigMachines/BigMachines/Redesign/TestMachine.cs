// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

namespace BigMachines.Redesign;

// [MachineObject] // ulong id = FarmHash.Hash64(Type.FullName)
public partial class TestMachine : Machine<int>
{
    public TestMachine(MachineControl<int> control, int identifier)
        : base(control, identifier)
    {
    }

    public static void Test1()
    {
        var bigMachine = new TestBigMachine();
        var machine = bigMachine.TestMachine.TryGet(0);
        if (machine is not null)
        {
            _ = machine.Command.Command1();
            _ = machine.RunAsync();
        }
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

            public Task<bool> Command2(string name)
            {
                byte[] packet;
                using (var writer = default(TinyhandWriter))
                {
                    writer.Write(this.machine.Identifier);
                    writer.Write(name);
                    packet = writer.FlushAndGetArray();
                }

                return Task.FromResult(true);

                // this.machine.Control.Send(packet);

                // var m = new TestMachine();
                // m.Command1();
            }

            private readonly TestMachine machine;
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

    public CommandResult Command1()
    {
        return CommandResult.Success;
    }
}
