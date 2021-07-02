﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines
{
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
}
