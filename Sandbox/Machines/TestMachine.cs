// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using Tinyhand;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Sandbox;

[TinyhandObject(UseServiceProvider = true)]
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
        /*var i = this.BigMachine.Continuous.GetInfo(true);
        if (i.Length == 0)
        {
            Console.WriteLine("ContinuousMachine: none");
            return StateResult.Terminate;
        }
        else
        {
            var d = i[0].Interface.CommandTwoWay<double, double>(0);
            Console.WriteLine($"ContinuousMachine: {d * 100:F2}%");
        }*/

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
    /*public enum State
    {// Generated
        Initial,
        First,
        Last,
    }*/

    /*public class Interface : ManMachineInterface<int, TestMachine.State>
    {// Generated
        public Interface(IMachineGroup<int> group, int identifier)
            : base(group, identifier)
        {
        }
    }*/

    public TestMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {// Custom
        this.IsSerializable = true;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
        this.SetLifespan(TimeSpan.FromSeconds(55));
    }

    /*protected override void CreateInterface(int identifier)
    {// Generated
        if (this.InterfaceInstance == null)
        {
            this.Identifier = identifier;
            this.InterfaceInstance = new Interface(this.Group, identifier);
        }
    }*/

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

        /*var task = System.Threading.Tasks.Task.Delay(1000);
        task.ContinueWith(x => { this.ChangeState(State.Two); }); // 1
        this.ReleaseAndInvoke(Task.Delay(1000)); // 2*/

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

    /*protected override StateResult InternalRun(StateParameter parameter)
    {// Generated
        return this.CurrentState switch
        {
            State.Initial => this.Initial(parameter),
            State.First => this.First(parameter),
            // State.Last => this.Last(),
            _ => StateResult.Terminate,
        };
    }*/

    /*protected override bool ChangeStateInternal(State state)
    {// Generated
        if (this.Status == OperationalState.Terminated)
        {
            return false;
        }
        else if (this.CurrentState == state)
        {
            return true;
        }

        bool canExit = true;
        if (this.CurrentState == State.First)
        {
            canExit = this.First(new StateParameter(RunType.CanExit)) != StateResult.Deny;
        }

        bool canEnter = state switch
        {
            State.First => this.First(new StateParameter(RunType.CanEnter)) != StateResult.Deny,
            _ => true,
        };

        if (canExit && canEnter)
        {
            this.CurrentState = state;
            this.RequrieRerun = true;
            return true;
        }
        else
        {
            return false;
        }
    }*/
}
