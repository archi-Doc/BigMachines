// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1602

namespace Tinyhand.Generator;

public enum PropertyAccessibility
{
    PublicSetter,
    ProtectedSetter,
    GetterOnly,
}

public sealed class TinyhandUnionAttributeMock
{
    public static readonly string SimpleName = "TinyhandUnion";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;
}

public sealed class TinyhandObjectAttributeMock
{
    public static readonly string SimpleName = "TinyhandObject";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public bool IncludePrivateMembers { get; set; } = false;

    public bool ImplicitMemberNameAsKey { get; set; } = false;

    public bool ExplicitKeysOnly { get; set; } = false;

    public bool ReconstructMembers { get; set; } = true;

    public bool ReuseMembers { get; set; } = true;

    public bool SkipDefaultValues { get; set; } = true;

    public bool UseServiceProvider { get; set; } = false;

    public int ReservedKeys { get; set; } = -1;

    public string LockObject { get; set; } = string.Empty;

    public bool EnumAsString { get; set; } = false;

    public bool UseResolver { get; set; } = false;

    public bool Structural { get; set; } = false;

    public TinyhandObjectAttributeMock()
    {
    }

    /// <summary>
    /// Create an attribute instance from constructor arguments and named arguments.
    /// </summary>
    /// <param name="constructorArguments">Constructor arguments.</param>
    /// <param name="namedArguments">Named arguments.</param>
    /// <returns>A new attribute instance.</returns>
    public static TinyhandObjectAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new TinyhandObjectAttributeMock();

        object? val;
        val = VisceralHelper.GetValue(-1, nameof(ImplicitMemberNameAsKey), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ImplicitMemberNameAsKey = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(IncludePrivateMembers), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.IncludePrivateMembers = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(ExplicitKeysOnly), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ExplicitKeysOnly = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(ReconstructMembers), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ReconstructMembers = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(ReuseMembers), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ReuseMembers = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(SkipDefaultValues), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.SkipDefaultValues = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(UseServiceProvider), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.UseServiceProvider = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(ReservedKeys), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ReservedKeys = (int)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(LockObject), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.LockObject = (string)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(EnumAsString), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.EnumAsString = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(UseResolver), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.UseResolver = (bool)val;
        }

        val = VisceralHelper.GetValue(-1, nameof(Structural), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Structural = (bool)val;
        }

        return attribute;
    }

    public bool LockObjectIsLockable { get; set; }
}

public class KeyAttributeMock
{
    public static readonly string SimpleName = "Key";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public int? IntKey { get; private set; }

    public string? StringKey { get; private set; }

    public bool Condition { get; private set; } = true;

    public string AddProperty { get; set; } = string.Empty;

    public PropertyAccessibility PropertyAccessibility { get; set; } = PropertyAccessibility.PublicSetter;

    public KeyAttributeMock(int x)
    {
        this.IntKey = x;
    }

    public KeyAttributeMock(string x)
    {
        this.StringKey = x;
    }

    public static KeyAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new KeyAttributeMock(null!);

        if (constructorArguments.Length > 0)
        {
            var val = constructorArguments[0];
            if (val is int intKey)
            {
                attribute.IntKey = intKey;
            }
            else if (val is string stringKey)
            {
                attribute.StringKey = stringKey;
            }
        }

        if (attribute.IntKey == null && attribute.StringKey == null)
        {// Exception: KeyAttribute requires a valid int key or string key.
            throw new ArgumentNullException();
        }

        var v = VisceralHelper.GetValue(-1, nameof(Condition), constructorArguments, namedArguments);
        if (v != null)
        {
            attribute.Condition = (bool)v;
        }

        v = VisceralHelper.GetValue(-1, nameof(AddProperty), constructorArguments, namedArguments);
        if (v != null)
        {
            attribute.AddProperty = (string)v;
        }

        v = VisceralHelper.GetValue(-1, nameof(PropertyAccessibility), constructorArguments, namedArguments);
        if (v != null)
        {
            attribute.PropertyAccessibility = (PropertyAccessibility)v;
        }

        return attribute;
    }

    public void SetKey(string x)
    {
        this.IntKey = null;
        this.StringKey = x;
    }
}

public class MemberNameAsKeyAttributeMock
{
    public static readonly string SimpleName = "MemberNameAsKey";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public static MemberNameAsKeyAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new MemberNameAsKeyAttributeMock();

        return attribute;
    }
}

public class MaxLengthAttributeMock
{
    public static readonly string SimpleName = "MaxLength";
    public static readonly string Name = SimpleName + "Attribute";
    public static readonly string FullName = "Tinyhand." + Name;

    public int MaxLength { get; private set; } = -1;

    public int MaxChildLength { get; private set; } = -1;

    public MaxLengthAttributeMock()
    {
    }

    public static MaxLengthAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new MaxLengthAttributeMock();

        object? val;
        val = VisceralHelper.GetValue(0, nameof(MaxLength), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.MaxLength = (int)val;
        }

        val = VisceralHelper.GetValue(1, nameof(MaxChildLength), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.MaxChildLength = (int)val;
        }

        return attribute;
    }
}
