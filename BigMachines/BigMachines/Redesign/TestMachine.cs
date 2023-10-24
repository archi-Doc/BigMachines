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
            machine.Command.Command1();
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

    public class Interface
    {
        public Interface(TestMachine machine)
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

            public void Command1()
            {
                // var m = new TestMachine();
                // m.Command1();
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

    protected void Initial()
    {
        // this.SetState(State.Second);
    }

    protected async Task Second()
    {
        // this.SetState(State.Second);
    }

    protected void Command1()
    {
    }
}
