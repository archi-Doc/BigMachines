// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using Tinyhand;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Sandbox
{
    [TinyhandObject(UseServiceProvider = true)]
    [StateMachine(0x18aba06a)]
    public partial class StringMachine : Machine<int>
    {
        public StringMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
        {
        }

        [StateMethod(0)]
        public StateResult Initial(StateParameter parameter)
        {
            Console.WriteLine("String");
            return StateResult.Continue;
        }
    }

    internal partial class NestedGenericParent<T>
    {
        [TinyhandObject(UseServiceProvider = true)]
        [StateMachine(126)]
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
    [StateMachine(124)]
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
    }

    [TinyhandObject(UseServiceProvider = true)]
    [StateMachine(125)]
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
        [StateMachine(333)]
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
        [StateMachine(334)]
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
        [StateMachine(444)]
        public partial class NestedMachineT : Machine<int>
        {
            public NestedMachineT(BigMachine<int> bigMachine)
            : base(bigMachine)
            {
                this.Param = default!;
                this.DefaultTimeout = TimeSpan.FromSeconds(1);
            }

            [Key(10)]
            public T Param { get; set; }

            [StateMethod(0)]
            public StateResult Two(StateParameter parameter)
            {
                Console.WriteLine("Two" + this.Param?.ToString());
                return StateResult.Continue;
            }
        }
    }

    public partial class ParentClassT2<T, TIdentifier>
        where TIdentifier : notnull
    {
        [TinyhandObject(UseServiceProvider = true)]
        [StateMachine(123)]
        public partial class NestedMachineT2 : Machine<TIdentifier>
        {
            public NestedMachineT2(BigMachine<TIdentifier> bigMachine)
            : base(bigMachine)
            {
                this.Param = default!;
                this.DefaultTimeout = TimeSpan.FromSeconds(1);
            }

            [Key(10)]
            public T Param { get; set; }

            [StateMethod(0)]
            public StateResult Two(StateParameter parameter)
            {
                Console.WriteLine("T2" + this.Param?.ToString());
                return StateResult.Continue;
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

    [StateMachine(0x34)]
    public partial class TestMachine3 : Machine<int>
    {
        public TestMachine3(BigMachine<int> bigMachine)
            : base(bigMachine)
        {// Custom
        }
    }

    [StateMachine(4)]
    public partial class TestMachine2<TIdentifier> : Machine<TIdentifier>
        where TIdentifier : notnull
    {
        public TestMachine2(BigMachine<TIdentifier> bigMachine)
            : base(bigMachine)
        {// Custom
        }
    }

    [TinyhandObject(UseServiceProvider = true)]
    [StateMachine(0x35, Group = typeof(MachineSingle<>))]
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

        [Key(10)]
        public int Dummy { get; set; }

        [StateMethod(99)]
        protected StateResult ErrorState(StateParameter parameter)
        {
            this.ChangeState(State.Initial);
            return StateResult.Continue;
        }

        [StateMethod(0)]
        protected StateResult Initial(StateParameter parameter)
        {// lock(this)
            Console.WriteLine("TestMachine(Initial)");

            this.SetTimeout(TimeSpan.FromSeconds(0.5));
            this.ChangeState(TestMachine.State.First);
            return StateResult.Continue;
        }

        private bool InitialCanEnter() => true;

        private bool InitialCanExit() => true;

        [StateMethod(1)]
        protected StateResult First(StateParameter parameter)
        {
            Console.WriteLine($"TestMachine(First) : {this.Dummy++}");

            // this.SetTimeout(44.5);
            // this.ChangeStateInternal(State.First);
            return StateResult.Continue;
        }

        protected bool FirstCanEnter() => true;

        protected override void ProcessCommand(CommandPost<int>.Command command)
        {// Custom
            if (command.Message is int x)
            {
                Console.WriteLine($"command: {x}");
                command.Response = x + 1;
            }
        }

        /*protected override StateResult RunInternal(StateParameter parameter)
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
            if (this.Status == MachineStatus.Terminated)
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
                this.StateChanged = true;
                return true;
            }
            else
            {
                return false;
            }
        }*/
    }
}
