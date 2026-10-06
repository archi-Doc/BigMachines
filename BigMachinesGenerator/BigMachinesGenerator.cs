// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable RS1036

namespace BigMachines.Generator;

[Generator]
public class BigMachinesGeneratorV2 : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var roots = GetAttributedTypes(context, BigMachineObjectAttributeMock.FullName);
        var machines = GetAttributedTypes(context, MachineObjectAttributeMock.FullName);
        var options = GetAttributedTypes(context, BigMachinesGeneratorOptionAttributeMock.FullName);
        var provider = context.CompilationProvider.Combine(roots).Combine(machines).Combine(options);

        context.RegisterImplementationSourceOutput(provider, static (context, source) =>
            Emit(context, source.Left.Left.Left, source.Left.Left.Right, source.Left.Right, source.Right));
    }

    private static IncrementalValueProvider<ImmutableArray<INamedTypeSymbol>> GetAttributedTypes(IncrementalGeneratorInitializationContext context, string metadataName)
        => context.SyntaxProvider.ForAttributeWithMetadataName(
            metadataName,
            static (node, _) => node is TypeDeclarationSyntax,
            static (context, _) => (INamedTypeSymbol)context.TargetSymbol).Collect();

    private static void Emit(SourceProductionContext context, Compilation compilation, ImmutableArray<INamedTypeSymbol> roots, ImmutableArray<INamedTypeSymbol> machines, ImmutableArray<INamedTypeSymbol> optionTypes)
    {
        if (roots.IsEmpty && machines.IsEmpty && optionTypes.IsEmpty)
        {
            return;
        }

        // Generator instances are shared across runs and compilations, so options are collected per run.
        var options = new GeneratorOptions();
        options.AssemblyName = compilation.AssemblyName ?? string.Empty;
        options.OutputKind = compilation.Options.OutputKind;

        var body = new BigMachinesBody(context);
        if (!optionTypes.IsEmpty)
        {
            var attributeSymbol = compilation.GetTypeByMetadataName(BigMachinesGeneratorOptionAttributeMock.FullName);
            foreach (var attribute in optionTypes[0].GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeSymbol))
                {
                    var va = new VisceralAttribute(BigMachinesGeneratorOptionAttributeMock.FullName, attribute);
                    var ta = BigMachinesGeneratorOptionAttributeMock.FromArray(va.ConstructorArguments, va.NamedArguments);
                    options.AttachDebugger = ta.AttachDebugger;
                    options.GenerateToFile = ta.GenerateToFile;
                    options.CustomNamespace = ta.CustomNamespace;
                    options.UseModuleInitializer = ta.UseModuleInitializer;
                    options.TargetFolder = System.IO.Path.GetDirectoryName(attribute.ApplicationSyntaxReference?.SyntaxTree.FilePath) is { Length: > 0 } directory ? System.IO.Path.Combine(directory, "Generated") : null;
                    break;
                }
            }
        }

        AddTypes(roots, BigMachinesObjectFlag.BigMachineObject);
        AddTypes(machines, BigMachinesObjectFlag.MachineObject);

        context.CancellationToken.ThrowIfCancellationRequested();
        body.Prepare();
        if (body.Abort)
        {
            return;
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        body.Generate(options, context.CancellationToken);

        void AddTypes(ImmutableArray<INamedTypeSymbol> types, BigMachinesObjectFlag flag)
        {
            foreach (var type in types)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                if (body.Add(type) is { } obj)
                {
                    obj.ObjectFlag |= flag;
                }
            }
        }
    }

    private sealed class GeneratorOptions : IGeneratorInformation
    {
        public bool AttachDebugger { get; set; }

        public bool GenerateToFile { get; set; }

        public string? CustomNamespace { get; set; }

        public bool UseModuleInitializer { get; set; } = true;

        public string? AssemblyName { get; set; }

        public int AssemblyId { get; set; }

        public OutputKind OutputKind { get; set; }

        public string? TargetFolder { get; set; }
    }
}

/* [Generator]
public class BigMachinesGenerator : ISourceGenerator, IGeneratorInformation
{
    public bool AttachDebugger { get; private set; } = false;

    public bool GenerateToFile { get; private set; } = false;

    public string? CustomNamespace { get; private set; }

    public string? AssemblyName { get; private set; }

    public int AssemblyId { get; private set; }

    public OutputKind OutputKind { get; private set; }

    public string? TargetFolder { get; private set; }

    public GeneratorExecutionContext Context { get; private set; }

    private BigMachinesBody body = default!;
    private INamedTypeSymbol? machineObjectAttributeSymbol;
    private INamedTypeSymbol? bigMachinesGeneratorOptionAttributeSymbol;
#pragma warning disable RS1024
    private HashSet<INamedTypeSymbol?> processedSymbol = new();
#pragma warning restore RS1024

    static BigMachinesGenerator()
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            this.Context = context;

            if (!(context.SyntaxReceiver is BigMachinesSyntaxReceiver receiver))
            {
                return;
            }

            var compilation = context.Compilation;

            this.machineObjectAttributeSymbol = compilation.GetTypeByMetadataName(MachineObjectAttributeMock.FullName);
            if (this.machineObjectAttributeSymbol == null)
            {
                return;
            }

            this.bigMachinesGeneratorOptionAttributeSymbol = compilation.GetTypeByMetadataName(BigMachinesGeneratorOptionAttributeMock.FullName);
            if (this.bigMachinesGeneratorOptionAttributeSymbol == null)
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();
            this.ProcessGeneratorOption(receiver, compilation);
            if (this.AttachDebugger)
            {
                System.Diagnostics.Debugger.Launch();
            }

            context.CancellationToken.ThrowIfCancellationRequested();
            this.Prepare(context, compilation);

            this.body = new BigMachinesBody(context);
            // receiver.Generics.Prepare(compilation);

            // IN: type declaration
            context.CancellationToken.ThrowIfCancellationRequested();
            foreach (var x in receiver.CandidateSet)
            {
                var model = compilation.GetSemanticModel(x.SyntaxTree);
                if (model.GetDeclaredSymbol(x) is INamedTypeSymbol s)
                {
                    this.ProcessSymbol(s);
                }
            }

            context.CancellationToken.ThrowIfCancellationRequested();
            this.body.Prepare();
            if (this.body.Abort)
            {
                return;
            }

            this.body.Generate(this, context.CancellationToken);
        }
        catch
        {
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        // System.Diagnostics.Debugger.Launch();

        context.RegisterForSyntaxNotifications(() => new BigMachinesSyntaxReceiver());
    }

    private void ProcessSymbol(INamedTypeSymbol s)
    {
        if (this.processedSymbol.Contains(s))
        {
            return;
        }

        this.processedSymbol.Add(s);
        foreach (var x in s.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(x.AttributeClass, this.machineObjectAttributeSymbol))
            { // MachineObject
                var obj = this.body.Add(s);
                break;
            }
        }
    }

    private void Prepare(GeneratorExecutionContext context, Compilation compilation)
    {
        this.AssemblyName = compilation.AssemblyName ?? string.Empty;
        this.AssemblyId = this.AssemblyName.GetHashCode();
        this.OutputKind = compilation.Options.OutputKind;
    }

    private void ProcessGeneratorOption(BigMachinesSyntaxReceiver receiver, Compilation compilation)
    {
        if (receiver.GeneratorOptionSyntax == null)
        {
            return;
        }

        var model = compilation.GetSemanticModel(receiver.GeneratorOptionSyntax.SyntaxTree);
        if (model.GetDeclaredSymbol(receiver.GeneratorOptionSyntax) is INamedTypeSymbol s)
        {
            var attr = s.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, this.bigMachinesGeneratorOptionAttributeSymbol));
            if (attr != null)
            {
                var va = new VisceralAttribute(BigMachinesGeneratorOptionAttributeMock.FullName, attr);
                var ta = BigMachinesGeneratorOptionAttributeMock.FromArray(va.ConstructorArguments, va.NamedArguments);

                this.AttachDebugger = ta.AttachDebugger;
                this.GenerateToFile = ta.GenerateToFile;
                this.CustomNamespace = ta.CustomNamespace;
                this.TargetFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(receiver.GeneratorOptionSyntax.SyntaxTree.FilePath), "Generated");
            }
        }
    }

    internal class BigMachinesSyntaxReceiver : ISyntaxReceiver
    {
        public TypeDeclarationSyntax? GeneratorOptionSyntax { get; private set; }

        public HashSet<TypeDeclarationSyntax> CandidateSet { get; } = new HashSet<TypeDeclarationSyntax>();

        // public VisceralGenerics Generics { get; } = new VisceralGenerics();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is TypeDeclarationSyntax typeSyntax)
            {// Our target is a type syntax.
                if (this.CheckAttribute(typeSyntax))
                {// If the type has the specific attribute.
                    this.CandidateSet.Add(typeSyntax);
                }
            }
        }

        /// <summary>
        /// Returns true if the Type Sytax contains the specific attribute.
        /// </summary>
        /// <param name="typeSyntax">A type syntax.</param>
        /// <returns>True if the Type Sytax contains the specific attribute.</returns>
        private bool CheckAttribute(TypeDeclarationSyntax typeSyntax)
        {
            foreach (var attributeList in typeSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var name = attribute.Name.ToString();
                    if (this.GeneratorOptionSyntax == null)
                    {
                        if (name.EndsWith(BigMachinesGeneratorOptionAttributeMock.StandardName) || name.EndsWith(BigMachinesGeneratorOptionAttributeMock.SimpleName))
                        {
                            this.GeneratorOptionSyntax = typeSyntax;
                        }
                    }

                    if (name.EndsWith(MachineObjectAttributeMock.StandardName) ||
                        name.EndsWith(MachineObjectAttributeMock.SimpleName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}*/
