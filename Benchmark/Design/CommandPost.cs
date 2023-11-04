// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using Tinyhand;

namespace BigMachines.Obsolete;

/*
/// <summary>
/// CommandPost is a class for dependency-free pub/sub service.<br/>
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
        ResponseCompleted,
    }

    public delegate void CommandFunc(Command command);

    /// <summary>
    /// Operation class to receive a message.<br/>
    /// You need to call <see cref="Operation.Dispose()"/> when the Operation is no longer necessary.
    /// </summary>
    public class Operation : IDisposable
    {
        public Operation(CommandPost commandPost, CommandFunc method)
        {
            this.CommandPost = commandPost;
            this.Method = method;
            lock (this.CommandPost.cs)
            {
                if (this.CommandPost.primaryOperation != null)
                {
                    throw new InvalidOperationException("Dispose the previous operation.");
                }

                this.CommandPost.primaryOperation = this;
            }
        }

        public void Dispose()
        {
            lock (this.CommandPost.cs)
            {
                this.CommandPost.primaryOperation = null;
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

    public CommandPost(int millisecondInterval = 5)
    {
        lock (cs)
        {
            this.MillisecondInterval = millisecondInterval;
            this.mainThread = new Thread(this.MainLoop);
            this.mainThread.Priority = ThreadPriority.AboveNormal;
            this.mainThread.Start(this.cancellationTokenSource.Token);
        }
    }

    /// <summary>
    /// Open a channel to receive a message.
    /// </summary>
    public Operation Open(CommandFunc method)
    {
        return new Operation(this, method);
    }

    public void Send<TMessage>(int id, TMessage message)
    {
        var m = new Command(CommandType.CommandAndForget, id, TinyhandSerializer.Clone(message));
        this.concurrentQueue.Enqueue(m);
        this.mainFlag = true;
    }

    public async Task<TResponse> SendTwoWay<TMessage, TResponse>(int id, TMessage message, int millisecondTimeout, CancellationToken cancellationToken = default)
    {
        if (millisecondTimeout < 0 || millisecondTimeout > MaxMillisecondTimeout)
        {
            millisecondTimeout = MaxMillisecondTimeout;
        }

        var m = new Command(CommandType.RequireResponse, id, TinyhandSerializer.Clone(message));
        this.concurrentQueue.Enqueue(m);
        this.mainFlag = true;

        int wait = 1;
        int total = 0;
        while (m.Type != CommandType.ResponseCompleted)
        {
            await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
            total += wait;
            wait++;

            if (total > millisecondTimeout)
            {// Timeout
                throw new OperationCanceledException();
            }
        }

        return (TResponse)m.Response!;
    }

    public int MillisecondInterval { get; }

    private void MainLoop(object? parameter)
    {
        var cancellationToken = (CancellationToken)parameter!;
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            else if (!this.mainFlag)
            {
                Thread.Sleep(this.MillisecondInterval);
                continue;
            }
            else
            {
                this.mainFlag = false;
            }

            var method = this.primaryOperation?.Method;
            while (this.concurrentQueue.TryDequeue(out var command))
            {
                if (method != null)
                {
                    var type = command.Type;
                    method(command);
                    if (type == CommandType.RequireResponse)
                    {
                        command.Type = CommandType.ResponseCompleted;
                    }
                }
            }
        }
    }

    private object cs = new();
    private CancellationTokenSource cancellationTokenSource = new();
    private Thread mainThread;
    private bool mainFlag;
    private Operation? primaryOperation;
    private ConcurrentQueue<Command> concurrentQueue = new();

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
                this.primaryOperation?.Dispose();
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
*/
