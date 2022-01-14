// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;

#pragma warning disable RS1024 // Compare symbols correctly
#pragma warning disable RS2008
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable SA1310 // Field names should not contain underscore

namespace Arc.Visceral;

public class VisceralBody<T>
    where T : VisceralObjectBase<T>, new()
{
    public VisceralBody(GeneratorExecutionContext? context)
    {
        this.Context = context;
    }

    public VisceralBody(SourceProductionContext? context)
    {
        this.Context2 = context;
    }

    public static readonly DiagnosticDescriptor Error_DebugAssert = new DiagnosticDescriptor(
        id: "TG000", title: "Debug.Assert()", messageFormat: "{0}",
        category: "TinyhandGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    [System.Diagnostics.Conditional("DEBUG")]
    public void DebugAssert(bool condition, string errorMessage)
    {// Use this method instead of Debug.Assert(). Debug.Assert() will bring up a lot of error dialogs.
        if (!condition)
        {
            this.ReportDiagnostic(Error_DebugAssert, Location.None, errorMessage);
        }
    }

    public void ReportDiagnostic(Diagnostic diagnostic)
    {
        this.Context?.ReportDiagnostic(diagnostic);
        this.Context2?.ReportDiagnostic(diagnostic);
        if (diagnostic.Severity == DiagnosticSeverity.Error)
        {
            this.Abort = true; // Abort the process if an error occurred.
        }
    }

    public void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs)
    {
        var diagnostic = Diagnostic.Create(descriptor, location, messageArgs);
        this.ReportDiagnostic(diagnostic);
    }

    public bool AddDiagnostic(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs)
    {
        if (descriptor.DefaultSeverity == DiagnosticSeverity.Error)
        {
            this.Abort = true; // Abort the process if an error occurred.
        }

        return this.DiagnosticItems.Add(new DiagnosticIdentification(descriptor, location, messageArgs));
    }

    public void FlushDiagnostic()
    {
        foreach (var x in this.DiagnosticItems)
        {
            var diagnostic = Diagnostic.Create(x.Descriptor, x.Location, x.Args);
            this.ReportDiagnostic(diagnostic);
        }

        this.DiagnosticItems.Clear();
    }

    public T? Add(T t)
    {
        if (!t.Kind.IsType())
        {
            return t;
        }

        t.GetRawInformation(out var symbol, out var type, out var memberInfo);
        this.FullNameToObject[t.FullName] = t;

        if (symbol != null)
        {
            this.SymbolToObject[symbol] = t;
        }

        if (type != null)
        {
            this.TypeToObject[type] = t;
        }

        return t;
    }

    public T? Add(ISymbol symbol)
    {
        if (this.TryGet(symbol, out var result))
        {// Search symbol.
            return result;
        }

        var fullName = this.SymbolToFullName(symbol);
        if (this.TryGet(fullName, out var result2))
        {// Search full name.
            return result2;
        }

        var t = new T();
        if (!t.Initialize(this, symbol, fullName))
        { // Failed.
            return null;
        }

        if (t.Kind.IsType())
        {
            this.SymbolToObject.Add(symbol, t);
            this.FullNameToObject.Add(fullName, t);
        }

        return t;
    }

    public T? Add(Type type)
    {
        if (this.TryGet(type, out var result))
        {// Search type.
            return result;
        }

        var fullName = VisceralHelper.TypeToFullName(type);
        if (this.TryGet(fullName, out var result2))
        {// Search full name.
            return result2;
        }

        var t = new T();
        if (!t.Initialize(this, type, fullName))
        { // Failed.
            return null;
        }

        if (t.Kind.IsType())
        {
            this.TypeToObject.Add(type, t);
            this.FullNameToObject.Add(fullName, t);
        }

        return t;
    }

    public T? Add(MemberInfo memberInfo)
    {
        if (memberInfo.MemberType == MemberTypes.TypeInfo || memberInfo.MemberType == MemberTypes.NestedType)
        {// Type
            return this.Add((Type)memberInfo);
        }

        var t = new T();
        if (!t.Initialize(this, memberInfo))
        { // Failed.
            return null;
        }

        return t;
    }

    public void StringToFile(string code, string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var fs = File.Create(path))
            {
                var b = Encoding.UTF8.GetBytes(code);
                fs.Write(b, 0, b.Length);
            }
        }
        catch
        {
        }
    }

    public GeneratorExecutionContext? Context { get; }

    public SourceProductionContext? Context2 { get; }

    public bool Abort { get; set; } // Set true to abort Prepare/Generate process.

    public string SymbolToSimpleName(ISymbol symbol)
    {
        string name;

        if (symbol is IMethodSymbol mb)
        {// Method
            if (mb.MethodKind == MethodKind.Constructor)
            {
                return mb.ContainingType.Name;
            }

            return mb.Name;
        }
        else if (symbol is IArrayTypeSymbol ats)
        {// Array []
            return this.SymbolToSimpleName(ats.ElementType);
        }
        else if (symbol is ITypeParameterSymbol tps)
        {// Generic parameter
            return tps.Name;
        }
        else if (symbol is INamedTypeSymbol nts)
        {
            if (nts.IsTupleType)
            {
                return "ValueTuple";
            }
            else if (nts.IsGenericType)
            {
                if (nts.Name == "Nullable" && nts.ContainingNamespace.Name == "System" && nts.TypeArguments.Length == 1)
                {// Nullable<T>
                    name = this.SymbolToName(nts.TypeArguments[0], NameFormat.None, false);
                    return VisceralHelper.Primitives_ShortenSimpleName(name) ?? name; // + "?";
                }
            }

            name = symbol.Name;
            return VisceralHelper.Primitives_ShortenSimpleName(name) ?? name;
        }

        return symbol.Name;
    }

    /*public string GetNamespaceAndClass(ISymbol symbol)
    {
        if (symbol.ContainingType == null)
        {
            return symbol.ContainingNamespace.ToDisplayString();
        }
        else
        {
            var containingType = symbol.ContainingType;

            var s = string.Empty;
            while (containingType != null)
            {
                symbol = containingType;
                s = "." + this.SymbolToLocalName(symbol) + s;
                containingType = symbol.ContainingType;
            }

            return symbol.ContainingNamespace.ToDisplayString() + s;
        }
    }

    public string GetContainingClassName(ISymbol symbol)
    {
        var containingType = symbol.ContainingType;
        var s = string.Empty;
        while (containingType != null)
        {
            s = "." + this.SymbolToLocalName(containingType) + s;
            containingType = containingType.ContainingType;
        }

        return s;
    }*/

    public string SymbolToFullName(ISymbol symbol, bool nullableAnnotation = false) => this.SymbolToName(symbol, NameFormat.AddNamespaceAndClass, nullableAnnotation);

    public string SymbolToRegionalName(ISymbol symbol, bool nullableAnnotation = false) => this.SymbolToName(symbol, NameFormat.AddClass, nullableAnnotation);

    public string SymbolToLocalName(ISymbol symbol, bool nullableAnnotation = false) => this.SymbolToName(symbol, NameFormat.None, nullableAnnotation);

    /*public SymbolDisplayFormat FormatDisplayName { get; } = new SymbolDisplayFormat(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    public SymbolDisplayFormat FormatFullName { get; } = new SymbolDisplayFormat(
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeParameters,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType
        );*/

    public bool TryGet(string fullName, out T result) => this.FullNameToObject.TryGetValue(fullName, out result);

    public bool TryGet(ISymbol symbol, out T result) => this.SymbolToObject.TryGetValue(symbol, out result);

    public bool TryGet(Type type, out T result) => this.TypeToObject.TryGetValue(type, out result);

    public Dictionary<string, T> FullNameToObject { get; } = new();

    public Dictionary<ISymbol, T> SymbolToObject { get; } = new();

    public Dictionary<Type, T> TypeToObject { get; } = new();

    private HashSet<DiagnosticIdentification> DiagnosticItems { get; } = new();

    internal readonly struct DiagnosticIdentification
    {
        public DiagnosticIdentification(DiagnosticDescriptor descriptor, Location? location, params object?[]? args)
        {
            this.Descriptor = descriptor;
            this.Location = location;
            this.Args = args;
        }

        public readonly DiagnosticDescriptor Descriptor;
        public readonly Location? Location;
        public readonly object?[]? Args;

        public override int GetHashCode()
        {
            var hash = this.Descriptor.GetHashCode();
            if (this.Location != null)
            {
                hash ^= this.Location.GetHashCode();
            }

            /*if (this.Args != null)
            {
                hash ^= this.Args.GetHashCode();
            }*/

            return hash;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != typeof(DiagnosticIdentification))
            {
                return false;
            }

            var x = (DiagnosticIdentification)obj;
            return this.Descriptor == x.Descriptor && this.Location == x.Location; // && this.Args == x.Args;
        }
    }

    private enum NameFormat
    {
        None, // Local
        AddClass, // Regional
        AddNamespaceAndClass, // Full
    }

    private string GetNamespaceClassPeriod(ISymbol symbol, NameFormat nameFormat)
    {
        if (nameFormat == NameFormat.None)
        {
            return string.Empty;
        }

        // Add Class
        var containingType = symbol.ContainingType;
        var result = string.Empty;
        while (containingType != null)
        {
            result = this.SymbolToLocalName(containingType) + "." + result;
            containingType = containingType.ContainingType;
        }

        if (nameFormat == NameFormat.AddNamespaceAndClass)
        {// Add Namespace
            var ns = symbol.ContainingNamespace?.ToDisplayString();
            if (ns != null && ns.Length > 0)
            {
                result = ns + "." + result;
            }
        }

        return result;
    }

    private string SymbolToName(ISymbol symbol, NameFormat nameFormat, bool addNullableAnnotation)
    {
        StringBuilder sb;

        if (symbol is IMethodSymbol mb)
        {// Method
            sb = new StringBuilder(this.GetNamespaceClassPeriod(symbol, nameFormat));

            if (mb.MethodKind == MethodKind.Constructor)
            {
                sb.Append(mb.ContainingType.Name);
            }
            else
            {
                sb.Append(mb.Name);
            }

            if (mb.IsGenericMethod)
            {
                sb.Append('<');
                for (var i = 0; i < mb.TypeArguments.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(this.SymbolToName(mb.TypeArguments[i], NameFormat.AddNamespaceAndClass, addNullableAnnotation));
                }

                sb.Append('>');
            }

            sb.Append('(');
            for (var i = 0; i < mb.Parameters.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(this.SymbolToName(mb.Parameters[i].Type, NameFormat.AddNamespaceAndClass, addNullableAnnotation));
            }

            sb.Append(')');

            return sb.ToString();
        }
        else if (symbol is IArrayTypeSymbol ats)
        {// Array []
            sb = new StringBuilder(this.SymbolToName(ats.ElementType, nameFormat, addNullableAnnotation));
            /*if (addNullableAnnotation && ats.ElementNullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated)
            {
                sb.Append('?');
            }*/

            sb.Append('[');
            for (var n = 1; n < ats.Rank; n++)
            {
                sb.Append(',');
            }

            sb.Append(']');

            if (addNullableAnnotation && ats.NullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated)
            {
                sb.Append('?');
            }

            return sb.ToString();
        }
        else if (symbol is ITypeParameterSymbol tps)
        {// Generic parameter
            if (addNullableAnnotation && tps.NullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated)
            {
                return tps.Name + "?";
            }
            else
            {
                return tps.Name;
            }
        }

        if (symbol is INamedTypeSymbol ts)
        { // NamedType
            if (ts.TypeArguments.Length == 0)
            {
                var name = ts.Name;
                if (addNullableAnnotation && ts.NullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated && ts.TypeKind.IsReferenceType())
                {
                    name += "?";
                }

                var shortName = VisceralHelper.Primitives_ShortenSimpleName(name);
                if (shortName != null)
                {
                    return shortName;
                }

                return this.GetNamespaceClassPeriod(symbol, nameFormat) + name;
            }

            if (ts.IsTupleType)
            {
                sb = new StringBuilder();

                sb.Append('(');
                for (var i = 0; i < ts.TypeArguments.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(this.SymbolToName(ts.TypeArguments[i], NameFormat.AddNamespaceAndClass, addNullableAnnotation));
                }

                sb.Append(')');
            }
            else if (ts.Name == "Nullable" && ts.ContainingNamespace.Name == "System" && ts.TypeArguments.Length == 1)
            {// Nullable<T>
                var name = this.SymbolToName(ts.TypeArguments[0], nameFormat, addNullableAnnotation);
                return (VisceralHelper.Primitives_ShortenName(name) ?? name) + "?";
            }
            else
            {// Generic Class
                sb = new StringBuilder(this.GetNamespaceClassPeriod(symbol, nameFormat));
                sb.Append(ts.Name);
                sb.Append('<');
                for (var i = 0; i < ts.TypeArguments.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(this.SymbolToName(ts.TypeArguments[i], NameFormat.AddNamespaceAndClass, addNullableAnnotation));
                }

                sb.Append('>');

                if (addNullableAnnotation && ts.NullableAnnotation == Microsoft.CodeAnalysis.NullableAnnotation.Annotated)
                {
                    sb.Append('?');
                }
            }

            return sb.ToString();
        }
        else
        {
            return this.GetNamespaceClassPeriod(symbol, nameFormat) + symbol.Name;
        }
    }
}
