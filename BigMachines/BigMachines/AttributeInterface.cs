// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class StateMachineAttribute : Attribute
    {
        public StateMachineAttribute(int typeId)
        {
            this.MachineTypeId = typeId;
        }

        public int MachineTypeId { get; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class StateMethodAttribute : Attribute
    {
        public StateMethodAttribute()
        {
        }
    }

    public enum StateInput
    {
        CanEnter,
        Run,
        CanExit,
    }

    public enum StateResult
    {
        Continue,
        Terminate,
        Deny,
    }

    public enum MachineStatus
    {
        Running,
        Paused,
        Terminated,
    }

    public enum RunType
    {
        RunManual,
        RunTimer,
        CanEnter,
        CanExit,
    }

    public struct StateParameter
    {
        public StateParameter(RunType type, object? message)
        {
            this.RunType = type;
            this.Message = message;
        }

        public RunType RunType { get; }

        public object? Message { get; }
    }
}
