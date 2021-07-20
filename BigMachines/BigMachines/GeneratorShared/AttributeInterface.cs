// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines
{
    /// <summary>
    /// Enables the state machine features for the target class.<br/>
    /// Class must be a partial type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class StateMachineAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateMachineAttribute"/> class.
        /// </summary>
        /// <param name="typeId">Type id (unique identifier for serialization).</param>
        public StateMachineAttribute(int typeId)
        {
            this.MachineTypeId = typeId;
        }

        /// <summary>
        /// Gets a type id (unique identifier for serialization) of the machine.
        /// </summary>
        public int MachineTypeId { get; }
    }

#pragma warning disable SA1629
    /// <summary>
    /// Adds the target method to the state machine.<br/>
    /// The format of a method must be: <br/>
    /// <see langword="protected"/> <see cref="StateResult"/> SampleState(<see cref="StateParameter"/> parameter)<br/>
    /// { <see langword="return"/> <see cref="StateResult.Continue"/>; }
    /// </summary>
#pragma warning restore SA1629
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class StateMethodAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateMethodAttribute"/> class.
        /// </summary>
        /// <param name="default">If set to true, the state method will be the default.</param>
        public StateMethodAttribute(bool @default = false)
        {
            this.Default = @default;
        }

        /// <summary>
        /// Gets a value indicating whether or not the state machine is default [the default is false].
        /// </summary>
        public bool Default { get; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to check for the state change [the default is false].
        /// </summary>
        public bool CheckStateChange { get; set; } = false;

        /// <summary>
        /// Gets or sets an identifier which is used for serialization.<br/>
        /// Id will be set as GetHashCode() of the method name by default [the default is int.MinValue].
        /// </summary>
        public int Id { get; set; } = int.MinValue;
    }

    /// <summary>
    /// Represents the result of a state method.
    /// </summary>
    public enum StateResult
    {
        /// <summary>
        /// Informs that the machine will continue to run.
        /// </summary>
        Continue,

        /// <summary>
        /// Informs that the machine is going to be terminated.
        /// </summary>
        Terminate,

        /// <summary>
        /// Informs that changing to this state or from this state is not possible.<br/>
        /// Available if <see cref="RunType"/> is <see cref="RunType.CanEnter"/> or <see cref="RunType.CanExit"/>.
        /// </summary>
        Deny,
    }

    /// <summary>
    /// Represents the state of the machine.
    /// </summary>
    public enum MachineStatus
    {
        /// <summary>
        /// Machine is running.
        /// </summary>
        Running,

        /// <summary>
        /// Machine is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// Machine is terminated.
        /// </summary>
        Terminated,
    }

    /// <summary>
    /// Represents the trigger of the state method.
    /// </summary>
    public enum RunType
    {
        /// <summary>
        /// Machine is run by <see cref="ManMachineInterface{TIdentifier, TState}.Run"/> method.
        /// </summary>
        RunManual,

        /// <summary>
        /// Machine is run by interval timer.
        /// </summary>
        RunTimer,

        /// <summary>
        /// Asking if the machine can be changed to this state.
        /// </summary>
        CanEnter,

        /// <summary>
        /// Asking if the machine can be changed from this state.
        /// </summary>
        CanExit,
    }

    /// <summary>
    /// Input parameter for a state method.
    /// </summary>
    public struct StateParameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StateParameter"/> struct.
        /// </summary>
        /// <param name="type">RunType.</param>
        /// <param name="message">Message.</param>
        public StateParameter(RunType type, object? message)
        {
            this.RunType = type;
            this.Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateParameter"/> struct.
        /// </summary>
        /// <param name="type">RunType.</param>
        public StateParameter(RunType type)
        {
            this.RunType = type;
            this.Message = null;
        }

        /// <summary>
        /// Gets a RunType.
        /// </summary>
        public RunType RunType { get; }

        /// <summary>
        /// Gets a message.
        /// </summary>
        public object? Message { get; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class BigMachinesGeneratorOptionAttribute : Attribute
    {
        public bool AttachDebugger { get; set; } = false;

        public bool GenerateToFile { get; set; } = false;

        public string? CustomNamespace { get; set; }

        public BigMachinesGeneratorOptionAttribute()
        {
        }
    }
}
