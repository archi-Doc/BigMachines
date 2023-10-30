// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS2008
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1117 // Parameters should be on same line or separate lines

namespace BigMachines.Generator;

public class BigMachinesBody : VisceralBody<BigMachinesObject>
{
    public const string BigMachineNamespace = "BigMachines";
    public const string DefaultBigMachineObject = "BigMachine";
    public const string BigMachineIdentifier = "BigMachine";
    public const string IMachineGroupIdentifier = "IMachineGroup<TIdentifier>";
    public const string StateIdentifier = "State";
    public const string InterfaceIdentifier = "Interface";
    public const string CreateInterfaceIdentifier = "CreateInterface";
    public const string InternalRunIdentifier = "InternalRun";
    public const string ChangeState = "ChangeState";
    public const string GetCurrentState = "GetCurrentState";
    public const string InternalChangeState = "InternalChangeState";
    public const string InternalCommand = "InternalCommand";
    public const string IntInitState = "IntInitState";
    public const string RegisterBM = "RegisterBM";
    public const string CommandIdentifier = "Command";

    public const string StateResultFullName = BigMachineNamespace + ".StateResult";
    public const string StateParameterFullName = BigMachineNamespace + ".StateParameter";
    public const string CommandResultResultFullName = BigMachineNamespace + ".CommandResult";
    public const string CommandResultResultFullName2 = BigMachineNamespace + ".CommandResult<TResponse>";
    public const string TaskFullName = "System.Threading.Tasks.Task";
    public const string TaskFullName2 = "System.Threading.Tasks.Task<TResult>";
    public const string CommandParameterFullName = BigMachineNamespace + ".CommandPost<{0}>.Command";

    public static readonly DiagnosticDescriptor Error_NotPartial = new DiagnosticDescriptor(
        id: "BMG001", title: "Not a partial class", messageFormat: "The target class '{0} must be a partial class",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NotPartialParent = new DiagnosticDescriptor(
        id: "BMG002", title: "Not a partial class/struct", messageFormat: "Parent object '{0}' is not a partial class/struct",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_AttributePropertyError = new DiagnosticDescriptor(
        id: "BMG003", title: "Attribute property type error", messageFormat: "The argument specified does not match the type of the property",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_KeywordUsed = new DiagnosticDescriptor(
        id: "BMG004", title: "Keyword used", messageFormat: "The type '{0}' already contains a definition for '{1}'",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NotDerived = new DiagnosticDescriptor(
        id: "BMG005", title: "Not derived", messageFormat: "MachineObject '{0}' must be derived from Machine class",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_MethodFormat = new DiagnosticDescriptor(
        id: "BMG006", title: "Invalid method", messageFormat: "State method must be in the format of 'protected StateResult {0}(StateParameter parameter)' or 'protected Task<StateResult> {0}(StateParameter parameter)'",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_OpenGenericClass = new DiagnosticDescriptor(
        id: "BMG007", title: "Not closed generic", messageFormat: "MachineObject '{0}' is not a closed generic class",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_DuplicateTypeId = new DiagnosticDescriptor(
        id: "BMG008", title: "Duplicate TypeId", messageFormat: "Machine id '{0}' must be unique",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_DuplicateStateId = new DiagnosticDescriptor(
        id: "BMG009", title: "Duplicate state id", messageFormat: "State method id must be unique",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NoDefaultStateMethod = new DiagnosticDescriptor(
        id: "BMG010", title: "No default state method", messageFormat: "A default state method with an id of 0 is required",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_GroupType = new DiagnosticDescriptor(
        id: "BMG011", title: "Group type error", messageFormat: "Group must implement IMachineGroup<TIdentifier> interface",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_IdentifierIsNotSerializable = new DiagnosticDescriptor(
        id: "BMG012", title: "Identifier not serializable", messageFormat: "Identifier type '{0}' must be serializable (have TinyhandObject attribute)",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_DuplicateCommandId = new DiagnosticDescriptor(
        id: "BMG013", title: "Duplicate command id", messageFormat: "Command id must be unique",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_MethodFormat2 = new DiagnosticDescriptor(
        id: "BMG014", title: "Invalid method", messageFormat: "Command method must be in the format of 'CommandResult Method(any param)' or 'CommandResult<TResponse> Method(any param)' or 'Task<CommandResult> Method(any param)' or 'Task<CommandResult<TResponse>> Method(any param)'",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Warning_MachineWithoutIdentifier = new DiagnosticDescriptor(
        id: "BMG015", title: "Machine control", messageFormat: "'Since the target machine does not have an identifier, MachineControl will be changed to SingleMachineControl",
        category: "BigMachinesGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_DuplicateMachineControl = new DiagnosticDescriptor(
        id: "BMG016", title: "Duplicate machine control", messageFormat: "Machine control name '{0}' must be unique",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_NoDefaultConstructor = new DiagnosticDescriptor(
        id: "BMG017", title: "No default constructor", messageFormat: "Default constructor is required unless the UseServiceProvider property is set to true",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_PrivateMachine = new DiagnosticDescriptor(
        id: "BMG018", title: "Private machine", messageFormat: "Machines with private accessibility are not supported",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor Error_BigMachineClass = new DiagnosticDescriptor(
        id: "BMG019", title: "BigMachine class", messageFormat: "BigMachines must be a public class and defined at the top level",
        category: "BigMachinesGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public BigMachinesBody(GeneratorExecutionContext context)
        : base(context)
    {
    }

    public BigMachinesBody(SourceProductionContext context)
        : base(context)
    {
    }

    internal HashSet<BigMachine> BigMachines = new();

    // internal Dictionary<uint, BigMachinesObject> Machines = new();

    internal Dictionary<string, List<BigMachinesObject>> Namespaces = new();

    internal List<(BigMachinesObject Machine, CommandMethod CommandMethod)> AllCommands = new();

    public void AddAllCommand(BigMachinesObject machine, CommandMethod commandMethod)
    {
        this.AllCommands.Add(new(machine, commandMethod));
    }

    public void Prepare()
    {
        // Configure objects.
        var array = this.FullNameToObject.Values.Where(x => x.ObjectFlag.HasFlag(BigMachinesObjectFlag.MachineObject)).ToArray();
        foreach (var x in array)
        {
            x.Configure();
        }

        this.FlushDiagnostic();
        if (this.Abort)
        {
            return;
        }

        array = array.Where(x => x.ObjectAttribute != null).ToArray();
        foreach (var x in array)
        {
            x.ConfigureRelation();
        }

        // Check
        foreach (var x in array)
        {
            x.Check();
        }

        this.FlushDiagnostic();
        if (this.Abort)
        {
            return;
        }

        // BigMachines
        this.PrepareBigMachines();

        // Check
        foreach (var x in this.BigMachines)
        {
            x.Check();
        }

        this.FlushDiagnostic();
        if (this.Abort)
        {
            return;
        }
    }

    public void Generate(IGeneratorInformation generator, CancellationToken cancellationToken)
    {
        ScopingStringBuilder ssb = new();
        GeneratorInformation info = new();
        List<BigMachinesObject> rootObjects = new();

        // Namespace
        foreach (var x in this.Namespaces)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.GenerateHeader(ssb);
            var ns = ssb.ScopeNamespace(x.Key);

            rootObjects.AddRange(x.Value); // For loader generation

            var firstFlag = true;
            foreach (var y in x.Value)
            {
                if (!firstFlag)
                {
                    ssb.AppendLine();
                }

                firstFlag = false;

                y.Generate(ssb, info); // Primary objects
            }

            var result = ssb.Finalize();

            if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
            {
                this.StringToFile(result, Path.Combine(generator.TargetFolder, $"gen.{BigMachineNamespace}.{x.Key}.cs"));
            }
            else
            {
                this.Context?.AddSource($"gen.{BigMachineNamespace}.{x.Key}", SourceText.From(result, Encoding.UTF8));
                this.Context2?.AddSource($"gen.{BigMachineNamespace}.{x.Key}", SourceText.From(result, Encoding.UTF8));
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        this.GenerateLoader(generator, info, rootObjects);
        this.GenerateBigMachines(generator, info);

        this.FlushDiagnostic();
    }

    private void PrepareBigMachines()
    {
        BigMachine bigMachine;
        var defaultFullName = $"{BigMachinesBody.BigMachineNamespace}.{BigMachinesBody.DefaultBigMachineObject}";

        var array = this.FullNameToObject.Values.Where(x => x.ObjectFlag.HasFlag(BigMachinesObjectFlag.BigMachineObject)).ToArray();
        foreach (var x in array)
        {
            bigMachine = new BigMachine(this, x);
            this.BigMachines.Add(bigMachine);
        }

        bigMachine = this.BigMachines.FirstOrDefault(x => x.Object?.FullName == defaultFullName);
        if (bigMachine is not null)
        {// Override the default bigmachine.
            bigMachine.Default = true;
        }
        else
        {// Add the default bigmachine.
            var defaultBigMachine = new BigMachine(this, null);
            defaultBigMachine.Default = true;
            this.BigMachines.Add(defaultBigMachine);
        }

        foreach (var x in this.BigMachines)
        {
            x.AddMachines();
        }
    }

    private void GenerateHeader(ScopingStringBuilder ssb, bool tinyhand = false)
    {
        ssb.AddHeader("// <auto-generated/>");
        ssb.AddUsing("System");
        ssb.AddUsing("System.Collections.Generic");
        ssb.AddUsing("System.Diagnostics.CodeAnalysis");
        ssb.AddUsing("System.Linq");
        ssb.AddUsing("System.Runtime.CompilerServices");
        ssb.AddUsing("System.Threading.Tasks");
        ssb.AddUsing(BigMachineNamespace);
        ssb.AddUsing(BigMachineNamespace + ".Control");
        if (tinyhand)
        {
            ssb.AddUsing("Tinyhand");
            ssb.AddUsing("Tinyhand.IO");
        }

        ssb.AppendLine("#nullable enable", false);
        ssb.AppendLine("#pragma warning disable CS1591", false);
        ssb.AppendLine("#pragma warning disable CS1998", false);
        ssb.AppendLine();
    }

    private void GenerateLoader(IGeneratorInformation generator, GeneratorInformation info, List<BigMachinesObject> rootObjects)
    {
        var ssb = new ScopingStringBuilder();
        this.GenerateHeader(ssb);

        using (var scopeFormatter = ssb.ScopeNamespace($"{BigMachineNamespace}.Generator"))
        {
            using (var methods = ssb.ScopeBrace("static class Generated"))
            {
                info.FinalizeBlock(ssb);

                BigMachinesObject.GenerateLoader(ssb, info, null, rootObjects);
            }
        }

        this.GenerateInitializer(generator, ssb, info);

        var result = ssb.Finalize();

        if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
        {
            this.StringToFile(result, Path.Combine(generator.TargetFolder, "gen.BigMachinesGenerated.cs"));
        }
        else
        {
            this.Context?.AddSource($"gen.BigMachinesLoader", SourceText.From(result, Encoding.UTF8));
            this.Context2?.AddSource($"gen.BigMachinesLoader", SourceText.From(result, Encoding.UTF8));
        }
    }

    private void GenerateInitializer(IGeneratorInformation generator, ScopingStringBuilder ssb, GeneratorInformation info)
    {
        // Namespace
        var ns = BigMachineNamespace;
        var assemblyId = string.Empty; // Assembly ID
        if (!string.IsNullOrEmpty(generator.CustomNamespace))
        {// Custom namespace.
            ns = generator.CustomNamespace;
        }
        else
        {// Other (Apps)
            // assemblyId = "_" + generator.AssemblyId.ToString("x");
            if (!string.IsNullOrEmpty(generator.AssemblyName))
            {
                assemblyId = VisceralHelper.AssemblyNameToIdentifier("_" + generator.AssemblyName);
            }
        }

        info.ModuleInitializerClass.Add($"{BigMachinesBody.BigMachineNamespace}.Generator.Generated");

        ssb.AppendLine();
        using (var scopeCrossLink = ssb.ScopeNamespace(ns!))
        {
            using (var scopeClass = ssb.ScopeBrace("public static class BigMachinesModule" + assemblyId))
            {
                ssb.AppendLine("private static bool Initialized;");
                ssb.AppendLine();
                ssb.AppendLine("[ModuleInitializer]");

                using (var scopeMethod = ssb.ScopeBrace("public static void Initialize()"))
                {
                    ssb.AppendLine("if (Initialized) return;");
                    ssb.AppendLine("Initialized = true;");
                    ssb.AppendLine();

                    foreach (var x in info.ModuleInitializerClass)
                    {
                        ssb.Append(x, true);
                        ssb.AppendLine(".RegisterBM();", false);
                    }
                }
            }

            this.GenerateAllCommand(generator, ssb, info);
        }
    }

    private void GenerateAllCommand(IGeneratorInformation generator, ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.AllCommands.Count == 0)
        {
            return;
        }

        ssb.AppendLine();
        using (var scopeExtension = ssb.ScopeBrace("public static class AllCommandExtension"))
        {
            foreach (var x in this.AllCommands)
            {
                this.GenerateAllCommandMethod(ssb, info, x.Machine, x.CommandMethod);
            }
        }
    }

    private void GenerateAllCommandMethod(ScopingStringBuilder ssb, GeneratorInformation info, BigMachinesObject machine, CommandMethod commandMethod)
    {
        if (machine.IdentifierObject is null)
        {
            return;
        }

        var identifierType = machine.IdentifierObject.FullName;
        var interfaceType = machine.FullName + ".Interface";
        var responseType = commandMethod.ResponseObject?.FullName;
        var resultType = responseType is null ? $"IdentifierAndCommandResult<{identifierType}>" : $"IdentifierAndCommandResult<{identifierType}, {responseType}>";
        var param = string.IsNullOrEmpty(commandMethod.ParameterTypesAndNames) ? string.Empty : ", ";

        using (var scopeMethod = ssb.ScopeBrace($"public static async Task<{resultType}[]> All{commandMethod.Name}(this MultiMachineControl<{identifierType}, {interfaceType}> control{param}{commandMethod.ParameterTypesAndNames})"))
        {
            ssb.AppendLine("var machines = control.GetArray();");
            ssb.AppendLine($"var results = new {resultType}[machines.Length];");
            ssb.AppendLine($"for (var i = 0; i < machines.Length; i++) results[i] = new(machines[i].Identifier, await machines[i].Command.{commandMethod.Name}({commandMethod.ParameterNames}).ConfigureAwait(false));");
            ssb.AppendLine("return results;");
        }
    }

    private void GenerateBigMachines(IGeneratorInformation generator, GeneratorInformation info)
    {
        var ssb = new ScopingStringBuilder();
        this.GenerateHeader(ssb, true);

        foreach (var x in this.BigMachines)
        {
            using (var scopeNamespace = ssb.ScopeNamespace(x.Namespace))
            {
                x.Generate(ssb, info);
            }
        }

        var result = ssb.Finalize();

        if (generator.GenerateToFile && generator.TargetFolder != null && Directory.Exists(generator.TargetFolder))
        {
            this.StringToFile(result, Path.Combine(generator.TargetFolder, "gen.BigMachines.cs"));
        }
        else
        {
            this.Context?.AddSource($"gen.BigMachines", SourceText.From(result, Encoding.UTF8));
            this.Context2?.AddSource($"gen.BigMachines", SourceText.From(result, Encoding.UTF8));
        }
    }
}
