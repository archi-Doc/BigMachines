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

    public partial class TestMachine2<TState> : Machine<int, TState>
        where TState : struct
    {
        public TestMachine2(BigMachine<int> bigMachine)
            : base(bigMachine)
        {// Custom
        }
    }

    [StateMachine(0x34)]
    public partial class TestMachine3 : TestMachine2<TestMachine3.State>
    {
        public TestMachine3(BigMachine<int> bigMachine)
            : base(bigMachine)
        {// Custom
        }
    }

    [TinyhandObject(UseServiceProvider = true)]
    [StateMachine(0x35)]
    public partial class TestMachine : Machine<int, TestMachine.State>
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

        [StateMethod(CheckStateChange = true)]
        protected StateResult ErrorState(StateParameter parameter)
        {
            if (parameter.RunType == RunType.CanEnter || parameter.RunType == RunType.CanExit)
            {
                return StateResult.Continue;
            }

            this.ChangeStateInternal(State.Initial);
            return StateResult.Continue;
        }

        [StateMethod(CheckStateChange = true)]
        protected StateResult Initial(StateParameter parameter)
        {// lock(this)
            if (parameter.RunType == RunType.CanEnter || parameter.RunType == RunType.CanExit)
            {
                return StateResult.Continue;
            }

            Console.WriteLine("TestMachine(Initial)");

            this.SetTimeout(TimeSpan.FromSeconds(0.5));
            this.ChangeStateInternal(TestMachine.State.First);
            return StateResult.Continue;
        }

        [StateMethod]
        protected StateResult First(StateParameter parameter)
        {
            if (parameter.RunType == RunType.CanEnter)
            {
                return StateResult.Continue;
            }

            Console.WriteLine($"TestMachine(First) : {this.Dummy++}");
            if (parameter.Message != null)
            {
                return StateResult.Deny;
            }

            // this.SetTimeout(44.5);
            // this.ChangeStateInternal(State.First);
            return StateResult.Continue;
        }

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
