﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using Tinyhand;

namespace BigMachines
{
    /// <summary>
    /// CommandPost is dependency-free pub/sub service.<br/>
    /// It's easy to use.<br/>
    /// 1. Open a channel (register a subscriber) : .<br/>
    /// 2. Send a message (publish) : "/>.
    /// </summary>
    public class CommandPost : IDisposable
    {
        public const int MaxMillisecondTimeout = 3_000;

        public enum CommandType
        {
            CommandAndForget,
            RequireResponse,
            Responded,
        }

        public delegate void CommandFunc(Command command);

        /// <summary>
        /// Channel class manages the relationship between CommandPost and registered method.<br/>
        /// You need to call <see cref="Channel.Dispose()"/> when the Channel is no longer necessary.
        /// </summary>
        public class Channel : IDisposable
        {   
            public Channel(CommandPost commandPost, CommandFunc method)
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

            public void Dispose()
            {
                lock (this.CommandPost.cs)
                {
                    this.CommandPost.primaryChannel = null;
                }

                this.CommandPost = null!;
                this.Method = null!;
            }

            public CommandPost CommandPost { get; private set; }

            public CommandFunc Method { get; private set; }
        }

        public class Command
        {
            public Command(CommandType type, int id, object? message)
            {
                this.Type = type;
                this.Id = id;
                this.Message = message;
            }

            public CommandType Type { get; internal set; }

            public int Id { get; }

            public object? Message { get; }

            public object? Response { get; set; }
        }

        public CommandPost(ThreadCoreBase parent, int millisecondInterval = 10)
        {
            lock (cs)
            {
                this.MillisecondInterval = millisecondInterval;
                this.Core = new(parent, this.MainLoop);
                this.Core.Thread.Priority = ThreadPriority.AboveNormal;
            }
        }

        /// <summary>
        /// Open a channel to receive a message.
        /// </summary>
        public Channel Open(CommandFunc method)
        {
            return new Channel(this, method);
        }

        public void Send<TMessage>(int id, TMessage message)
        {
            var m = new Command(CommandType.CommandAndForget, id, TinyhandSerializer.Clone(message));
            this.concurrentQueue.Enqueue(m);
            this.commandAdded.Set();
        }

        public TResult? SendTwoWay<TMessage, TResult>(int id, TMessage message, int millisecondTimeout = 100)
        {
            if (millisecondTimeout < 0 || millisecondTimeout > MaxMillisecondTimeout)
            {
                millisecondTimeout = MaxMillisecondTimeout;
            }

            var m = new Command(CommandType.RequireResponse, id, TinyhandSerializer.Clone(message));
            this.concurrentQueue.Enqueue(m);
            this.commandAdded.Set();

            try
            {
                while (true)
                {
                    this.commandResponded.Wait(this.MillisecondInterval, this.Core.CancellationToken);
                    if (m.Type == CommandType.Responded)
                    {
                        this.commandResponded.Reset();
                        return (TResult)m.Response!;
                    }
                }
            }
            catch
            {
                return default;
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
                    this.commandAdded.Wait(MillisecondInterval, core.CancellationToken);
                }
                catch
                {
                    break;
                }

                this.commandAdded.Reset();
                var method = this.primaryChannel?.Method;
                while (this.concurrentQueue.TryDequeue(out var command))
                {
                    if (method != null)
                    {
                        var type = command.Type;
                        method(command);
                        if (type == CommandType.RequireResponse)
                        {
                            command.Type = CommandType.Responded;
                        }

                        this.commandResponded.Set();
                    }
                }
            }
        }

        private object cs = new();
        private ManualResetEventSlim commandAdded = new(false);
        private ManualResetEventSlim commandResponded = new(false);
        private ConcurrentQueue<Command> concurrentQueue = new();
        private Channel? primaryChannel;

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls.

        /// <summary>
        /// Finalizes an instance of the <see cref="CommandPost"/> class.
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
