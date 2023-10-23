// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Tinyhand;
using Tinyhand.IO;
using ValueLink;

namespace BigMachines.Redesign;

[TinyhandObject]
public partial class TestBigMachine : ITinyhandSerialize<TestBigMachine>
{
    public TestBigMachine()
    {
    }

    public UnorderedMachineControl<int, TestMachine.Interface> TestMachine { get; private set; } = new((x, y) => new TestMachine(x, y).InterfaceInstance);

    static void ITinyhandSerialize<TestBigMachine>.Serialize(ref TinyhandWriter writer, scoped ref TestBigMachine? value, TinyhandSerializerOptions options)
    {
        throw new System.NotImplementedException();
    }

    static void ITinyhandSerialize<TestBigMachine>.Deserialize(ref TinyhandReader reader, scoped ref TestBigMachine? value, TinyhandSerializerOptions options)
    {
        throw new System.NotImplementedException();
    }
}

[TinyhandObject]
public partial class UnorderedMachineControl<TIdentifier, TInterface> : MachineControl<TIdentifier>
{
    public UnorderedMachineControl(Func<UnorderedMachineControl<TIdentifier, TInterface>, TIdentifier, TInterface> createDelegate)
    {
        this.createDelegate = createDelegate;
    }

    private Func<UnorderedMachineControl<TIdentifier, TInterface>, TIdentifier, TInterface> createDelegate; // MachineControl + Identifier -> Machine.Interface

    [TinyhandObject(Tree = true)]
    [ValueLinkObject(Isolation = IsolationLevel.Serializable)]
    private partial class Item
    {
        public Item()
        {
            this.Identifier = default!;
            this.Interface = default!;
        }

        public Item(TIdentifier identifier, TInterface @interface)
        {
            this.Identifier = identifier;
            this.Interface = @interface;
        }

#pragma warning disable SA1401 // Fields should be private

        [Key(0)]
        [Link(Primary = true, Unique = true, Type = ChainType.Unordered)]
        public TIdentifier Identifier;

        [Key(1)]
        public TInterface Interface;

#pragma warning restore SA1401 // Fields should be private
    }

    private readonly Item.GoshujinClass items = new();

    public TInterface? TryGet(TIdentifier identifier)
    {
        lock (this.items.SyncObject)
        {
            if (this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                return item.Interface;
            }
            else
            {
                return default;
            }
        }
    }

    public TInterface GetOrCreate(TIdentifier identifier)
    {
        lock (this.items.SyncObject)
        {
            if (!this.items.IdentifierChain.TryGetValue(identifier, out var item))
            {
                item = new(identifier, this.createDelegate(this, identifier));
            }

            return item.Interface;
        }
    }
}

public partial class MachineControl<TIdentifier> : MachineControl
{
}

public partial class MachineControl
{
}

public abstract class ManMachineInterface // MANMACHINE INTERFACE by Shirow.
{
    public ManMachineInterface()
    {
    }
}

public class Machine
{
    public Machine(MachineControl control)
    {
        this.Control = control;
    }

    public MachineControl Control { get; }

#pragma warning disable SA1401
    protected object? interfaceInstance;
#pragma warning restore SA1401
}

public class Machine<TIdentifier> : Machine
{
    public Machine(MachineControl<TIdentifier> control, TIdentifier identifier)
        : base(control)
    {
        this.Control = control;
        this.Identifier = identifier;
    }

    public new MachineControl<TIdentifier> Control { get; }

    public TIdentifier Identifier { get; }
}

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

                this.machine.Control.Send(packet);

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
