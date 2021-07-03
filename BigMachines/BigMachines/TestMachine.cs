// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arc.Threading;
using Tinyhand;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines
{
    [TinyhandObject]
    public partial class TestMachine : Machine<int, TestMachine.State>
    {
        public enum State
        {
            Initial,
            First,
            Last,
        }

        public TestMachine()
            : base(default!, default)
        {
        }

        public TestMachine(BigMachine<int> bigMachine, int identifier)
            : base(bigMachine, identifier)
        {
        }

        [Key(2)]
        public int Dummy { get; set; }

        [StateMethod]
        protected StateResult Initial()
        {// lock(this)
            this.SetTimeout(44);
            this.ChangeStateInternal(TestMachine.State.First);
            return StateResult.Continue;
        }

        [StateMethod]
        protected StateResult First(StateInput input)
        {
            if (input == StateInput.CanEnter)
            {
                return StateResult.Terminate;
            }

            this.SetTimeout(44);
            this.ChangeStateInternal(State.First);
            return StateResult.Continue;
        }

        protected override StateResult RunInternal()
        {// Generated
            return this.CurrentState switch
            {
                State.Initial => this.Initial(),
                State.First => this.First(StateInput.Run),
                // State.Last => this.Last(),
                _ => StateResult.Terminate,
            };
        }

        protected override bool ChangeStateInternal(TestMachine.State state)
        {// Generated
            if (this.CurrentState == state)
            {
                return true;
            }

            bool canExit = true;
            if (this.CurrentState == State.First)
            {
                canExit = this.First(StateInput.CanExit) != StateResult.Deny;
            }

            bool canEnter = state switch
            {
                State.First => this.First(StateInput.CanEnter) != StateResult.Deny,
                _ => true,
            };

            if (canExit && canEnter)
            {
                this.CurrentState = state;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
