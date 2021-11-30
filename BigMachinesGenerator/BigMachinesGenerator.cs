﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace BigMachines.Generator
{
    [Generator]
    public class BigMachinesGeneratorV2 : IIncrementalGenerator, IGeneratorInformation
    {
        public bool AttachDebugger { get; private set; }

        public bool GenerateToFile { get; private set; }

        public string? CustomNamespace { get; private set; }

        public string? AssemblyName { get; private set; }

        public int AssemblyId { get; private set; }

        public OutputKind OutputKind { get; private set; }

        public string? TargetFolder { get; private set; }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.CompilationProvider.Combine(
                context.SyntaxProvider
                .CreateSyntaxProvider(static (s, _) => IsSyntaxTargetForGeneration(s), static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Collect());

            context.RegisterImplementationSourceOutput(provider, this.Emit);
        }

        private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
            node is TypeDeclarationSyntax m && m.AttributeLists.Count > 0;

        private static TypeDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var typeSyntax = (TypeDeclarationSyntax)context.Node;
            foreach (var attributeList in typeSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var name = attribute.Name.ToString();
                    if (name.EndsWith(BigMachinesGeneratorOptionAttributeMock.StandardName) ||
                        name.EndsWith(BigMachinesGeneratorOptionAttributeMock.SimpleName))
                    {
                        return typeSyntax;
                    }
                    else if (name.EndsWith(MachineObjectAttributeMock.StandardName) ||
                        name.EndsWith(MachineObjectAttributeMock.SimpleName))
                    {
                        return typeSyntax;
                    }
                }
            }

            return null;
        }

        private void Emit(SourceProductionContext context, (Compilation compilation, ImmutableArray<TypeDeclarationSyntax?> types) source)
        {
            var compilation = source.compilation;
            var machineObjectAttributeSymbol = compilation.GetTypeByMetadataName(MachineObjectAttributeMock.FullName);
            if (machineObjectAttributeSymbol == null)
            {
                return;
            }

            var bigMachinesGeneratorOptionAttributeSymbol = compilation.GetTypeByMetadataName(BigMachinesGeneratorOptionAttributeMock.FullName);
            if (bigMachinesGeneratorOptionAttributeSymbol == null)
            {
                return;
            }

            this.AssemblyName = compilation.AssemblyName ?? string.Empty;
            this.AssemblyId = this.AssemblyName.GetHashCode();
            this.OutputKind = compilation.Options.OutputKind;

            var body = new BigMachinesBody(context);
#pragma warning disable RS1024 // Symbols should be compared for equality
            var processed = new HashSet<INamedTypeSymbol?>();
#pragma warning restore RS1024 // Symbols should be compared for equality

            var generatorOptionSet = false;
            foreach (var x in source.types)
            {
                if (x == null)
                {
                    continue;
                }

                context.CancellationToken.ThrowIfCancellationRequested();

                var model = compilation.GetSemanticModel(x.SyntaxTree);
                if (model.GetDeclaredSymbol(x) is INamedTypeSymbol s &&
                    !processed.Contains(s))
                {
                    processed.Add(s);
                    foreach (var y in s.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(y.AttributeClass, machineObjectAttributeSymbol))
                        { // MachineObject
                            body.Add(s);
                            break;
                        }
                        else if (!generatorOptionSet &&
                            SymbolEqualityComparer.Default.Equals(y.AttributeClass, bigMachinesGeneratorOptionAttributeSymbol))
                        {
                            generatorOptionSet = true;
                            var va = new VisceralAttribute(BigMachinesGeneratorOptionAttributeMock.FullName, y);
                            var ta = BigMachinesGeneratorOptionAttributeMock.FromArray(va.ConstructorArguments, va.NamedArguments);

                            this.AttachDebugger = ta.AttachDebugger;
                            this.GenerateToFile = ta.GenerateToFile;
                            this.CustomNamespace = ta.CustomNamespace;
                            this.TargetFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(x.SyntaxTree.FilePath), "Generated");
                        }
                    }
                }
            }

            context.CancellationToken.ThrowIfCancellationRequested();
            body.Prepare();
            if (body.Abort)
            {
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();
            body.Generate(this, context.CancellationToken);
        }
    }

    // [Generator]
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

                // IN: close generic (member, expression)
                /*foreach (var ts in receiver.Generics.ItemDictionary.Values.Where(a => a.GenericsKind == VisceralGenericsKind.ClosedGeneric).Select(a => a.TypeSymbol))
                {
                    if (ts != null)
                    {
                        this.ProcessSymbol(ts);
                    }
                }

                this.SalvageCloseGeneric(receiver.Generics);*/

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

                /*else if (syntaxNode is GenericNameSyntax genericSyntax)
                {// Generics
                    this.Generics.Add(genericSyntax);
                }*/
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
    }
}
