// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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

    public UnorderedMachineControl<int, TestMachine.Interface> TestMachine { get; private set; } = new(x => new TestMachine());

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
                item = new(identifier, default!);
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

public abstract class ManMachineInterface
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

    public MachineControl Control { get; private set; }
}

public class Machine<TIdentifier> : Machine
{
    public Machine(MachineControl<TIdentifier> control, TIdentifier identifier)
        : base(control)
    {
        this.Control = control;
        this.Identifier = identifier;
    }

    public new MachineControl<TIdentifier> Control { get; private set; }

    public TIdentifier Identifier { get; private set; }
}

// [MachineObject] // ulong id = FarmHash.Hash64(Type.FullName)
internal partial class TestMachine : Machine<int>
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
        }
    }

    private Interface @interface;

    public class Interface
    {
        public Interface(MachineControl control, int identifier)
        {
            this.control = control;
            this.identifier = identifier;
        }

        private readonly MachineControl control;
        private readonly int identifier;

        public static class Command
        {
            public static void Command1()
            {
                var m = new TestMachine();
                m.Command1();
            }
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
