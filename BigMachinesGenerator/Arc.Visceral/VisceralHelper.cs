// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1202 // Elements should be ordered by access

namespace Arc.Visceral
{
    public static class VisceralHelper
    {
        public static bool IsValidIdentifier(string identifier)
        {
            var span = identifier.AsSpan();
            if (span.Length == 0)
            {
                return false;
            }

            if (IsValidIdentifierCharacter(span[0], true) == false)
            {
                return false;
            }

            for (var n = 1; n < span.Length; n++)
            {
                if (IsValidIdentifierCharacter(span[n], false) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsValidIdentifierCharacter(char c, bool first = false)
        {
            if (first && c == '_')
            {// first '_'
                return true;
            }

            var cat = char.GetUnicodeCategory(c);

            if (cat == UnicodeCategory.UppercaseLetter ||
                cat == UnicodeCategory.LowercaseLetter ||
                cat == UnicodeCategory.TitlecaseLetter ||
                cat == UnicodeCategory.ModifierLetter ||
                cat == UnicodeCategory.OtherLetter ||
                cat == UnicodeCategory.LetterNumber)
            {// letter-character
                return true;
            }

            if (first)
            {
                if (c == '_')
                {
                    return true;
                }
            }
            else
            {
                if (cat == UnicodeCategory.DecimalDigitNumber ||
                cat == UnicodeCategory.ConnectorPunctuation ||
                cat == UnicodeCategory.NonSpacingMark ||
                cat == UnicodeCategory.SpacingCombiningMark ||
                cat == UnicodeCategory.Format)
                {
                    return true;
                }
            }

            return false;
        }

        public static string AssemblyNameToIdentifier(string name)
        {
            var span = new Span<char>(name.ToCharArray());
            var changed = false;

            for (var n = 0; n < name.Length; n++)
            {
                if (!IsValidIdentifierCharacter(span[n], n == 0))
                {// Replace invalid character with '_' (though not a perfect solution)
                    span[n] = '_';
                    changed = true;
                }
            }

            if (!changed)
            {
                return name;
            }
            else
            {
                return span.ToString();
            }
        }

        public static Type? GetUnderlyingType(this MemberInfo memberInfo) => memberInfo.MemberType switch
        {
            MemberTypes.Constructor => typeof(void), // ((ConstructorInfo)memberInfo)
            MemberTypes.Event => ((EventInfo)memberInfo).EventHandlerType,
            MemberTypes.Field => ((FieldInfo)memberInfo).FieldType,
            MemberTypes.Method => ((MethodInfo)memberInfo).ReturnType,
            MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
            _ => null,
        };

        public static string AccessibilityToString(this Accessibility accessibility) => accessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Public => "public",
            _ => string.Empty,
        };

        public static string AccessibilityToStringPlusSpace(this Accessibility accessibility) => accessibility switch
        {
            Accessibility.Private => "private ",
            Accessibility.ProtectedAndInternal => "private protected ",
            Accessibility.Protected => "protected ",
            Accessibility.Internal => "internal ",
            Accessibility.ProtectedOrInternal => "protected internal ",
            Accessibility.Public => "public ",
            _ => string.Empty,
        };

        public static (string property, string getter, string setter) GetterSetterAccessibilityToPropertyString(Accessibility getter, Accessibility setter)
        {
            var max = getter;
            max = max > setter ? max : setter;

            var p = max.AccessibilityToStringPlusSpace();
            return (p,
                getter == max ? string.Empty : getter.AccessibilityToStringPlusSpace(),
                setter == max ? string.Empty : setter.AccessibilityToStringPlusSpace());
        }

        public static bool IsInternal(this Accessibility accessibility) => accessibility switch
        {
            Accessibility.ProtectedAndInternal => true,
            Accessibility.Internal => true,
            Accessibility.ProtectedOrInternal => false,
            _ => false,
        };

        public static Accessibility FieldInfoToAccessibility(FieldInfo? fi)
        {
            if (fi == null)
            {
                return Accessibility.NotApplicable;
            }
            else if (fi.IsPrivate)
            {
                return Accessibility.Private;
            }
            else if (fi.IsFamilyAndAssembly)
            {
                return Accessibility.ProtectedAndInternal;
            }
            else if (fi.IsFamily)
            {
                return Accessibility.Protected;
            }
            else if (fi.IsAssembly)
            {
                return Accessibility.Internal;
            }
            else if (fi.IsFamilyOrAssembly)
            {
                return Accessibility.ProtectedOrInternal;
            }
            else if (fi.IsPublic)
            {
                return Accessibility.Public;
            }
            else
            {
                return Accessibility.NotApplicable;
            }
        }

        public static Accessibility MethodBaseToAccessibility(MethodBase? mb)
        {
            if (mb == null)
            {
                return Accessibility.NotApplicable;
            }
            else if (mb.IsPrivate)
            {
                return Accessibility.Private;
            }
            else if (mb.IsFamilyAndAssembly)
            {
                return Accessibility.ProtectedAndInternal;
            }
            else if (mb.IsFamily)
            {
                return Accessibility.Protected;
            }
            else if (mb.IsAssembly)
            {
                return Accessibility.Internal;
            }
            else if (mb.IsFamilyOrAssembly)
            {
                return Accessibility.ProtectedOrInternal;
            }
            else if (mb.IsPublic)
            {
                return Accessibility.Public;
            }

            return Accessibility.NotApplicable;
        }

        public static string PropertyInfoToAccessibilityName(PropertyInfo pi)
        {
            var a1 = MethodBaseToAccessibility(pi.GetMethod);
            var a2 = MethodBaseToAccessibility(pi.SetMethod);

            var min = a1 < a2 ? a1 : a2;
            return AccessibilityToString(min);
        }

        public static string EventInfoToAccessibilityName(EventInfo ei)
        {
            var a1 = MethodBaseToAccessibility(ei.AddMethod);
            var a2 = MethodBaseToAccessibility(ei.RemoveMethod);

            var min = a1 < a2 ? a1 : a2;
            return AccessibilityToString(min);
        }

        public static VisceralGenericsKind TypeToGenericsKind(ISymbol symbol)
        {
            var ts = symbol as INamedTypeSymbol;
            if (ts == null)
            {
                return VisceralGenericsKind.NotGeneric;
            }

            if (!ts.IsGenericType)
            {
                return VisceralGenericsKind.NotGeneric;
            }
            else if (ts.IsUnboundGenericType)
            {
                return VisceralGenericsKind.OpenGeneric; // VisceralGenericsKind.UnboundGeneric;
            }
            else
            {
                var c = ts.ContainingType;
                while (c != null)
                {
                    if (TypeToGenericsKind(c) == VisceralGenericsKind.OpenGeneric)
                    {
                        return VisceralGenericsKind.OpenGeneric;
                    }

                    c = c.ContainingType;
                }

                foreach (var x in ts.TypeArguments)
                {
                    if (x.Kind == SymbolKind.TypeParameter)
                    {
                        return VisceralGenericsKind.OpenGeneric;
                    }
                    else if (x.Kind == SymbolKind.NamedType)
                    {
                        if (TypeToGenericsKind(x) == VisceralGenericsKind.OpenGeneric)
                        {
                            return VisceralGenericsKind.OpenGeneric;
                        }
                    }
                }

                return VisceralGenericsKind.ClosedGeneric;
            }
        }

        public static bool SortAndSequenceEqual<T>(ImmutableArray<T> x, ImmutableArray<T> y)
        {
            if (x.IsDefault || y.IsDefault)
            {
                return x.IsDefault && y.IsDefault;
            }

            return x.Sort().SequenceEqual(y.Sort());
        }

        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol? symbol)
        {
            var current = symbol;
            while (current != null && current.SpecialType != SpecialType.System_Object)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static bool IsStruct(this Type type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
        }

        public static bool IsClass(this Type type) => type.IsClass;

        public static bool IsType(this VisceralObjectKind type) =>
            type == VisceralObjectKind.Class ||
            type == VisceralObjectKind.Struct ||
            type == VisceralObjectKind.Interface ||
            type == VisceralObjectKind.Record ||
            type == VisceralObjectKind.TypeParameter;

        public static bool IsReferenceType(this VisceralObjectKind type) =>
            type == VisceralObjectKind.Class ||
            type == VisceralObjectKind.Record ||
            type == VisceralObjectKind.Interface;

        public static bool IsValueType(this VisceralObjectKind type) =>
            type == VisceralObjectKind.Struct;

        public static bool IsReferenceType(this TypeKind typeKind) =>
            typeKind == TypeKind.Array ||
            typeKind == TypeKind.Class ||
            typeKind == TypeKind.Dynamic;

        public static bool IsValueType(this TypeKind typeKind) =>
            typeKind == TypeKind.Enum ||
            typeKind == TypeKind.Struct;

        public static bool IsValue(this VisceralObjectKind type) =>
            type == VisceralObjectKind.Field ||
            type == VisceralObjectKind.Property;

        public static string ToUnboundTypeName(this string typeName)
        {
            var s = typeName.AsSpan();
            var length = s.Length;
            Span<char> d = length <= 1024 ? stackalloc char[length] : new char[length];

            var dp = 0;
            for (var sp = 0; sp < length; sp++)
            {
                d[dp++] = s[sp];
                if (s[sp] == '<')
                {
                    sp++;
                    while (sp < length && s[sp] != '>')
                    {
                        sp++;
                        continue;
                    }

                    d[dp++] = '>';
                }
            }

            return d.Slice(0, dp).ToString();
        }

        public static string ToPathSafeString(this string path)
        { // 0-31, 34 ", 42 *, 47 /, 58 :, 60 <, 62 >, 63 ?, 92 \, 124 |
            var length = path.Length;
            ReadOnlySpan<char> source = path.AsSpan();
            char[]? pooledName = null;
            Span<char> destination = length <= 10 ?
                stackalloc char[length] : (pooledName = ArrayPool<char>.Shared.Rent(length));
            int destinationLength = 0;

            foreach (var x in source)
            {
                if (x <= 31 || x == '\"' || x == '*' || x == '/' || x == ':' || x == '|')
                {// Skip
                    continue;
                }
                else if (x == '<')
                {// Converts < to {
                    destination[destinationLength++] = '{';
                }
                else if (x == '>')
                {// Converts > to }
                    destination[destinationLength++] = '}';
                }
                else if (x == '?')
                {// Converts ? to $
                    destination[destinationLength++] = '$';
                }
                else
                {// Other (includes \)
                    destination[destinationLength++] = x;
                }
            }

            var result = destination.Slice(0, destinationLength).ToString();
            if (pooledName != null)
            {
                ArrayPool<char>.Shared.Return(pooledName);
            }

            return result;
        }

        public static bool IsDefaultValueType(string name) => name switch
        {
            "bool" => true,
            _ => false,
        };

        public static string? Primitives_ShortenName(string fullName) => fullName switch
        {
            "System.Void" => "void",
            "System.Object" => "object",
            "System.Boolean" => "bool",
            "System.Boolean?" => "bool?",
            "System.Boolean[]" => "bool[]",
            "System.SByte" => "sbyte",
            "System.SByte?" => "sbyte?",
            "System.SByte[]" => "sbyte[]",
            "System.Byte" => "byte",
            "System.Byte?" => "byte?",
            "System.Byte[]" => "byte[]",
            "System.Int16" => "short",
            "System.Int16?" => "short?",
            "System.Int16[]" => "short[]",
            "System.UInt16" => "ushort",
            "System.UInt16?" => "ushort?",
            "System.UInt16[]" => "ushort[]",
            "System.Int32" => "int",
            "System.Int32?" => "int?",
            "System.Int32[]" => "int[]",
            "System.UInt32" => "uint",
            "System.UInt32?" => "uint?",
            "System.UInt32[]" => "uint[]",
            "System.Int64" => "long",
            "System.Int64?" => "long?",
            "System.Int64[]" => "long[]",
            "System.UInt64" => "ulong",
            "System.UInt64?" => "ulong?",
            "System.UInt64[]" => "ulong[]",
            "System.Single" => "float",
            "System.Single?" => "float?",
            "System.Single[]" => "float[]",
            "System.Double" => "double",
            "System.Double?" => "double?",
            "System.Double[]" => "double[]",
            "System.Decimal" => "decimal",
            "System.Decimal?" => "decimal?",
            "System.Decimal[]" => "decimal[]",
            "System.String" => "string",
            "System.String?" => "string",
            "System.String[]" => "string[]",
            "System.Char" => "char",
            "System.Char?" => "char?",
            "System.Char[]" => "char[]",

            "Void" => "void",
            "Object" => "object",
            "Boolean" => "bool",
            "Boolean?" => "bool?",
            "Boolean[]" => "bool[]",
            "SByte" => "sbyte",
            "SByte?" => "sbyte?",
            "SByte[]" => "sbyte[]",
            "Byte" => "byte",
            "Byte?" => "byte?",
            "Byte[]" => "byte[]",
            "Int16" => "short",
            "Int16?" => "short?",
            "Int16[]" => "short[]",
            "UInt16" => "ushort",
            "UInt16?" => "ushort?",
            "UInt16[]" => "ushort[]",
            "Int32" => "int",
            "Int32?" => "int?",
            "Int32[]" => "int[]",
            "UInt32" => "uint",
            "UInt32?" => "uint?",
            "UInt32[]" => "uint[]",
            "Int64" => "long",
            "Int64?" => "long?",
            "Int64[]" => "long[]",
            "UInt64" => "ulong",
            "UInt64?" => "ulong?",
            "UInt64[]" => "ulong[]",
            "Single" => "float",
            "Single?" => "float?",
            "Single[]" => "float[]",
            "Double" => "double",
            "Double?" => "double?",
            "Double[]" => "double[]",
            "Decimal" => "decimal",
            "Decimal?" => "decimal?",
            "Decimal[]" => "decimal[]",
            "String" => "string",
            "String?" => "string",
            "String[]" => "string[]",
            "Char" => "char",
            "Char?" => "char?",
            "Char[]" => "char[]",

            "void" => "void",
            "object" => "object",
            "bool" => "bool",
            "bool?" => "bool?",
            "bool[]" => "bool[]",
            "sbyte" => "sbyte",
            "sbyte?" => "sbyte?",
            "sbyte[]" => "sbyte[]",
            "byte" => "byte",
            "byte?" => "byte?",
            "byte[]" => "byte[]",
            "short" => "short",
            "short?" => "short?",
            "short[]" => "short[]",
            "ushort" => "ushort",
            "ushort?" => "ushort?",
            "ushort[]" => "ushort[]",
            "int" => "int",
            "int?" => "int?",
            "int[]" => "int[]",
            "uint" => "uint",
            "uint?" => "uint?",
            "uint[]" => "uint[]",
            "long" => "long",
            "long?" => "long?",
            "long[]" => "long[]",
            "ulong" => "ulong",
            "ulong?" => "ulong?",
            "ulong[]" => "ulong[]",
            "float" => "float",
            "float?" => "float?",
            "float[]" => "float[]",
            "double" => "double",
            "double?" => "double?",
            "double[]" => "double[]",
            "decimal" => "decimal",
            "decimal?" => "decimal?",
            "decimal[]" => "decimal[]",
            "string" => "string",
            "string?" => "string",
            "string[]" => "string[]",
            "char" => "char",
            "char?" => "char?",
            "char[]" => "char[]",

            _ => null,
        };

        public static string? Primitives_ShortenFullName(string fullName) => fullName switch
        {
            "System.Void" => "void",
            "System.Object" => "object",
            "System.Boolean" => "bool",
            "System.Boolean?" => "bool?",
            "System.Boolean[]" => "bool[]",
            "System.SByte" => "sbyte",
            "System.SByte?" => "sbyte?",
            "System.SByte[]" => "sbyte[]",
            "System.Byte" => "byte",
            "System.Byte?" => "byte?",
            "System.Byte[]" => "byte[]",
            "System.Int16" => "short",
            "System.Int16?" => "short?",
            "System.Int16[]" => "short[]",
            "System.UInt16" => "ushort",
            "System.UInt16?" => "ushort?",
            "System.UInt16[]" => "ushort[]",
            "System.Int32" => "int",
            "System.Int32?" => "int?",
            "System.Int32[]" => "int[]",
            "System.UInt32" => "uint",
            "System.UInt32?" => "uint?",
            "System.UInt32[]" => "uint[]",
            "System.Int64" => "long",
            "System.Int64?" => "long?",
            "System.Int64[]" => "long[]",
            "System.UInt64" => "ulong",
            "System.UInt64?" => "ulong?",
            "System.UInt64[]" => "ulong[]",
            "System.Single" => "float",
            "System.Single?" => "float?",
            "System.Single[]" => "float[]",
            "System.Double" => "double",
            "System.Double?" => "double?",
            "System.Double[]" => "double[]",
            "System.Decimal" => "decimal",
            "System.Decimal?" => "decimal?",
            "System.Decimal[]" => "decimal[]",
            "System.String" => "string",
            "System.String?" => "string",
            "System.String[]" => "string[]",
            "System.Char" => "char",
            "System.Char?" => "char?",
            "System.Char[]" => "char[]",
            _ => null,
        };

        public static string? Primitives_ShortenSimpleName(string fullName) => fullName switch
        {
            "Void" => "void",
            "Object" => "object",
            "Boolean" => "bool",
            "Boolean?" => "bool?",
            "Boolean[]" => "bool[]",
            "SByte" => "sbyte",
            "SByte?" => "sbyte?",
            "SByte[]" => "sbyte[]",
            "Byte" => "byte",
            "Byte?" => "byte?",
            "Byte[]" => "byte[]",
            "Int16" => "short",
            "Int16?" => "short?",
            "Int16[]" => "short[]",
            "UInt16" => "ushort",
            "UInt16?" => "ushort?",
            "UInt16[]" => "ushort[]",
            "Int32" => "int",
            "Int32?" => "int?",
            "Int32[]" => "int[]",
            "UInt32" => "uint",
            "UInt32?" => "uint?",
            "UInt32[]" => "uint[]",
            "Int64" => "long",
            "Int64?" => "long?",
            "Int64[]" => "long[]",
            "UInt64" => "ulong",
            "UInt64?" => "ulong?",
            "UInt64[]" => "ulong[]",
            "Single" => "float",
            "Single?" => "float?",
            "Single[]" => "float[]",
            "Double" => "double",
            "Double?" => "double?",
            "Double[]" => "double[]",
            "Decimal" => "decimal",
            "Decimal?" => "decimal?",
            "Decimal[]" => "decimal[]",
            "String" => "string",
            "String?" => "string?",
            "String[]" => "string[]",
            "Char" => "char",
            "Char?" => "char?",
            "Char[]" => "char[]",
            _ => null,
        };

        public static Type? Primitives_FullNameToType(string fullName) => fullName switch
        {
            "System.Object" => typeof(object),
            "System.Boolean" => typeof(bool),
            "System.Boolean?" => typeof(bool?),
            "System.Boolean[]" => typeof(bool[]),
            "System.SByte" => typeof(sbyte),
            "System.SByte?" => typeof(sbyte?),
            "System.SByte[]" => typeof(sbyte[]),
            "System.Byte" => typeof(byte),
            "System.Byte?" => typeof(byte?),
            "System.Byte[]" => typeof(byte[]),
            "System.Int16" => typeof(short),
            "System.Int16?" => typeof(short?),
            "System.Int16[]" => typeof(short[]),
            "System.UInt16" => typeof(ushort),
            "System.UInt16?" => typeof(ushort?),
            "System.UInt16[]" => typeof(ushort[]),
            "System.Int32" => typeof(int),
            "System.Int32?" => typeof(int?),
            "System.Int32[]" => typeof(int[]),
            "System.UInt32" => typeof(uint),
            "System.UInt32?" => typeof(uint?),
            "System.UInt32[]" => typeof(uint[]),
            "System.Int64" => typeof(long),
            "System.Int64?" => typeof(long?),
            "System.Int64[]" => typeof(long[]),
            "System.UInt64" => typeof(ulong),
            "System.UInt64?" => typeof(ulong?),
            "System.UInt64[]" => typeof(ulong[]),
            "System.Single" => typeof(float),
            "System.Single?" => typeof(float?),
            "System.Single[]" => typeof(float[]),
            "System.Double" => typeof(double),
            "System.Double?" => typeof(double?),
            "System.Double[]" => typeof(double[]),
            "System.Decimal" => typeof(decimal),
            "System.Decimal?" => typeof(decimal?),
            "System.Decimal[]" => typeof(decimal[]),
            "System.String" => typeof(string),
            "System.String?" => typeof(string),
            "System.String[]" => typeof(string[]),
            _ => null,
        };

        public static Type? Primitives_DisplayNameToType(string fullName) => fullName switch
        {
            "Object" => typeof(object),
            "Boolean" => typeof(bool),
            "Boolean?" => typeof(bool?),
            "Boolean[]" => typeof(bool[]),
            "SByte" => typeof(sbyte),
            "SByte?" => typeof(sbyte?),
            "SByte[]" => typeof(sbyte[]),
            "Byte" => typeof(byte),
            "Byte?" => typeof(byte?),
            "Byte[]" => typeof(byte[]),
            "Int16" => typeof(short),
            "Int16?" => typeof(short?),
            "Int16[]" => typeof(short[]),
            "UInt16" => typeof(ushort),
            "UInt16?" => typeof(ushort?),
            "UInt16[]" => typeof(ushort[]),
            "Int32" => typeof(int),
            "Int32?" => typeof(int?),
            "Int32[]" => typeof(int[]),
            "UInt32" => typeof(uint),
            "UInt32?" => typeof(uint?),
            "UInt32[]" => typeof(uint[]),
            "Int64" => typeof(long),
            "Int64?" => typeof(long?),
            "Int64[]" => typeof(long[]),
            "UInt64" => typeof(ulong),
            "UInt64?" => typeof(ulong?),
            "UInt64[]" => typeof(ulong[]),
            "Single" => typeof(float),
            "Single?" => typeof(float?),
            "Single[]" => typeof(float[]),
            "Double" => typeof(double),
            "Double?" => typeof(double?),
            "Double[]" => typeof(double[]),
            "Decimal" => typeof(decimal),
            "Decimal?" => typeof(decimal?),
            "Decimal[]" => typeof(decimal[]),
            "String" => typeof(string),
            "String?" => typeof(string),
            "String[]" => typeof(string[]),
            _ => null,
        };

        public static string MemberInfoToFullName(this MemberInfo memberInfo) =>
            VisceralHelper.TypeToFullName(memberInfo.DeclaringType) + "." + VisceralHelper.MemberInfoToLocalName(memberInfo);

        public static string MemberInfoToLocalName(this MemberInfo memberInfo)
        {
            if (memberInfo is MethodBase mb)
            {// Adds method generics and parameters.
                var sb = new StringBuilder(VisceralHelper.MemberInfoToSimpleName(memberInfo));

                // Generics <T, U>
                if (mb.IsGenericMethod)
                {
                    sb.Append('<');
                    var g = mb.GetGenericArguments();
                    for (var i = 0; i < g.Length; i++)
                    {
                        sb.Append(VisceralHelper.TypeToFullName(g[i]));
                        if (i != (g.Length - 1))
                        {
                            sb.Append(", ");
                        }
                    }

                    sb.Append('>');
                }

                // Parameters (int, string)
                sb.Append('(');
                var p = mb.GetParameters();
                for (var i = 0; i < p.Length; i++)
                {
                    sb.Append(VisceralHelper.TypeToFullName(p[i].ParameterType));
                    if (i != (p.Length - 1))
                    {
                        sb.Append(", ");
                    }
                }

                sb.Append(')');

                return sb.ToString();
            }

            return VisceralHelper.MemberInfoToSimpleName(memberInfo);
        }

        public static string MemberInfoToSimpleName(this MemberInfo memberInfo)
        {
            if (memberInfo.MemberType == MemberTypes.Constructor)
            {// Constructor method ".ctor" -> "ClassName"
                return VisceralHelper.GetSimpleGenericName(memberInfo.DeclaringType.Name);
            }

            /* else if (VisceralHelper.GetUnderlyingType(memberInfo) is { } type)
            {
                if (VisceralHelper.IsTuple(type))
                {
                    return "ValueTuple";
                }
            }*/

            return VisceralHelper.GetSimpleGenericName(memberInfo.Name);
        }

        public static string TypeToFullName(this Type type) => VisceralHelper.TypeToName(type, true);

        public static string TypeToLocalName(this Type type) => VisceralHelper.TypeToName(type, false);

        public static string GetNamespaceAndClass(this Type type)
        {
            if (type.DeclaringType == null)
            {
                return type.Namespace + "." + VisceralHelper.TypeToLocalName(type);
            }
            else
            {
                var declaringType = type.DeclaringType;
                var s = string.Empty;
                while (declaringType != null)
                {
                    type = declaringType;
                    s = "." + VisceralHelper.TypeToLocalName(type) + s;
                    declaringType = type.DeclaringType;
                }

                return type.Namespace + s;
            }
        }

        private static string GetSimpleGenericName(string name)
        {
            var idx = name.IndexOf('`');
            if (idx < 0)
            {
                return name;
            }
            else
            {
                return name.Substring(0, idx);
            }
        }

        private static string TypeToName(Type type, bool appendNamespace)
        {
            if (type.IsArray)
            {
                var sb = new StringBuilder(VisceralHelper.TypeToName(type.GetElementType(), appendNamespace));
                sb.Append('[');
                for (var n = 1; n < type.GetArrayRank(); n++)
                {
                    sb.Append(',');
                }

                sb.Append(']');

                return sb.ToString();
            }
            else if (type.IsGenericParameter)
            {
                return type.Name;
            }
            else if (!type.IsGenericType)
            {
                var shortName = VisceralHelper.Primitives_ShortenSimpleName(type.Name);
                if (shortName != null)
                {
                    return shortName;
                }

                if (appendNamespace)
                {
                    return VisceralHelper.GetNamespaceAndClass(type);
                }
                else
                {
                    return type.Name;
                }
            }

            return GenericTypeToName(type, appendNamespace);
        }

        public static string GenericTypeToName(Type type, bool appendNamespace)
        {
            var definitionType = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();
            StringBuilder sb;

            if (definitionType == typeof(Nullable<>) && args.Length == 1)
            { // int?
                var name = VisceralHelper.TypeToName(args[0], appendNamespace);
                if (appendNamespace)
                {
                    var shortName = VisceralHelper.Primitives_ShortenFullName(name);
                    if (shortName != null)
                    {
                        return shortName + "?";
                    }
                    else
                    {
                        return name + "?";
                    }
                }
                else
                {
                    return (VisceralHelper.Primitives_ShortenSimpleName(name) ?? name) + "?";
                }
            }
            else if (definitionType.IsTuple())
            { // (int, string)
                sb = new StringBuilder();

                var count = definitionType.GetGenericArguments().Length;
                sb.Append("(");
                for (int i = 0; i < args.Length; ++i)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(VisceralHelper.TypeToName(args[i], true));
                }

                sb.Append(")");

                return sb.ToString();
            }
            else
            { // Class<T1, T2>
                if (!appendNamespace)
                {
                    sb = new StringBuilder();
                    sb.Append(GetSimpleGenericName(type.Name));

                    var declaringCount = type.DeclaringType == null ? 0 : type.DeclaringType.GetGenericArguments().Length;
                    var currentCount = type.GetGenericArguments().Length;
                    if (currentCount > declaringCount)
                    {
                        sb.Append("<");
                        for (var i = declaringCount; i < currentCount; i++)
                        {
                            if (i > declaringCount)
                            {
                                sb.Append(", ");
                            }

                            sb.Append(VisceralHelper.TypeToName(args[i], true));
                        }

                        sb.Append(">");
                    }

                    return sb.ToString();
                }

                var list = new List<Type>();
                while (type != null)
                {
                    list.Add(type);
                    type = type.DeclaringType;
                }

                sb = new StringBuilder();
                sb.Append(list.Last().Namespace);
                sb.Append('.');

                var lastCount = 0;
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    type = list[i];

                    sb.Append(GetSimpleGenericName(type.Name));

                    var currentCount = type.GetGenericArguments().Length;
                    if (currentCount > lastCount)
                    {
                        sb.Append("<");
                        for (var j = lastCount; j < currentCount; j++)
                        {
                            if (j > lastCount)
                            {
                                sb.Append(", ");
                            }

                            sb.Append(VisceralHelper.TypeToName(args[j], true));
                        }

                        sb.Append(">");
                    }

                    lastCount = currentCount;

                    if (i > 0)
                    {
                        sb.Append('.');
                    }
                }

                return sb.ToString();
            }
        }

        /*public static string GenericTypeToName(Type type, bool appendNamespace)
        {
            var definitionType = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();
            var rootType = type.DeclaringType;
            while (rootType != null && rootType.DeclaringType != null)
            {
                rootType = rootType.DeclaringType;
            }

            if (definitionType == typeof(Nullable<>) && args.Length == 1)
            { // int?
                var name = VisceralHelper.TypeToName(args[0], appendNamespace);
                if (appendNamespace)
                {
                    var shortName = VisceralHelper.Primitives_ShortenFullName(name);
                    if (shortName != null)
                    {
                        return shortName + "?";
                    }
                    else
                    {
                        return name + "?";
                    }
                }
                else
                {
                    return (VisceralHelper.Primitives_ShortenSimpleName(name) ?? name) + "?";
                }
            }
            else if (definitionType.IsTuple())
            { // (int, string)
                var sb = new StringBuilder();

                sb.Append("(");
                for (int i = 0; i < args.Length; ++i)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(VisceralHelper.TypeToName(args[i], true));
                }

                sb.Append(")");

                return sb.ToString();
            }
            else
            { // Class<T1, T2>
                var sb = new StringBuilder();

                sb.Append(GetSimpleGenericName(type.Name));
                if (type.ContainsGenericParameters)
                {
                    sb.Append("<");
                    for (int i = 0; i < args.Length; ++i)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }

                        sb.Append(VisceralHelper.TypeToName(args[i], true));
                    }

                    sb.Append(">");
                }

                if (appendNamespace)
                {
                    return VisceralHelper.GetNamespaceAndClass(type) + "." + sb.ToString();
                }
                else
                {
                    return sb.ToString();
                }
            }
        }*/

        public static string TypeToSimpleName(this Type type)
        {
            if (type.IsArray)
            {
                return VisceralHelper.TypeToSimpleName(type.GetElementType());
            }
            else if (type.IsGenericParameter)
            {
                return type.Name;
            }
            else if (type.IsGenericType)
            {
                var definitionType = type.GetGenericTypeDefinition();
                var args = type.GetGenericArguments();

                if (definitionType == typeof(Nullable<>) && args.Length == 1)
                { // int?
                    var name = VisceralHelper.TypeToName(args[0], false);
                    return VisceralHelper.Primitives_ShortenSimpleName(name) ?? name; // + "?";
                }
                else
                {
                    return GetSimpleGenericName(type.Name);
                }
            }

            /* else if (type.DeclaringType != null)
           {
               var declaringType = type.DeclaringType;
               var s = GetSimpleGenericName(type.Name);
               while (declaringType != null)
               {
                   type = declaringType;
                   s = VisceralHelper.TypeToLocalName(type) + s;
                   declaringType = type.DeclaringType;
               }
           }*/
            else if (VisceralHelper.IsTuple(type))
            {
                return "ValueTuple";
            }

            return VisceralHelper.Primitives_ShortenSimpleName(type.Name) ?? type.Name;
        }

        public static bool IsTuple(this Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            var openType = type.GetGenericTypeDefinition();

            return openType == typeof(ValueTuple<>)
                || openType == typeof(ValueTuple<,>)
                || openType == typeof(ValueTuple<,,>)
                || openType == typeof(ValueTuple<,,,>)
                || openType == typeof(ValueTuple<,,,,>)
                || openType == typeof(ValueTuple<,,,,,>)
                || openType == typeof(ValueTuple<,,,,,,>)
                || (openType == typeof(ValueTuple<,,,,,,,>) && IsTuple(type.GetGenericArguments()[7]));
        }

        public static string GetProjectPath()
        {
            var current = System.IO.Directory.GetCurrentDirectory();

            var debugIndex = current.IndexOf("\\bin\\Debug");
            if (debugIndex >= 0)
            {
                return current.Substring(0, debugIndex);
            }

            var releaseIndex = current.IndexOf("\\bin\\Release");
            if (releaseIndex >= 0)
            {
                return current.Substring(0, releaseIndex);
            }

            throw new Exception("Fatal error, no project path.");
        }
    }
}
