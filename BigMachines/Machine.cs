// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arc.Threading;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
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

        public ManMachineInterface<TIdentifier, TState>? GetMachine<TState>(TIdentifier identifier)
        {
            if (this.identificationToMachine.TryGetValue(identifier, out var machine))
            {
                if (machine.GetStateType() != typeof(TState))
                {
                    throw new InvalidOperationException();
                }

                return new ManMachineInterface<TIdentifier, TState>(this, identifier);
            }

            return null;
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

        private ConcurrentDictionary<TIdentifier, MachineBase<TIdentifier>> identificationToMachine = new();
        private ConcurrentDictionary<TIdentifier, Type> identificationToStateType = new();
    }

    public class ManMachineInterface<TIdentifier, TState>
        where TIdentifier : notnull
    {
        public BigMachine<TIdentifier> BigMachine { get; }

        public TIdentifier Identifier { get; }

        internal ManMachineInterface(BigMachine<TIdentifier> bigMachine, TIdentifier identifier)
        {
            this.BigMachine = bigMachine;
            this.Identifier = identifier;
        }

        public void Send<TMessage>(TIdentifier idenfitier, TMessage message)
        {
            this.BigMachine.CommandPost.Send<KeyValuePair<TIdentifier, TMessage>>(0, new KeyValuePair<TIdentifier, TMessage>(idenfitier, message));
        }

        public TResponse? SendTwoWay<TMessage, TResponse>(TIdentifier idenfitier, TMessage message)
        {
            return this.BigMachine.CommandPost.SendTwoWay<KeyValuePair<TIdentifier, TMessage>, TResponse>(0, new KeyValuePair<TIdentifier, TMessage>(idenfitier, message));
        }
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

    public enum StateInput
    {
        CanEnter,
        Run,
        CanExit,
    }

    public enum StateResult
    {
        Continue,
        Terminate,
        Deny,
    }

    public abstract class MachineBase<TIdentifier>
        where TIdentifier : notnull
    {
        public MachineBase(BigMachine<TIdentifier> bigMachine, TIdentifier identifier)
        {
            this.BigMachine = bigMachine;
            this.Identifier = identifier;
        }

        public BigMachine<TIdentifier> BigMachine { get; }

        public TIdentifier Identifier { get; }

        public virtual Type GetStateType() => throw new InvalidOperationException();

        internal void ProcessCommand(CommandPost.Command command)
        {
        }
    }

    public class Machine<TIdentifier, TState> : MachineBase<TIdentifier>
        where TIdentifier : notnull
    {
        public Machine(BigMachine<TIdentifier> bigMachine, TIdentifier identifier)
            : base(bigMachine, identifier)
        {
            this.CurrentState = default!;
        }

        // public BigMachine<TIdentifier> BigMachine { get; }

        // public TIdentifier Identifier { get; }

        public TState CurrentState { get; protected set; }

        // public virtual Type GetStateType() => throw new InvalidOperationException();

        public void Run()
        {
            lock (this)
            {
                this.RunInternal();
            }
        }

        internal void SetTimeout(int millisecondToWait)
        {
        }

        protected virtual bool ChangeState(TState state) => false;

        protected virtual void RunInternal()
        {
        }
    }

    public partial class TestMachine : Machine<int, TestMachine.State>
    {
        public enum State
        {
            Initial,
            First,
            Last,
        }

        public TestMachine(BigMachine<int> bigMachine, int identifier)
            : base(bigMachine, identifier)
        {
        }

        protected override void RunInternal()
        {
            switch (this.CurrentState)
            {
                case State.Initial:
                    this.Initial();
                    break;

                case State.First:
                    this.First(StateInput.Run);
                    break;
            }
        }

        protected StateResult Initial()
        {// lock(this)
            this.SetTimeout(44);
            this.ChangeState(TestMachine.State.First);
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
            this.ChangeState(State.First);
            return StateResult.Continue;
        }

        protected override bool ChangeState(TestMachine.State state)
        {
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
