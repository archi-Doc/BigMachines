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
        Manual,
        Internal,
    }

    public struct RunParameter
    {
        public RunParameter(RunType type)
        {
            this.RunType = type;
        }

        public RunType RunType { get; }
    }
}
