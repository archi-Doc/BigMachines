// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using BigMachines.Control;
using Tinyhand;

namespace BigMachines;

/// <summary>
/// Represents the type of MachineControl.
/// </summary>
public enum MachineControlKind
{
    /// <summary>
    /// Assigns <see cref="SingleMachineControl{TMachine, TInterface}"/> if the machine is derived from <see cref="Machine"/>, or <see cref="UnorderedMachineControl{TIdentifier, TMachine, TInterface}"/> if the machine is derived from <see cref="Machine{TIdentifier}"/>.
    /// </summary>
    Default,

    /// <summary>
    /// Manages a single machine (<see cref="SingleMachineControl{TMachine, TInterface}"/>).
    /// </summary>
    Single,

    /// <summary>
    /// Manage multiple machines with identifiers (<see cref="UnorderedMachineControl{TIdentifier, TMachine, TInterface}"/>.
    /// </summary>
    Unordered,

    /// <summary>
    /// Manage multiple machines and run them sequentially (<see cref="SequentialMachineControl{TIdentifier, TMachine, TInterface}"/>.
    /// </summary>
    Sequential,
}

/// <summary>
/// Add the attribute to the target class to create a big machine.<br/>
/// The target class must be an empty partial class and must not have a default constructor.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BigMachineObjectAttribute : Attribute
{
    public BigMachineObjectAttribute()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether or not to include all machines contained in this assembly [default is <see langword="false"/>].
    /// </summary>
    public bool Comprehensive { get; set; } = false;
}

/// <summary>
/// Add a machine to the BigMachine.
/// </summary>
/// <typeparam name="TMachine">The type of the machine.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AddMachineAttribute<TMachine> : Attribute
where TMachine : Machine
{
    public AddMachineAttribute()
    {
    }

    /// <summary>
    /// Gets or sets the name to identify the machine.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether or not to make the machine volatile and exclude it during data persistence. [default is <see langword="false"/>].
    /// </summary>
    public bool Volatile { get; set; }
}

/// <summary>
/// Add the attribute to the target class to create a machine.<br/>
/// The class must be a partial class and inherit from either <see cref="Machine"/> or <see cref="Machine{TIdentifier}"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class MachineObjectAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MachineObjectAttribute"/> class.
    /// </summary>
    public MachineObjectAttribute()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating which <see cref="BigMachines.Control.MachineControl"/> is assigned to the machine.
    /// </summary>
    public MachineControlKind Control { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not to use <seealso cref="IServiceProvider"/> to create an instance [default is <see langword="false"/>]. Set <see cref="TinyhandSerializer.ServiceProvider"/>.
    /// </summary>
    public bool UseServiceProvider { get; set; } = false;

    /*
    /// <summary>
    /// Gets the identifier for the Machine.<br/>
    /// You can set a random number, but it needs to be unique.<br/>
    /// If you specify 0, the hash of the class name is used.
    /// </summary>
    public uint MachineId { get; }*/

    /// <summary>
    /// Gets or sets a value indicating whether or not the machine is continuous machine (runs continuously and uses one thread).
    /// </summary>
    public bool Continuous { get; set; }
}

#pragma warning disable SA1629

/// <summary>
/// Adds a state method to the machine.<br/>
/// The format of the method is as follows: <br/><br/>
/// <see langword="protected"/> <see cref="StateResult"/> ExampleState(<see cref="StateParameter"/> parameter)<br/>
///  => <see cref="StateResult.Continue"/>;<br/><br/>
///  <see langword="protected"/> <see langword="async"/> Task&lt;<see cref="StateResult"/>&gt; ExampleState(<see cref="StateParameter"/> parameter)<br/>
///  => <see cref="StateResult.Terminate"/>;
/// </summary>
#pragma warning restore SA1629
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class StateMethodAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StateMethodAttribute"/> class.
    /// </summary>
    /// <param name="stateId">The identifier for the state method.<br/>
    /// State method with an id of 0 is the default (first to be executed) state method and is required to be present in every machine.<br/>
    /// You can set a random number, but it needs to be unique.<br/>
    /// If you specify <see cref="uint.MaxValue"/>, the hash of the method name is used.</param>
    public StateMethodAttribute(uint stateId = uint.MaxValue)
    {
        this.StateId = stateId;
    }

    /// <summary>
    /// Gets the identifier for the state method.<br/>
    /// State method with an id of 0 is the default (first to be executed) state method and is required to be present in every machine.<br/>
    /// You can set a random number, but it needs to be unique.<br/>
    /// If you specify <see cref="uint.MaxValue"/>, the hash of the method name is used.
    /// </summary>
    public uint StateId { get; }
}

#pragma warning disable SA1629
/// <summary>
/// Adds a command method to the machine.<br/>
/// Command methods are executed asynchronously.<br/>
/// The format of the method is as follows: <br/><br/>
/// <see langword="protected"/> <see cref="CommandResult"/> ExampleCommand(any param)<br/>
/// => <see cref="CommandResult.Success"/>;<br/><br/>
/// <see langword="protected"/> <see cref="CommandResult{TResponse}"/> ExampleCommand(any param)<br/>
/// => new(<see cref="CommandResult.Success"/>, response);
/// </summary>
#pragma warning restore SA1629
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class CommandMethodAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandMethodAttribute"/> class.
    /// </summary>
    public CommandMethodAttribute()
    {
        // this.CommandId = commandId;
    }

    /*
    /// <summary>
    /// Gets the identifier for the command method.<br/>
    /// You can set a random number, but it needs to be unique.<br/>
    /// If you specify <see cref="uint.MaxValue"/>, the hash of the method name is used.
    /// </summary>
    public uint CommandId { get; }*/

    /// <summary>
    /// Gets or sets a value indicating whether the command method executes with locking the machine [the default is <see langword="true"/>].
    /// </summary>
    public bool WithLock { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not to add an extension method for issuing commands to all machines [the default is <see langword="false"/>].
    /// </summary>
    public bool All { get; set; } = false;
}

/// <summary>
/// Represents the result of a state method.
/// </summary>
public enum StateResult
{
    /// <summary>
    /// The machine will continue to run.
    /// </summary>
    Continue,

    /// <summary>
    /// The machine is going to be terminated.
    /// </summary>
    Terminate,
}

/// <summary>
/// Represents the result of a command method.
/// </summary>
public enum CommandResult
{
    /// <summary>
    /// The command was successfully executed.
    /// </summary>
    Success,

    /// <summary>
    /// The command execution failed.
    /// </summary>
    Failure,

    /// <summary>
    /// The command was not executed because the machine is terminated.
    /// </summary>
    Terminated,
}

public readonly struct CommandResult<TResponse>
{
    public CommandResult(CommandResult result, TResponse response)
    {
        this.Result = result;
        this.Resnpose = response;
    }

    public readonly CommandResult Result;
    public readonly TResponse Resnpose;
}

public readonly struct IdentifierAndCommandResult<TIdentifier>
    where TIdentifier : notnull
{
    public IdentifierAndCommandResult(TIdentifier identifier, CommandResult result)
    {
        this.Identifier = identifier;
        this.Result = result;
    }

    public readonly TIdentifier Identifier;
    public readonly CommandResult Result;
}

public readonly struct IdentifierAndCommandResult<TIdentifier, TResponse>
    where TIdentifier : notnull
{
    public IdentifierAndCommandResult(TIdentifier identifier, CommandResult<TResponse> result)
    {
        this.Identifier = identifier;
        this.Result = result;
    }

    public readonly TIdentifier Identifier;
    public readonly CommandResult<TResponse> Result;
}

/// <summary>
/// Represents the operational state of the machine.
/// </summary>
[Flags]
public enum OperationalFlag
{
    /// <summary>
    /// Machine is running (in state methods).
    /// </summary>
    Running = 1,

    /// <summary>
    /// Machine is paused.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Machine is terminated.
    /// </summary>
    Terminated = 4,
}

/// <summary>
/// Represents the trigger of the state method.
/// </summary>
public enum RunType
{
    /// <summary>
    /// Machine is not running.
    /// </summary>
    NotRunning,

    /// <summary>
    /// Machine is run by <see cref="Machine.ManMachineInterface.RunAsync"/> method.
    /// </summary>
    Manual,

    /// <summary>
    /// Machine is run by interval timer.
    /// </summary>
    Timer,

    /// <summary>
    /// Machine is run by. // tempcode.
    /// </summary>
    Continuous,
}

/// <summary>
/// Input parameter of a state method.
/// </summary>
public struct StateParameter
{
    /*/// <summary>
    /// Initializes a new instance of the <see cref="StateParameter"/> struct.
    /// </summary>
    /// <param name="type">RunType.</param>
    /// <param name="message">Message.</param>
    public StateParameter(RunType type, object? message)
    {
        this.RunType = type;
        this.Message = message;
    }*/

    /// <summary>
    /// Initializes a new instance of the <see cref="StateParameter"/> struct.
    /// </summary>
    /// <param name="type">RunType.</param>
    public StateParameter(RunType type)
    {
        this.RunType = type;
        // this.CancellationToken = calcellationToken;
        // this.Message = null;
    }

    /// <summary>
    /// Gets a RunType.
    /// </summary>
    public RunType RunType { get; }

    /*/// <summary>
    /// Gets a CancellationToken.
    /// </summary>
    public CancellationToken CancellationToken { get; }*/

    /*/// <summary>
    /// Gets a message.
    /// </summary>
    public object? Message { get; }*/
}

/// <summary>
/// Represents the result of <see cref="Machine{TIdentifier}"/>.ChangeState() method.
/// </summary>
public enum ChangeStateResult
{
    /// <summary>
    /// Successfully changed state.
    /// </summary>
    Success,

    /// <summary>
    /// Could not exit the current state.
    /// </summary>
    UnableToExit,

    /// <summary>
    /// Could not move to the next state.
    /// </summary>
    UnableToEnter,

    /// <summary>
    /// The machine is terminated.
    /// </summary>
    Terminated,
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class BigMachinesGeneratorOptionAttribute : Attribute
{
    public bool AttachDebugger { get; set; } = false;

    public bool GenerateToFile { get; set; } = false;

    public string? CustomNamespace { get; set; }

    public bool UseModuleInitializer { get; set; } = true;

    public BigMachinesGeneratorOptionAttribute()
    {
    }
}
