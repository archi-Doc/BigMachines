// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arc.Threading;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines
{
    public class BigMachine<TIdentifier>
        where TIdentifier : notnull
    {
        public BigMachine(ThreadCoreBase parent)
        {
            this.CommandPost = new(parent);
            this.CommandPost.Open(this.Distributor);
        }

        public CommandPost CommandPost { get; }

        public void Send<TMessage>(TIdentifier idenfitier, TMessage message)
        {
            this.CommandPost.Send<KeyValuePair<TIdentifier, TMessage>>(0, new KeyValuePair<TIdentifier, TMessage>(idenfitier, message));
        }

        public TResponse? SendTwoWay<TMessage, TResponse>(TIdentifier idenfitier, TMessage message)
        {
            return this.CommandPost.SendTwoWay<KeyValuePair<TIdentifier, TMessage>, TResponse>(0, new KeyValuePair<TIdentifier, TMessage>(idenfitier, message));
        }

        private void Distributor(CommandPost.Command command)
        {
            var id = default(TIdentifier)!;
            if (this.identificationToMachine.TryGetValue(id, out var machine))
            {
                lock (machine)
                {
                    machine.ProcessCommand(command);
                }
            }
        }

        private ConcurrentDictionary<TIdentifier, Machine<TIdentifier>> identificationToMachine = new();
    }

    /*public interface IMachine<TIdentification>
        where TIdentification : notnull
    {
        void Run();
    }*/

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class StateMethodAttribute : Attribute
    {
        public StateMethodAttribute()
        {
        }
    }

    public enum StateMethodType
    {
        Run,
        CanEnter,
        CanExit,
    }

    public enum StateMethodInput
    {
        CanEnter,
        Run,
        CanExit,
    }

    public enum StateMethodResult
    {
        Continue,
        Terminate,
        Deny,
    }

    public class Machine<TIdentifier>
        where TIdentifier : notnull
    {
        public BigMachine<TIdentifier> BigMachine { get; }

        public TIdentifier Identifier { get; }

        public TState CurrentState { get; protected set; }

        public Machine(BigMachine<TIdentifier> bigMachine, TIdentifier identifier)
        {
            this.BigMachine = bigMachine;
            this.Identifier = identifier;
        }

        public void Run()
        {
            lock (this)
            {
                this.RunInternal();
            }
        }

        internal virtual void ProcessCommand(CommandPost.Command command)
        {
            return;
        }

        protected virtual void RunInternal()
        {
        }
    }

    public interface InitialToIdentification<TIdentification>
        where TIdentification : notnull
    {
        TIdentification GetIdentification();
    }

    public class TestMachine : Machine<TestMachine.Initial, TestMachine.Identification, TestMachine.State>
    {
        public class Initial : InitialToIdentification<Identification>
        {
            public Identification GetIdentification()
            {
                throw new NotImplementedException();
            }
        }

        public class Identification
        {
            public int Id { get; set; }
        }

        public enum State
        {
            Initial,
            First,
            Last,
        }

        public TestMachine(Initial initialCondition)
        {
            this.InitialCondition = initialCondition;
        }

        protected override void RunInternal()
        {
            switch (this.CurrentState)
            {
                case State.Initial:
                    this.Initial();
                    break;

                case State.First:
                    this.First(StateMethodInput.Run);
                    break;
            }
        }

        [StateMethod(StateMethodType.CanEnter)]
        protected StateMethodResult Initial()
        {// lock(this)
            this.Wait(44);
            this.ChangeState(to);
            return StateMethodResult.Continue;
        }

        [StateMethod]
        protected StateMethodResult First(StateMethodInput input)
        {
            if (input == StateMethodInput.CanEnter)
            {
                return StateMethodResult.Terminate;
            }

            this.Wait(44);
            this.ChangeState(to);
            return StateMethodResult.Continue;
        }

        protected StateMethodResult ChangeState(TestMachine.State state)
        {
            if (this.CurrentState == state)
            {
                return StateMethodResult.Continue;
            }

            bool canExit = true;
            if (this.CurrentState == State.First)
            {
                canExit = this.First(StateMethodInput.CanExit) != StateMethodResult.Deny;
            }

            bool canEnter = state switch
            {
                State.First => this.First(StateMethodInput.CanEnter) != StateMethodResult.Deny,
                _ => true,
            };

            if (canExit && canEnter)
            {
                this.CurrentState = state;
                return StateMethodResult.Continue;
            }
            else
            {
                return StateMethodResult.Terminate;
            }
        }
    }

    /*public class TestMachine : Machine<TestState>
    {
        public enum State
        {
            Initial,
            First,
            Last,
        }

        Dictionary<State, Func<State, StateResult>> info;

        public TestMachine()
        {
            info = new();
            info.Add(State.Initial, this.Initial);
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
        public class StateAttribute : Attribute
        {
            public StateAttribute(TestState state)
            {
            }
        }

        public StateResult Initial(State previous)
        {
            return this.Wait(44);
            this.ChangeState(from);
            return StateResult.Continue;
        }
    }*/
}
