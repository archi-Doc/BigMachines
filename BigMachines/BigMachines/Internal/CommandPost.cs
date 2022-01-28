// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        /// One-way command.
        /// </summary>
        Command,

        /// <summary>
        /// Two-way command which requires a response
        /// </summary>
        CommandTwoWay,

        /// <summary>
        /// Exception. Used internally.
        /// </summary>
        Exception,

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

    public static bool IsTwoWay(CommandType commandType) => commandType switch
    {
        CommandType.CommandTwoWay => true,
        CommandType.StateTwoWay => true,
        CommandType.RunTwoWay => true,
        _ => false,
    };

    /// <summary>
    /// Defines the type of delegate used to receive and process commands.
    /// </summary>
    /// <param name="command">Command.</param>
    /// <param name="commandList">Command list.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public delegate Task CommandDelegate(Command? command, List<Command>? commandList);

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
        /// <param name="message">Message.</param>
        public Command(BigMachine<TIdentifier> bigMachine, IMachineGroup<TIdentifier> group, CommandType type, TIdentifier identifier, object? message)
        {
            LoopChecker? checker;
            if (bigMachine.EnableLoopChecker)
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
            this.Message = message;
            this.LoopChecker = checker;
        }

        internal IMachineGroup<TIdentifier> Group { get; }

        public CommandType Type { get; internal set; }

        public TIdentifier Identifier { get; }

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

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandPost{TIdentifier}"/> class.
    /// </summary>
    /// <param name="bigMachine">BigMachine.</param>
    /// <param name="method">CommandDelegate.</param>
    public CommandPost(BigMachine<TIdentifier> bigMachine, CommandDelegate method)
    {
        this.BigMachine = bigMachine;
        this.commandDelegate = method;
    }

    /// <summary>
    /// Send a message to the receiver.<br/>
    /// Caution! TMessage must be serializable by Tinyhand because the message will be cloned and passed to the receiver.
    /// </summary>
    /// <typeparam name="TMessage">The type of a message.</typeparam>
    /// <param name="group">The channel to receive message.</param>
    /// <param name="commandType">CommandType of a message.</param>
    /// <param name="identifier">The identifier of a message.</param>
    /// <param name="message">The message to send.<br/>Must be serializable by Tinyhand because the message will be cloned and passed to the receiver.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    public Task SendAsync<TMessage>(IMachineGroup<TIdentifier> group, CommandType commandType, TIdentifier identifier, TMessage message)
    {
        var c = new Command(this.BigMachine, group, commandType, identifier, TinyhandSerializer.Clone(message));
        return this.commandDelegate(c, null);
    }

    public Task SendGroupAsync<TMessage>(CommandType commandType, IMachineGroup<TIdentifier> group, IEnumerable<TIdentifier> identifiers, TMessage message) => this.SendGroupsAsync(commandType, Enumerable.Repeat(group, identifiers.Count()), identifiers, message);

    public Task SendGroupsAsync<TMessage>(CommandType commandType, IEnumerable<IMachineGroup<TIdentifier>> groups, IEnumerable<TIdentifier> identifiers, TMessage message)
    {
        var messageClone = TinyhandSerializer.Clone(message);
        var list = new List<Command>();
        var g = groups.GetEnumerator();
        var i = identifiers.GetEnumerator();
        while (g.MoveNext() && i.MoveNext())
        {
            var c = new Command(this.BigMachine, g.Current, commandType, i.Current, messageClone);
            list.Add(c);
        }

        return this.commandDelegate(null, list);
    }

    public async Task<TResult?> SendAndReceiveAsync<TMessage, TResult>(IMachineGroup<TIdentifier> group, CommandType commandType, TIdentifier identifier, TMessage message)
    {
        var c = new Command(this.BigMachine, group, commandType, identifier, TinyhandSerializer.Clone(message));
        var task = this.commandDelegate(c, null);

        if (IsTwoWay(commandType))
        {
            await task;
            if (c.Response is TResult result)
            {// Valid result
                return result;
            }
        }

        return default;
    }

    public TResult? SendTwoWay<TMessage, TResult>(IMachineGroup<TIdentifier> group, CommandType commandType, TIdentifier identifier, TMessage message, int millisecondTimeout = 100)
    {
        if (millisecondTimeout < 0 || millisecondTimeout > MaxMillisecondTimeout)
        {
            millisecondTimeout = MaxMillisecondTimeout;
        }

        var c = new Command(this.BigMachine, group, commandType, identifier, TinyhandSerializer.Clone(message));
        var task = this.commandDelegate(c, null);

        if (commandType == CommandType.CommandTwoWay ||
            commandType == CommandType.StateTwoWay ||
            commandType == CommandType.RunTwoWay)
        {// TwoWay
            try
            {
                if (task.Wait(millisecondTimeout, this.BigMachine.Core.CancellationToken))
                {// Completed
                    if (c.Response is TResult result)
                    {// Valid result
                        return result;
                    }
                }
            }
            catch
            {
            }
        }

        return default;
    }

    public KeyValuePair<TIdentifier, TResult?>[] SendGroupTwoWay<TMessage, TResult>(CommandType commandType, IMachineGroup<TIdentifier> group, IEnumerable<TIdentifier> identifiers, TMessage message, int millisecondTimeout = 100) => this.SendGroupsTwoWay<TMessage, TResult>(commandType, Enumerable.Repeat(group, identifiers.Count()), identifiers, message, millisecondTimeout);

    public KeyValuePair<TIdentifier, TResult?>[] SendGroupsTwoWay<TMessage, TResult>(CommandType commandType, IEnumerable<IMachineGroup<TIdentifier>> groups, IEnumerable<TIdentifier> identifiers, TMessage message, int millisecondTimeout = 100)
    {
        if (millisecondTimeout < 0 || millisecondTimeout > MaxMillisecondTimeout)
        {
            millisecondTimeout = MaxMillisecondTimeout;
        }

        var messageClone = TinyhandSerializer.Clone(message);
        var list = new List<Command>();
        var g = groups.GetEnumerator();
        var i = identifiers.GetEnumerator();
        while (g.MoveNext() && i.MoveNext())
        {
            var c = new Command(this.BigMachine, g.Current, commandType, i.Current, messageClone);
            list.Add(c);
        }

        var task = this.commandDelegate(null, list);

        if (commandType == CommandType.CommandTwoWay ||
            commandType == CommandType.StateTwoWay ||
            commandType == CommandType.RunTwoWay)
        {// TwoWay
            try
            {
                if (task.Wait(millisecondTimeout, this.BigMachine.Core.CancellationToken))
                {// Completed
                    var array = new KeyValuePair<TIdentifier, TResult?>[list.Count];
                    var n = 0;
                    foreach (var x in list)
                    {
                        if (x.Response is TResult result)
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
            }
            catch
            {
            }
        }

        return Array.Empty<KeyValuePair<TIdentifier, TResult?>>();
    }

    public BigMachine<TIdentifier> BigMachine { get; }

    private CommandDelegate commandDelegate;
}
