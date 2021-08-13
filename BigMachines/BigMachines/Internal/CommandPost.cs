﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class CommandPost<TIdentifier> : IDisposable
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
        public delegate void CommandFunc(Command command);

        /// <summary>
        /// Channel class manages the relationship between CommandPost and registered method.<br/>
        /// You need to call <see cref="Channel.Dispose()"/> when the Channel is no longer necessary.
        /// </summary>
        public class Channel : IDisposable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Channel"/> class.
            /// </summary>
            /// <param name="commandPost">CommandPost.</param>
            /// <param name="method">The method to receive and process commands.</param>
            public Channel(CommandPost<TIdentifier> commandPost, CommandFunc method)
            {
                this.CommandPost = commandPost;
                this.Method = method;
                lock (this.CommandPost.cs)
                {
                    if (this.CommandPost.primaryChannel != null)
                    {
                        throw new InvalidOperationException("Dispose the previous channel.");
                    }

                    this.CommandPost.primaryChannel = this;
                }
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                lock (this.CommandPost.cs)
                {
                    this.CommandPost.primaryChannel = null;
                }

                this.CommandPost = null!;
                this.Method = null!;
            }

            public CommandPost<TIdentifier> CommandPost { get; private set; }

            public CommandFunc Method { get; private set; }
        }

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

                this.LoopChecker = LoopChecker.Instance;
                this.Channel = channel;
                this.Identifier = identifier;
                this.Message = message;
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
        /// <param name="parent">The parent.</param>
        /// <param name="millisecondInterval">The number of milliseconds to wait for each interval.</param>
        public CommandPost(ThreadCoreBase parent, int millisecondInterval = 10)
        {
            lock (this.cs)
            {
                this.MillisecondInterval = millisecondInterval;
                this.Core = new(parent, this.MainLoop);
                this.Core.Thread.Priority = ThreadPriority.AboveNormal;
            }
        }

        /// <summary>
        /// Open a channel to receive a message.
        /// </summary>
        /// <param name="method">The method to receive and process commands.</param>
        /// <returns>Channel.</returns>
        public Channel Open(CommandFunc method)
        {
            return new Channel(this, method);
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

        public void SendGroup<TMessage>(CommandType commandType, object? channel, IEnumerable<TIdentifier> identifiers, TMessage message)
        {
            var messageClone = TinyhandSerializer.Clone(message);
            foreach (var x in identifiers)
            {
                var m = new Command(commandType, channel, x, messageClone);
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

        public KeyValuePair<TIdentifier, TResult?>[] SendGroupTwoWay<TMessage, TResult>(CommandType commandType, object? channel, IEnumerable<TIdentifier> identifiers, TMessage message, int millisecondTimeout = 100)
        {
            if (millisecondTimeout < 0 || millisecondTimeout > MaxMillisecondTimeout)
            {
                millisecondTimeout = MaxMillisecondTimeout;
            }

            var end = Stopwatch.GetTimestamp() + (long)((double)millisecondTimeout / 1000d * (double)Stopwatch.Frequency);

            var commandQueue = new Queue<Command>();
            var messageClone = TinyhandSerializer.Clone(message);
            foreach (var x in identifiers)
            {
                var m = new Command(commandType, channel, x, messageClone);
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
            var method = this.primaryChannel?.Method;
            while (this.concurrentQueue.TryDequeue(out var command))
            {
                if (method != null)
                {
                    var type = command.Type;
                    method(command);
                    command.Type = CommandType.Responded;

                    this.commandResponded.Set();
                }
            }
        }

        private object cs = new();
        private ManualResetEventSlim commandAdded = new(false);
        private ManualResetEventSlim commandResponded = new(false);
        private ConcurrentQueue<Command> concurrentQueue = new();
        private Channel? primaryChannel;

#pragma warning disable SA1124 // Do not use regions
        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls.
#pragma warning restore SA1124 // Do not use regions

        /// <summary>
        /// Finalizes an instance of the <see cref="CommandPost{TIdentifier}"/> class.
        /// </summary>
        ~CommandPost()
        {
            this.Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// free managed/native resources.
        /// </summary>
        /// <param name="disposing">true: free managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // free managed resources.
                    this.primaryChannel?.Dispose();
                }

                // free native resources here if there are any.
                this.disposed = true;
            }
        }
        #endregion
    }
}