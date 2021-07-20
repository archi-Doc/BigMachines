﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
        /// <param name="typeId">Machine Type id (unique identifier for serialization).</param>
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
        /// <param name="id">The identifier used for serialization.<br/>
        /// 0: Default state method.<br/>
        /// Id can be a random number, but it must be unique.</param>
        public StateMethodAttribute(int id)
        {
            this.Id = id;
        }

        /// <summary>
        /// Gets an identifier used for serialization.<br/>
        /// 0: Default state method.<br/>
        /// Id can be a random number, but it must be unique.
        /// </summary>
        public int Id { get; }
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