// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using Tinyhand;

namespace Sandbox;

[BigMachineObject(Comprehensive = true)]
[AddMachine<ParentClass.TinyMachine2<int>>()]
public partial class TestBigMachine;

[MachineObject]
public partial class CommandTestMachine : Machine<int>
{
    public CommandTestMachine()
        : base()
    {
    }

    [CommandMethod(All = true)]
    public CommandResult CommandParam(string name, int age)
        => CommandResult.Success;

    [CommandMethod(All = true)]
    public CommandResult<int> CommandResponse(int x)
        => new(CommandResult.Success, x + 2);

    [CommandMethod(WithLock = false, All = true)]
    public CommandResult CommandNoLock()
        => CommandResult.Success;

    [CommandMethod(All = true)]
    public async Task<CommandResult<int>> TaskCommandResponse(int x)
        => new(CommandResult.Success, x + 3);

    [CommandMethod(All = true)]
    public async Task<CommandResult> TaskCommandParam(string name, int age)
        => CommandResult.Success;
}

[MachineObject]
public partial class AccessibilityMachine : Machine
{
    public AccessibilityMachine()
        : base()
    {
    }

    [StateMethod(0)]
    public StateResult PublicState(StateParameter parameter)
        => StateResult.Terminate;

    [StateMethod]
    protected StateResult ProtectedState(StateParameter parameter)
        => StateResult.Terminate;

    [StateMethod]
    private StateResult PrivateState(StateParameter parameter)
        => StateResult.Terminate;
}

/*[MachineObject]
public partial class AccessibilityMachine2 : AccessibilityMachine
{// PrivateState() is currently not supported.
    public AccessibilityMachine2()
        : base()
    {
    }
}*/

[MachineObject(UseServiceProvider = true)]
public partial class NoDefaultConstructorMachine : Machine
{
    public NoDefaultConstructorMachine(IServiceProvider serviceProvider)
        : base()
    {
    }

    [StateMethod(0)]
    public StateResult PublicState(StateParameter parameter)
        => StateResult.Terminate;
}

public partial class ParentClass
{
    /*[BigMachineObject]
    public partial class NestedBigMachine
    {
        public NestedBigMachine()
        {
        }
    }*/

    [MachineObject]
    public partial class TinyMachine : Machine
    {
        public TinyMachine()
            : base()
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
        }

        [StateMethod(0)]
        public StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine($"Tiny machine: {this.count++}");
            if (this.count > 4)
            {
                return StateResult.Terminate;
            }

            return StateResult.Continue;
        }

        [CommandMethod]
        protected CommandResult<int> Command1(int x)
            => new(CommandResult.Success, x + 2);

        protected override void OnTermination()
        {
            Console.WriteLine($"Tiny machine: Terminated");
        }

        private int count;
    }

    [TinyhandObject]
    [MachineObject(Control = MachineControlKind.Single)]
    public partial class TinyMachine2<TData> : Machine
    {
        public TinyMachine2()
            : base()
        {
        }

        [StateMethod(0)]
        public StateResult Initial(StateParameter parameter)
            => StateResult.Terminate;

        [StateMethod]
        public StateResult Second(StateParameter parameter)
            => StateResult.Terminate;

        [StateMethod]
        public async Task<StateResult> Third(StateParameter parameter)
            => StateResult.Terminate;
    }

    /*[MachineObject]
    private partial class PrivateMachine : Machine
    {// Private machine is not supported.
        public PrivateMachine()
            : base()
        {
        }

        [StateMethod(0)]
        public StateResult Initial(StateParameter parameter)
            => StateResult.Terminate;
    }*/
}


/*[TinyhandObject(UseServiceProvider = true)]
[MachineObject(0x436a0f8f, Continuous = true)]
public partial class ContinuousMachine : Machine<int>
{
    public ContinuousMachine(BigMachine<int> bigMachine)
    : base(bigMachine)
    {
        this.total = 5_000_000_000;
        this.IsSerializable = true;
    }

    [StateMethod(0)]
    public StateResult Initial(StateParameter parameter)
    {
        for (var n = 0; n < 1_000_000; n++)
        {
            this.count++;
        }

        if (this.count > this.total)
        {
            return StateResult.Terminate;
        }

        return StateResult.Continue;
    }

    [Key(11)]
    private long total;

    [Key(12)]
    private long count;
}

[MachineObject(0xbed5cbf5)]
public partial class ContinuousWatcher : Machine<int>
{
    public ContinuousWatcher(BigMachine<int> bigMachine)
    : base(bigMachine)
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    public StateResult Initial(StateParameter parameter)
    {
        var results = this.BigMachine.Continuous.CommandAndReceiveAsync<int, int, double>(true, 0, 0).Result;
        if (results.Length == 0)
        {
            Console.WriteLine("ContinuousMachine: none");

            var info = this.BigMachine.Continuous.GetInfo(true);
            if (info.Length == 0)
            {// No running machine.
                return StateResult.Terminate;
            }
        }
        else
        {
            foreach (var x in results)
            {
                Console.WriteLine($"ContinuousMachine {x.Key}: {x.Value * 100:F2}%");
            }
        }

        return StateResult.Continue;
    }
}

internal partial class NestedGenericParent<T>
{
    [TinyhandObject(UseServiceProvider = true)]
    [MachineObject(126)]
    private partial class NestedGenericMachine<TIdentifier> : Machine<TIdentifier>
        where TIdentifier : notnull
    {
        public NestedGenericMachine(BigMachine<TIdentifier> bigMachine)
        : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
        }

        [StateMethod(0)]
        public StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine("NestedGeneric");
            return StateResult.Continue;
        }
    }
}

[TinyhandObject(UseServiceProvider = true)]
[MachineObject(124)]
public partial class GenericMachine<TIdentifier> : Machine<TIdentifier>
    where TIdentifier : notnull
{
    public GenericMachine(BigMachine<TIdentifier> bigMachine)
    : base(bigMachine)
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    public StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine("Generic");
        return StateResult.Continue;
    }

    [CommandMethod(3)]
    protected int GetInfo(int n)
    {// void, Task
        return 4;
    }
}

[TinyhandObject(UseServiceProvider = true)]
[MachineObject(125)]
public partial class GenericMachine2<TIdentifier> : Machine<TIdentifier>
    where TIdentifier : notnull
{
    public GenericMachine2(BigMachine<TIdentifier> bigMachine)
    : base(bigMachine)
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [StateMethod(0)]
    public StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine("Generic");
        return StateResult.Continue;
    }
}

public partial class ParentClass
{
    [TinyhandObject(UseServiceProvider = true)]
    [MachineObject(333)]
    public partial class NestedMachine : Machine<int>
    {
        public NestedMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
            this.SetLifespan(TimeSpan.FromSeconds(10));
        }

        [StateMethod(0)]
        public StateResult One(StateParameter parameter)
        {
            Console.WriteLine("One");
            return StateResult.Continue;
        }
    }
}

public partial class ParentClass2
{
    [TinyhandObject(UseServiceProvider = true)]
    [MachineObject(334)]
    private partial class NestedMachine2<TIdentifier> : Machine<TIdentifier>
        where TIdentifier : notnull
    {
        public NestedMachine2(BigMachine<TIdentifier> bigMachine)
        : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
            this.SetLifespan(TimeSpan.FromSeconds(10));
        }

        [StateMethod(0)]
        public StateResult One(StateParameter parameter)
        {
            Console.WriteLine("One");
            return StateResult.Continue;
        }
    }
}

public partial class ParentClassT<T>
{
    [TinyhandObject(UseServiceProvider = true)]
    [MachineObject(444)]
    public partial class NestedMachineT : Machine<int>
    {
        public NestedMachineT(BigMachine<int> bigMachine)
        : base(bigMachine)
        {
            this.Param = default!;
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
        }

        [Key(11)]
        public T Param { get; set; }

        [StateMethod(0)]
        public StateResult Two(StateParameter parameter)
        {
            Console.WriteLine("Two" + this.Param?.ToString());
            return StateResult.Continue;
        }
    }
}

public partial class ParentClassT2<T, TIdentifier2>
    where TIdentifier2 : notnull
{
    [TinyhandObject(UseServiceProvider = true)]
    [MachineObject(123)]
    public partial class NestedMachineT2 : Machine<TIdentifier2>
    {
        public NestedMachineT2(BigMachine<TIdentifier2> bigMachine)
        : base(bigMachine)
        {
            this.Param = default!;
            this.DefaultTimeout = TimeSpan.FromSeconds(1);
        }

        [Key(11)]
        public T Param { get; set; }

        [StateMethod(0)]
        public StateResult Two(StateParameter parameter)
        {
            Console.WriteLine("T2" + this.Param?.ToString());
            return StateResult.Continue;
        }

        [CommandMethod(0)]
        protected async Task TestCommand()
        {
        }
    }
}

public class TestGroup : MachineGroup<int>
{
    internal TestGroup(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
    }
}

public class TestGroup2<TIdentifier> : MachineGroup<TIdentifier>
    where TIdentifier : notnull
{
    internal TestGroup2(BigMachine<TIdentifier> bigMachine)
        : base(bigMachine)
    {
    }
}

[MachineObject(0x34)]
public partial class TestMachine3 : Machine<int>
{
    public TestMachine3(BigMachine<int> bigMachine)
        : base(bigMachine)
    {// Custom
    }
}

[MachineObject(4)]
public partial class TestMachine2<TIdentifier> : Machine<TIdentifier>
    where TIdentifier : notnull
{
    public TestMachine2(BigMachine<TIdentifier> bigMachine)
        : base(bigMachine)
    {// Custom
    }
}

[TinyhandObject(UseServiceProvider = true)]
[MachineObject(0x35, Group = typeof(SingleGroup<>))]
public partial class TestMachine : Machine<int>
{
    public TestMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {// Custom
        this.IsSerializable = true;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
        this.SetLifespan(TimeSpan.FromSeconds(55));
    }

    [Key(11)]
    public int Dummy { get; set; }

    [StateMethod(99)]
    protected StateResult ErrorState(StateParameter parameter)
    {
        this.ChangeState(State.Initial);
        return StateResult.Continue;
    }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine("TestMachine(Initial)");

        this.SetTimeout(TimeSpan.FromSeconds(0.5));
        this.ChangeState(TestMachine.State.First);
        return StateResult.Continue;
    }

    private bool InitialCanEnter() => true;

    private bool InitialCanExit() => true;

    [StateMethod(1)]
    protected async Task<StateResult> First(StateParameter parameter)
    {
        Console.WriteLine($"TestMachine(First) : {this.Dummy++}");

        Console.WriteLine("Delay start");
        await Task.Delay(1000).WithoutLock(this);
        Console.WriteLine("Delay end");

        // this.SetTimeout(44.5);
        // this.ChangeStateInternal(State.First);
        return StateResult.Continue;
    }

    protected bool FirstCanEnter() => true;

    [CommandMethod(33, WithLock = true)]
    protected int GetInfo(int n)
    {// void, Task
        return 4;
    }

    [CommandMethod(3, WithLock = false)]
    protected async Task<int> GetInfo2(int n)
    {// void, Task
        return 4;
    }

    [CommandMethod(4)]
    protected async Task<int> GetInfo4(int message)
    {// void, Task
        return message + 1;
    }
}*/
