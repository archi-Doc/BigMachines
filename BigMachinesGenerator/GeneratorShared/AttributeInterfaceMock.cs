// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines.Generator
{
    public static class AttributeHelper
    {
        public static object? GetValue(int constructorIndex, string? name, object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            if (constructorIndex >= 0 && constructorIndex < constructorArguments.Length)
            {// Constructor Argument.
                return constructorArguments[constructorIndex];
            }
            else if (name != null)
            {// Named Argument.
                var pair = namedArguments.FirstOrDefault(x => x.Key == name);
                if (pair.Equals(default(KeyValuePair<string, object?>)))
                {
                    return null;
                }

                return pair.Value;
            }
            else
            {
                return null;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class MachineObjectAttributeMock : Attribute
    {
        public static readonly string SimpleName = "MachineObject";
        public static readonly string StandardName = SimpleName + "Attribute";
        public static readonly string FullName = "BigMachines." + StandardName;

        public MachineObjectAttributeMock()
        {
        }

        public uint MachineTypeId { get; set; }

        public ISymbol? Group { get; set; }

        public static MachineObjectAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new MachineObjectAttributeMock();
            object? val;

            val = AttributeHelper.GetValue(0, nameof(MachineTypeId), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.MachineTypeId = (uint)val;
            }

            val = AttributeHelper.GetValue(-1, nameof(Group), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.Group = val as ISymbol;
            }

            return attribute;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class StateMethodAttributeMock : Attribute
    {
        public static readonly string SimpleName = "StateMethod";
        public static readonly string StandardName = SimpleName + "Attribute";
        public static readonly string FullName = "BigMachines." + StandardName;

        public StateMethodAttributeMock()
        {
        }

        public uint Id { get; set; }

        public static StateMethodAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new StateMethodAttributeMock();
            object? val;

            val = AttributeHelper.GetValue(0, nameof(Id), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.Id = (uint)val;
            }

            return attribute;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class BigMachinesGeneratorOptionAttributeMock : Attribute
    {
        public static readonly string SimpleName = "BigMachinesGeneratorOption";
        public static readonly string StandardName = SimpleName + "Attribute";
        public static readonly string FullName = "BigMachines." + StandardName;

        public bool AttachDebugger { get; set; } = false;

        public bool GenerateToFile { get; set; } = false;

        public string? CustomNamespace { get; set; }

        public BigMachinesGeneratorOptionAttributeMock()
        {
        }

        public static BigMachinesGeneratorOptionAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
        {
            var attribute = new BigMachinesGeneratorOptionAttributeMock();
            object? val;

            val = AttributeHelper.GetValue(-1, nameof(AttachDebugger), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.AttachDebugger = (bool)val;
            }

            val = AttributeHelper.GetValue(-1, nameof(GenerateToFile), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.GenerateToFile = (bool)val;
            }

            val = AttributeHelper.GetValue(-1, nameof(CustomNamespace), constructorArguments, namedArguments);
            if (val != null)
            {
                attribute.CustomNamespace = (string)val;
            }

            return attribute;
        }
    }
}
