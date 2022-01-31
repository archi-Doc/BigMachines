// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tinyhand;

#pragma warning disable SA1202

namespace BigMachines;

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
        /// Issue a command to the machine.
        /// </summary>
        Command,

        /// <summary>
        /// Change the state of the machine.
        /// </summary>
        ChangeState,

        /// <summary>
        /// Run the machine.
        /// </summary>
        Run,

        /// <summary>
        /// Report an exception. Used internally.
        /// </summary>
        Exception,
    }

    public enum BatchCommandType
    {
        /// <summary>
        /// Serialize machines.
        /// </summary>
        Serialize,
    }

    /// <summary>
    /// Defines the type of delegate used to receive and process commands.
    /// </summary>
    /// <param name="command">Command.</param>
    /// <param name="commandList">Command list.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public delegate Task CommandDelegate(Command? command, List<Command>? commandList);

    public delegate Task ProcessBatchCommandDelegate(BatchCommand command);

    /// <summary>
    /// Command class contains command information.
    /// </summary>
    public class Command
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// </summary>
        /// <param name="bigMachine">BigMachine.</param>
        /// <param name="group">Machine group.</param>
        /// <param name="type">CommandType.</param>
        /// <param name="identifier">Identifier.</param>
        /// <param name="data">Data.</param>
        /// <param name="message">Message.</param>
        public Command(BigMachine<TIdentifier> bigMachine, IMachineGroup<TIdentifier> group, CommandType type, TIdentifier identifier, int data, object? message)
        {
            LoopChecker? checker;
            if (bigMachine.LoopCheckerMode != LoopCheckerMode.Disabled)
            {// LoopChecker enabled.
                checker = LoopChecker.AsyncLocalInstance.Value;
                if (checker == null)
                {
                    checker = new();
                    LoopChecker.AsyncLocalInstance.Value = checker;
                }
            }
            else
            {
                checker = null;
            }

            this.Group = group;
            this.Type = type;
            this.Identifier = identifier;
            this.Data = data;
            this.Message = message;
            this.LoopChecker = checker;
        }

        internal IMachineGroup<TIdentifier> Group { get; }

        public CommandType Type { get; internal set; }

        public TIdentifier Identifier { get; }

        public int Data { get; set; }

        public object? Message { get; }

        public object? Response { get; set; }

        // public Exception? GetException() => this.Type == CommandType.Exception ? (this.Response as Exception) : null;

        internal LoopChecker? LoopChecker { get; }

        /*internal void SetException(Exception ex)
        {
            this.Type = CommandType.Exception;
            this.Response = ex;
        }*/
    }

    public class BatchCommand
    {
        public BatchCommand(BatchCommandType type, IMachineGroup<TIdentifier>? group, TIdentifier? identifier, object? option)
        {
            this.Type = type;
            this.Group = group;
            this.Identifier = identifier;
            this.Option = option;
        }

        public BatchCommandType Type { get; internal set; }

        internal IMachineGroup<TIdentifier>? Group { get; }

        internal TIdentifier? Identifier { get; }

        public object? Option { get; }

        public object? Response { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandPost{TIdentifier}"/> class.
    /// </summary>
    /// <param name="bigMachine">BigMachine.</param>
    /// <param name="method">CommandDelegate.</param>
    /// <param name="processBatchCommand">Delegate for batch command.</param>
    public CommandPost(BigMachine<TIdentifier> bigMachine, CommandDelegate method, ProcessBatchCommandDelegate processBatchCommand)
    {
        this.BigMachine = bigMachine;
        this.commandDelegate = method;
        this.processBatchCommand = processBatchCommand;
    }

    /// <summary>
    /// Send a message to the receiver.<br/>
    /// Caution! TMessage must be serializable by Tinyhand because the message will be cloned and passed to the receiver.
    /// </summary>
    /// <typeparam name="TMessage">The type of a message.</typeparam>
    /// <param name="group">Machine group.</param>
    /// <param name="commandType">CommandType of the message.</param>
    /// <param name="identifier">The identifier of the message.</param>
    /// <param name="data"><see langword="int"/> type data included in the message.</param>
    /// <param name="message">The message to send.<br/>Must be serializable by Tinyhand because the message will be cloned and passed to the receiver.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public Task SendAsync<TMessage>(IMachineGroup<TIdentifier> group, CommandType commandType, TIdentifier identifier, int data, TMessage message)
    {
        var c = new Command(this.BigMachine, group, commandType, identifier, data, TinyhandSerializer.Clone(message));
        return this.commandDelegate(c, null);
    }

    public Task SendAsync(IMachineGroup<TIdentifier> group, CommandType commandType, TIdentifier identifier, int data)
    {
        var c = new Command(this.BigMachine, group, commandType, identifier, data, null);
        return this.commandDelegate(c, null);
    }

    public Task SendGroupAsync<TMessage>(IMachineGroup<TIdentifier> group, CommandType commandType, IEnumerable<TIdentifier> identifiers, int data, TMessage message) => this.SendGroupsAsync(Enumerable.Repeat(group, identifiers.Count()), commandType, identifiers, data, message);

    public Task SendGroupsAsync<TMessage>(IEnumerable<IMachineGroup<TIdentifier>> groups, CommandType commandType, IEnumerable<TIdentifier> identifiers, int data, TMessage message)
    {
        var messageClone = TinyhandSerializer.Clone(message);
        var list = new List<Command>();
        var g = groups.GetEnumerator();
        var i = identifiers.GetEnumerator();
        while (g.MoveNext() && i.MoveNext())
        {
            var c = new Command(this.BigMachine, g.Current, commandType, i.Current, data, messageClone);
            list.Add(c);
        }

        return this.commandDelegate(null, list);
    }

    public async Task<TResponse?> SendAndReceiveAsync<TMessage, TResponse>(IMachineGroup<TIdentifier> group, CommandType commandType, TIdentifier identifier, int data, TMessage message)
    {
        var c = new Command(this.BigMachine, group, commandType, identifier, data, TinyhandSerializer.Clone(message));

        await this.commandDelegate(c, null).ConfigureAwait(false);
        if (c.Response is TResponse result)
        {// Valid result
            return result;
        }

        return default;
    }

    public async Task<TResponse?> SendAndReceiveAsync<TResponse>(IMachineGroup<TIdentifier> group, CommandType commandType, TIdentifier identifier, int data)
    {
        var c = new Command(this.BigMachine, group, commandType, identifier, data, null);

        await this.commandDelegate(c, null).ConfigureAwait(false);
        if (c.Response is TResponse result)
        {// Valid result
            return result;
        }

        return default;
    }

    public Task<KeyValuePair<TIdentifier, TResponse?>[]> SendAndReceiveGroupAsync<TMessage, TResponse>(IMachineGroup<TIdentifier> group, CommandType commandType, IEnumerable<TIdentifier> identifiers, int data, TMessage message) => this.SendAndReceiveGroupsAsync<TMessage, TResponse>(Enumerable.Repeat(group, identifiers.Count()), commandType, identifiers, data, message);

    public async Task<KeyValuePair<TIdentifier, TResponse?>[]> SendAndReceiveGroupsAsync<TMessage, TResponse>(IEnumerable<IMachineGroup<TIdentifier>> groups, CommandType commandType, IEnumerable<TIdentifier> identifiers, int data, TMessage message)
    {
        var messageClone = TinyhandSerializer.Clone(message);
        var list = new List<Command>();
        var g = groups.GetEnumerator();
        var i = identifiers.GetEnumerator();
        while (g.MoveNext() && i.MoveNext())
        {
            var c = new Command(this.BigMachine, g.Current, commandType, i.Current, data, messageClone);
            list.Add(c);
        }

        await this.commandDelegate(null, list).ConfigureAwait(false);

        var array = new KeyValuePair<TIdentifier, TResponse?>[list.Count];
        var n = 0;
        foreach (var x in list)
        {
            if (x.Response is TResponse result)
            {// Valid result
                array[n++] = new(x.Identifier, result);
            }
        }

        if (array.Length != n)
        {
            Array.Resize(ref array, n);
        }

        return array;
    }

    public async Task<TResponse?> BatchSingleAsync<TResponse>(BatchCommandType type, IMachineGroup<TIdentifier> group, TIdentifier identifier, object? option)
    {
        var c = new BatchCommand(type, group, identifier, option);

        await this.processBatchCommand(c).ConfigureAwait(false);
        if (c.Response is TResponse result)
        {// Valid result
            return result;
        }

        return default;
    }

    public async Task<TResponse?> BatchGroupAsync<TResponse>(BatchCommandType type, IMachineGroup<TIdentifier> group, object? option)
    {
        var c = new BatchCommand(type, group, default, option);

        await this.processBatchCommand(c).ConfigureAwait(false);
        if (c.Response is TResponse result)
        {// Valid result
            return result;
        }

        return default;
    }

    public async Task<TResponse?> BatchAllAsync<TResponse>(BatchCommandType type, object? option)
    {
        var c = new BatchCommand(type, null, default, option);

        await this.processBatchCommand(c).ConfigureAwait(false);
        if (c.Response is TResponse result)
        {// Valid result
            return result;
        }

        return default;
    }

    public BigMachine<TIdentifier> BigMachine { get; }

    private CommandDelegate commandDelegate;
    private ProcessBatchCommandDelegate processBatchCommand;
}
