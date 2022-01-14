// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Globalization;

namespace Arc.Visceral;

public static class VisceralDefaultValue
{
    public static string GetEscapedString(string s)
    {
        int max = (s.Length * 2) + 1;
        var source = s.AsSpan();
        Span<char> buffer = max <= 1024 ? stackalloc char[max] : new char[max];
        var destination = buffer;

        var from = 0;
        for (int i = 0; i < source.Length; i++)
        {
            char escapeChar;
            switch (source[i])
            {
                case '"': // 0x22
                    escapeChar = '"';
                    break;
                case '\\': // 0x5C
                    escapeChar = '\\';
                    break;
                case '\b': // 0x08
                    escapeChar = 'b';
                    break;
                case '\f': // 0xC
                    escapeChar = 'f';
                    break;
                case '\n': // 0x0A
                    escapeChar = 'n';
                    break;
                case '\r': // 0x0D
                    escapeChar = 'r';
                    break;
                case '\t': // 0x09
                    escapeChar = 't';
                    break;

                default:
                    continue;
            }

            source.Slice(from, i - from).CopyTo(destination);
            destination = destination.Slice(i - from);
            from = i + 1;
            destination[0] = '\\';
            destination[1] = escapeChar;
            destination = destination.Slice(2);
        }

        if (from != source.Length)
        {
            source.Slice(from, source.Length - from).CopyTo(destination);
            destination = destination.Slice(source.Length - from);
        }

        return buffer.Slice(0, buffer.Length - destination.Length).ToString();
        // return new string();
    }

    public static object? ConvertDefaultValue(object defaultValue, string typeName)
    {
        try
        {
            var defaultValueTypeName = VisceralHelper.Primitives_ShortenName(defaultValue.GetType().FullName);
            if (defaultValueTypeName == typeName)
            {// Type matched
                if (defaultValueTypeName == "string")
                {
                    return GetEscapedString((string)defaultValue);
                }

                return defaultValue;
            }

            if (typeName == "sbyte")
            {
                return Convert.ToSByte(defaultValue, CultureInfo.InvariantCulture);
            }
            else if (typeName == "byte")
            {
                return Convert.ToByte(defaultValue, CultureInfo.InvariantCulture);
            }
            else if (typeName == "short")
            {
                return Convert.ToInt16(defaultValue, CultureInfo.InvariantCulture);
            }
            else if (typeName == "ushort")
            {
                return Convert.ToUInt16(defaultValue, CultureInfo.InvariantCulture);
            }
            else if (typeName == "int")
            {
                return Convert.ToInt32(defaultValue, CultureInfo.InvariantCulture);
            }
            else if (typeName == "uint")
            {
                return Convert.ToUInt32(defaultValue, CultureInfo.InvariantCulture);
            }
            else if (typeName == "long")
            {
                return Convert.ToInt64(defaultValue, CultureInfo.InvariantCulture);
            }
            else if (typeName == "ulong")
            {
                return Convert.ToUInt64(defaultValue, CultureInfo.InvariantCulture);
            }
            else if (typeName == "float")
            {
                return Convert.ToSingle(defaultValue, CultureInfo.InvariantCulture);
            }
            else if (typeName == "double")
            {
                return Convert.ToDouble(defaultValue, CultureInfo.InvariantCulture);
            }
            else if (typeName == "decimal")
            {
                // return decimal.Parse(ds);
                return Convert.ToDecimal(defaultValue, CultureInfo.InvariantCulture);
            }
            else
            {
                return null;
            }
        }
        catch
        {
            return null;
        }
    }

    public static bool IsDefaultableType(Type type)
    {
        if (type.IsGenericType)
        {
            var definitionType = type.GetGenericTypeDefinition();
            var argumentType = type.GetGenericArguments()[0];
            if (definitionType == typeof(Nullable<>))
            {
                return IsDefaultableType(argumentType);
            }
            else
            {
                return false;
            }
        }

        if (type == typeof(bool) || type == typeof(sbyte) || type == typeof(byte) ||
            type == typeof(short) || type == typeof(ushort) || type == typeof(int) ||
            type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
            type == typeof(float) || type == typeof(double) || type == typeof(decimal) ||
            type == typeof(string) || type == typeof(char))
        {
            return true;
        }

        return false;
    }

    public static bool IsDefaultableType(string fullName) => fullName switch
    {
        "bool" => true,
        "bool?" => true,
        "sbyte" => true,
        "sbyte?" => true,
        "byte" => true,
        "byte?" => true,
        "short" => true,
        "short?" => true,
        "ushort" => true,
        "ushort?" => true,
        "int" => true,
        "int?" => true,
        "uint" => true,
        "uint?" => true,
        "long" => true,
        "long?" => true,
        "ulong" => true,
        "ulong?" => true,
        "float" => true,
        "float?" => true,
        "double" => true,
        "double?" => true,
        "decimal" => true,
        "decimal?" => true,
        "string" => true,
        "string?" => true,
        "char" => true,
        "char?" => true,
        _ => false,
    };

    public static bool IsEnumUnderlyingType(string fullName) => fullName switch
    {
        "sbyte" => true,
        "byte" => true,
        "short" => true,
        "ushort" => true,
        "int" => true,
        "uint" => true,
        "long" => true,
        "ulong" => true,
        _ => false,
    };

    public static string? DefaultValueToString(object? obj)
    {
        if (obj == null)
        {
            return "null";
        }

        var type = obj.GetType();
        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var arg = type.GetGenericArguments()[0];
            if (definition == typeof(Nullable<>))
            {
                return DefaultValueToString(Convert.ChangeType(obj, arg));
            }
            else
            {
                return null;
            }
        }

        if (type == typeof(sbyte) || type == typeof(byte) ||
            type == typeof(short) || type == typeof(ushort) || type == typeof(int) ||
            type == typeof(EnumString))
        {
            return obj.ToString();
        }
        else if (type == typeof(bool))
        {
            if ((bool)obj == true)
            {
                return "true";
            }
            else
            {
                return "false";
            }
        }
        else if (type == typeof(uint))
        {
            return obj.ToString() + "u";
        }
        else if (type == typeof(long))
        {
            return obj.ToString() + "L";
        }
        else if (type == typeof(ulong))
        {
            return obj.ToString() + "ul";
        }
        else if (obj is float f)
        {
            return f.ToString(CultureInfo.InvariantCulture) + "f";
        }
        else if (obj is double d)
        {
            return d.ToString(CultureInfo.InvariantCulture) + "d";
        }
        else if (obj is decimal d2)
        {
            return d2.ToString(CultureInfo.InvariantCulture) + "m";
        }
        else if (type == typeof(string))
        {
            return "\"" + obj.ToString() + "\"";
        }
        else if (type == typeof(char))
        {
            return "'" + obj.ToString() + "'";
        }
        else
        {
            return null;
        }
    }
}

public class EnumString
{
    public EnumString(string name)
    {
        this.Name = name;
    }

    public string Name { get; }

    public override string ToString() => this.Name;
}
