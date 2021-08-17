// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using Tinyhand;

namespace BigMachines
{
    /// <summary>
    /// CommandPost is dependency-free pub/sub service.<br/>
    /// </summary>
    /// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
    public class CommandPost<TIdentifier>
        where TIdentifier : notnull
    {
        public const int MaxMillisecondTimeout = 3_000;

        /// <summary>
        /// Command type.
        /// </summary>
        public enum CommandType
        {
            /// <summary>
            /// One-way command.
            /// </summary>
            Command,

            /// <summary>
            /// Two-way command which requires a response
            /// </summary>
            CommandTwoWay,

            /// <summary>
            /// Responded. Used internally.
            /// </summary>
            Responded,

            /// <summary>
            /// Changing state. Used internally.
            /// </summary>
            State,

            /// <summary>
            /// Changing state. Used internally.
            /// </summary>
            StateTwoWay,

            /// <summary>
            /// Run. Used internally.
            /// </summary>
            Run,

            /// <summary>
            /// Run. Used internally.
            /// </summary>
            RunTwoWay,
        }

        /// <summary>
        /// Defines the type of delegate used to receive and process commands.
        /// </summary>
        /// <param name="command">Command.</param>
        public delegate void CommandDelegate(Command command);

        /// <summary>
        /// Command class contains command information.
        /// </summary>
        public class Command
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Command"/> class.
            /// </summary>
            /// <param name="type">CommandType.</param>
            /// <param name="channel">Channel.</param>
            /// <param name="identifier">Identifier.</param>
            /// <param name="message">Message.</param>
            public Command(CommandType type, object? channel, TIdentifier identifier, object? message)
            {
                if (BigMachine<TIdentifier>.EnableLoopChecker && LoopChecker.Instance == null)
                {// LoopChecker enabled.
                    LoopChecker.Instance = new();
                }

                this.Type = type;
                this.Channel = channel;
                this.Identifier = identifier;
                this.Message = message;
                this.LoopChecker = LoopChecker.Instance;
            }

            public CommandType Type { get; internal set; }

            public object? Channel { get; }

            public TIdentifier Identifier { get; }

            public object? Message { get; }

            public object? Response { get; set; }

            internal LoopChecker? LoopChecker { get; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandPost{TIdentifier}"/> class.
        /// </summary>
        /// <param name="method">CommandDelegate.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="millisecondInterval">The number of milliseconds to wait for each interval.</param>
        public CommandPost(CommandDelegate method, ThreadCoreBase parent, int millisecondInterval = 10)
        {
            this.commandDelegate = method;
            this.MillisecondInterval = millisecondInterval;
            this.Core = new(parent, this.MainLoop);
            this.Core.Thread.Priority = ThreadPriority.AboveNormal;
        }

        /// <summary>
        /// Send a message to the receiver.<br/>
        /// Caution! TMessage must be serializable by Tinyhand because the message will be cloned and passed to the receiver.
        /// </summary>
        /// <typeparam name="TMessage">The type of a message.</typeparam>
        /// <param name="commandType">CommandType of a message.</param>
        /// <param name="channel">The channel to receive message.</param>
        /// <param name="identifier">The identifier of a message.</param>
        /// <param name="message">The message to send.<br/>Must be serializable by Tinyhand because the message will be cloned and passed to the receiver.</param>
        public void Send<TMessage>(CommandType commandType, object? channel, TIdentifier identifier, TMessage message)
        {
            var m = new Command(commandType, channel, identifier, TinyhandSerializer.Clone(message));
            this.concurrentQueue.Enqueue(m);
            this.commandAdded.Set();
        }

        public void SendGroup<TMessage>(CommandType commandType, IMachineGroup<TIdentifier> group, IEnumerable<TIdentifier> identifiers, TMessage message) => this.SendGroups(commandType, Enumerable.Repeat(group, identifiers.Count()), identifiers, message);

        public void SendGroups<TMessage>(CommandType commandType, IEnumerable<IMachineGroup<TIdentifier>> groups, IEnumerable<TIdentifier> identifiers, TMessage message)
        {
            var messageClone = TinyhandSerializer.Clone(message);
            var g = groups.GetEnumerator();
            var i = identifiers.GetEnumerator();
            while (g.MoveNext() && i.MoveNext())
            {
                var m = new Command(commandType, g.Current, i.Current, messageClone);
                this.concurrentQueue.Enqueue(m);
            }

            this.commandAdded.Set();
        }

        public TResult? SendTwoWay<TMessage, TResult>(CommandType commandType, object? channel, TIdentifier identifier, TMessage message, int millisecondTimeout = 100)
        {
            if (millisecondTimeout < 0 || millisecondTimeout > MaxMillisecondTimeout)
            {
                millisecondTimeout = MaxMillisecondTimeout;
            }

            var end = Stopwatch.GetTimestamp() + (long)((double)millisecondTimeout / 1000d * (double)Stopwatch.Frequency);

            var m = new Command(commandType, channel, identifier, TinyhandSerializer.Clone(message));
            this.concurrentQueue.Enqueue(m);

            var taskFlag = Thread.CurrentThread == this.Core.Thread;
            if (!taskFlag)
            {
                this.commandAdded.Set();
            }

            if (commandType == CommandType.CommandTwoWay ||
                commandType == CommandType.StateTwoWay ||
                commandType == CommandType.RunTwoWay)
            {
                if (taskFlag)
                {// Another thread required.
                    Task.Run(this.MainProcess);
                }

                try
                {
                    while (true)
                    {
                        this.commandResponded.Wait(this.MillisecondInterval, this.Core.CancellationToken);
                        if (m.Type == CommandType.Responded)
                        {
                            this.commandResponded.Reset();
                            if (m.Response is TResult result)
                            {
                                return result;
                            }
                            else
                            {
                                return default;
                            }
                        }

                        if (Stopwatch.GetTimestamp() >= end)
                        {// Timeout
                            Console.WriteLine("Timeout");
                            return default;
                        }
                    }
                }
                catch
                {
                    return default;
                }
            }
            else
            {
                return default;
            }
        }

        public KeyValuePair<TIdentifier, TResult?>[] SendGroupTwoWay<TMessage, TResult>(CommandType commandType, IMachineGroup<TIdentifier> group, IEnumerable<TIdentifier> identifiers, TMessage message, int millisecondTimeout = 100) => this.SendGroupsTwoWay<TMessage, TResult>(commandType, Enumerable.Repeat(group, identifiers.Count()), identifiers, message, millisecondTimeout);

        public KeyValuePair<TIdentifier, TResult?>[] SendGroupsTwoWay<TMessage, TResult>(CommandType commandType, IEnumerable<IMachineGroup<TIdentifier>> groups, IEnumerable<TIdentifier> identifiers, TMessage message, int millisecondTimeout = 100)
        {
            if (millisecondTimeout < 0 || millisecondTimeout > MaxMillisecondTimeout)
            {
                millisecondTimeout = MaxMillisecondTimeout;
            }

            var end = Stopwatch.GetTimestamp() + (long)((double)millisecondTimeout / 1000d * (double)Stopwatch.Frequency);

            var commandQueue = new Queue<Command>();
            var messageClone = TinyhandSerializer.Clone(message);
            var g = groups.GetEnumerator();
            var i = identifiers.GetEnumerator();
            while (g.MoveNext() && i.MoveNext())
            {
                var m = new Command(commandType, g.Current, i.Current, messageClone);
                commandQueue.Enqueue(m);
                this.concurrentQueue.Enqueue(m);
            }

            var taskFlag = Thread.CurrentThread == this.Core.Thread;
            if (!taskFlag)
            {
                this.commandAdded.Set();
            }

            var responseList = new KeyValuePair<TIdentifier, TResult?>[commandQueue.Count];
            var responseNumber = 0;
            if (commandType == CommandType.CommandTwoWay ||
                commandType == CommandType.StateTwoWay ||
                commandType == CommandType.RunTwoWay)
            {
                if (taskFlag)
                {// Another thread required.
                    Task.Run(this.MainProcess);
                }

                try
                {
                    while (commandQueue.Count > 0)
                    {
                        this.commandResponded.Wait(this.MillisecondInterval, this.Core.CancellationToken);
                        var flag = false;
                        while (commandQueue.TryPeek(out var c))
                        {
                            if (c.Type == CommandType.Responded)
                            {
                                flag = true;
                                commandQueue.Dequeue();
                                if (c.Response is TResult result)
                                {
                                    responseList[responseNumber++] = new(c.Identifier, result);
                                }
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (flag)
                        {
                            this.commandResponded.Reset();
                        }

                        if (Stopwatch.GetTimestamp() >= end)
                        {// Timeout
                            goto SendGroupTwoWay_Exit;
                        }
                    }

SendGroupTwoWay_Exit:
                    if (responseList.Length != responseNumber)
                    {
                        Array.Resize(ref responseList, responseNumber);
                    }

                    return responseList;
                }
                catch
                {
                    return Array.Empty<KeyValuePair<TIdentifier, TResult?>>();
                }
            }
            else
            {
                return Array.Empty<KeyValuePair<TIdentifier, TResult?>>();
            }
        }

        public int MillisecondInterval { get; }

        public ThreadCore Core { get; }

        private void MainLoop(object? parameter)
        {
            var core = (ThreadCore)parameter!;
            while (!core.IsTerminated)
            {
                try
                {
                    this.commandAdded.Wait(this.MillisecondInterval, core.CancellationToken);
                }
                catch
                {
                    break;
                }

                this.commandAdded.Reset();

                this.MainProcess();
            }
        }

        private void MainProcess()
        {
            while (this.concurrentQueue.TryDequeue(out var command))
            {
                    var type = command.Type;
                    this.commandDelegate(command);
                    command.Type = CommandType.Responded;

                    this.commandResponded.Set();
            }
        }

        private CommandDelegate commandDelegate;
        private ManualResetEventSlim commandAdded = new(false);
        private ManualResetEventSlim commandResponded = new(false);
        private ConcurrentQueue<Command> concurrentQueue = new();
    }
}
