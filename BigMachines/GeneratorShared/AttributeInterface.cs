// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using BigMachines.Control;
using Tinyhand;

namespace BigMachines;

/// <summary>
/// Specifies how a machine is managed.
/// </summary>
public enum MachineControlKind
{
    /// <summary>
    /// Selects <see cref="SingleMachineControl{TMachine, TInterface}"/> for <see cref="Machine"/> or <see cref="UnorderedMachineControl{TIdentifier, TMachine, TInterface}"/> for <see cref="Machine{TIdentifier}"/>.
    /// </summary>
    Default,

    /// <summary>
    /// Manages a single machine (<see cref="SingleMachineControl{TMachine, TInterface}"/>).
    /// </summary>
    Single,

    /// <summary>
    /// Manages identified machines without ordering guarantees.
    /// </summary>
    Unordered,

    /// <summary>
    /// Manages identified machines through a sequential queue.
    /// </summary>
    Sequential,
}

/// <summary>
/// Marks a partial class as a generated big-machine root.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BigMachineObjectAttribute : Attribute
{
    public BigMachineObjectAttribute()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether to include all non-private machines in the assembly.
    /// </summary>
    public bool Inclusive { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to request recursive-call detection.
    /// </summary>
    /// <remarks>This option is currently reserved and does not enable generated checks.</remarks>
    public bool RecursiveDetection { get; set; } = false;
}

/// <summary>
/// Adds a machine type to a generated big-machine root.
/// </summary>
/// <typeparam name="TMachine">The machine type to add.</typeparam>
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
    /// Gets or sets a value indicating whether to exclude the machine control from persistence.
    /// </summary>
    public bool Volatile { get; set; }
}

/// <summary>
/// Marks a partial <see cref="Machine"/>-derived class for source generation.
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
    /// Gets or sets the control used to manage the machine.
    /// </summary>
    public MachineControlKind Control { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="TinyhandSerializer.ServiceProvider"/> creates machine instances.
    /// </summary>
    public bool UseServiceProvider { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether a single machine is created when its big-machine root starts.
    /// </summary>
    public bool StartByDefault { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of dedicated workers for a sequential control.
    /// </summary>
    public int NumberOfTasks { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether the machine is excluded from automatic root registration.
    /// </summary>
    public bool Private { get; set; } = false;
}

#pragma warning disable SA1629

/// <summary>
/// Marks a machine method as a state handler.
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
/// Marks a machine method as a generated asynchronous command endpoint.
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
    /// Gets or sets a value indicating whether the command holds the machine semaphore while executing.
    /// </summary>
    public bool WithLock { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to generate an extension that invokes the command on all matching machines.
    /// </summary>
    public bool All { get; set; } = false;
}

/// <summary>
/// Represents the result of a state method.
/// </summary>
public enum StateResult
{
    /// <summary>
    /// Keeps the machine available for future execution.
    /// </summary>
    Continue,

    /// <summary>
    /// Terminates the machine after the state method returns.
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

/// <summary>
/// Combines a command status with its response value.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public readonly struct CommandResult<TResponse>
{
    public CommandResult(CommandResult result, TResponse response)
    {
        this.Result = result;
        this.Resnpose = response;
    }

    public CommandResult(TResponse response)
    {
        this.Result = CommandResult.Success;
        this.Resnpose = response;
    }

    public readonly CommandResult Result;
    public readonly TResponse Resnpose;

    /// <summary>
    /// Gets the response returned by the command.
    /// </summary>
    public TResponse Response => this.Resnpose;
}

/// <summary>
/// Associates a machine identifier with a command status.
/// </summary>
/// <typeparam name="TIdentifier">The machine identifier type.</typeparam>
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

/// <summary>
/// Associates a machine identifier with a command response.
/// </summary>
/// <typeparam name="TIdentifier">The machine identifier type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public readonly struct IdentifierAndCommandResult<TIdentifier, TResponse>
    where TIdentifier : notnull
{
    public IdentifierAndCommandResult(TIdentifier identifier, CommandResult<TResponse> result)
    {
        this.Identifier = identifier;
        this.Result = result;
    }

    public IdentifierAndCommandResult(TIdentifier identifier, TResponse response)
    {
        this.Identifier = identifier;
        this.Result = new(response);
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
    /// The machine is executing a state method.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The machine is paused.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// The machine is terminated.
    /// </summary>
    Terminated = 4,
}

/// <summary>
/// Represents the trigger of the state method.
/// </summary>
public enum RunType
{
    /// <summary>
    /// No execution trigger is active.
    /// </summary>
    NotRunning,

    /// <summary>
    /// The machine was invoked by <see cref="Machine.ManMachineInterface.RunAsync"/>.
    /// </summary>
    Manual,

    /// <summary>
    /// The machine was invoked by its timer.
    /// </summary>
    Timer,

    /// <summary>
    /// Reserved for continuous execution.
    /// </summary>
    Continuous,
}

/// <summary>
/// Provides context for a state-method invocation.
/// </summary>
public struct StateParameter
{
    /*/// <summary>
    /// Initializes a new instance of the <see cref="StateParameter"/> struct.
    /// </summary>
    /// <param name="type">The execution trigger.</param>
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
    /// Gets the execution trigger.
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
/// Describes the outcome of a requested state transition.
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

/// <summary>
/// Configures BigMachines source generation for the containing compilation.
/// </summary>
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
