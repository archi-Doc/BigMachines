﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1602

namespace BigMachines.Generator;

public enum MachineControlKind
{
    Default,
    Single,
    Unordered,
    Sequential,
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BigMachineObjectAttributeMock : Attribute
{
    public static readonly string SimpleName = "BigMachineObject";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = BigMachinesBody.BigMachineNamespace + "." + StandardName;

    public bool Inclusive { get; set; }

    public bool RecursiveDetection { get; set; }

    public static BigMachineObjectAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new BigMachineObjectAttributeMock();
        object? val;

        val = VisceralHelper.GetValue(-1, nameof(Inclusive), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Inclusive = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(RecursiveDetection), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.RecursiveDetection = (bool)val;
        }

        return attribute;
    }
}

[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = true, Inherited = false)]
public sealed class AddMachineAttributeMock : Attribute
{
    public static readonly string SimpleName = "AddMachine";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = BigMachinesBody.BigMachineNamespace + "." + StandardName;

    public AddMachineAttributeMock()
    {
    }

    public string Name { get; set; } = string.Empty;

    public bool Volatile { get; set; }

    internal Location? Location { get; set; }

    public static AddMachineAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new AddMachineAttributeMock();
        object? val;

        val = VisceralHelper.GetValue(-1, nameof(Volatile), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Volatile = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(Name), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Name = (string)val;
        }

        return attribute;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class MachineObjectAttributeMock : Attribute
{
    public static readonly string SimpleName = "MachineObject";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = BigMachinesBody.BigMachineNamespace + "." + StandardName;

    public MachineObjectAttributeMock()
    {
    }

    // public uint MachineId { get; set; }

    public MachineControlKind Control { get; set; }

    public bool UseServiceProvider { get; set; } = false;

    public bool StartByDefault { get; set; } = false;

    public int NumberOfTasks { get; set; } = 0;

    public bool Private { get; set; } = false;

    public static MachineObjectAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new MachineObjectAttributeMock();
        object? val;

        /*val = VisceralHelper.GetValue(0, nameof(MachineId), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.MachineId = (uint)val;
        }*/

        val = VisceralHelper.GetValue(-1, nameof(Control), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Control = (MachineControlKind)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(UseServiceProvider), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.UseServiceProvider = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(StartByDefault), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.StartByDefault = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(NumberOfTasks), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.NumberOfTasks = (int)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(Private), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Private = (bool)val;
        }

        return attribute;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class StateMethodAttributeMock : Attribute
{
    public static readonly string SimpleName = "StateMethod";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = BigMachinesBody.BigMachineNamespace + "." + StandardName;

    public StateMethodAttributeMock()
    {
    }

    public uint StateId { get; set; }

    public static StateMethodAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new StateMethodAttributeMock();
        object? val;

        val = VisceralHelper.GetValue(0, nameof(StateId), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.StateId = (uint)val;
        }

        return attribute;
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class CommandMethodAttributeMock : Attribute
{
    public static readonly string SimpleName = "CommandMethod";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = BigMachinesBody.BigMachineNamespace + "." + StandardName;

    public CommandMethodAttributeMock()
    {
    }

    // public uint CommandId { get; set; }

    public bool WithLock { get; set; } = true;

    public bool All { get; set; } = false;

    public static CommandMethodAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new CommandMethodAttributeMock();
        object? val;

        /*val = VisceralHelper.GetValue(0, nameof(CommandId), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.CommandId = (uint)val;
        }*/

        val = VisceralHelper.GetValue(-1, nameof(WithLock), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.WithLock = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(All), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.All = (bool)val;
        }

        return attribute;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class BigMachinesGeneratorOptionAttributeMock : Attribute
{
    public static readonly string SimpleName = "BigMachinesGeneratorOption";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = BigMachinesBody.BigMachineNamespace + "." + StandardName;

    public bool AttachDebugger { get; set; } = false;

    public bool GenerateToFile { get; set; } = false;

    public string? CustomNamespace { get; set; }

    public bool UseModuleInitializer { get; set; } = true;

    public BigMachinesGeneratorOptionAttributeMock()
    {
    }

    public static BigMachinesGeneratorOptionAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new BigMachinesGeneratorOptionAttributeMock();
        object? val;

        val = VisceralHelper.GetValue(-1, nameof(AttachDebugger), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.AttachDebugger = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(GenerateToFile), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.GenerateToFile = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(CustomNamespace), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.CustomNamespace = (string)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(UseModuleInitializer), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.UseModuleInitializer = (bool)val;
        }

        return attribute;
    }
}
