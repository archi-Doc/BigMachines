﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Arc.Visceral;
using Microsoft.CodeAnalysis;
using Tinyhand;
using Tinyhand.Generator;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines.Generator;

public enum DeclarationCondition
{
    NotDeclared, // Not declared
    ImplicitlyDeclared, // declared (implicitly)
    ExplicitlyDeclared, // declared (explicitly interface)
}

[Flags]
public enum BigMachinesObjectFlag
{
    Configured = 1 << 0,
    RelationConfigured = 1 << 1,
    Checked = 1 << 2,

    BigMachineObject = 1 << 3,
    MachineObject = 1 << 4,
    TinyhandObject = 1 << 5,

    CanCreateInstance = 1 << 12, // Can create an instance
    HasRegisterBM = 1 << 13, // RegisterBM() declared
    StructualEnabled = 1 << 14, // Tinyhand structual
}

public class BigMachinesObject : VisceralObjectBase<BigMachinesObject>
{
    public BigMachinesObject()
    {
    }

    public new BigMachinesBody Body => (BigMachinesBody)((VisceralObjectBase<BigMachinesObject>)this).Body;

    public BigMachinesObjectFlag ObjectFlag { get; set; }

    public MachineObjectAttributeMock? ObjectAttribute { get; internal set; }

    public TinyhandObjectAttributeMock? TinyhandAttribute { get; private set; }

    public BigMachinesObject? MachineObject { get; private set; }

    public BigMachinesObject? IdentifierObject { get; internal set; }

    public bool UseServiceProvider { get; private set; } = false;

    public string StateName { get; private set; } = string.Empty;

    public string CommandName { get; private set; } = string.Empty;

    public List<StateMethod>? StateMethodList { get; private set; }

    public List<CommandMethod>? CommandMethodList { get; private set; }

    public string? LoaderIdentifier { get; private set; }

    public int LoaderNumber { get; private set; } = -1;

    public bool IsAbstractOrInterface => this.Kind == VisceralObjectKind.Interface || (this.symbol is INamedTypeSymbol nts && nts.IsAbstract);

    public List<BigMachinesObject>? Children { get; private set; } // The opposite of ContainingObject

    public List<BigMachinesObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

    public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

    public int GenericsNumber { get; private set; }

    public string GenericsNumberString => this.GenericsNumber > 1 ? this.GenericsNumber.ToString() : string.Empty;

    public BigMachinesObject? ClosedGenericHint { get; private set; }

    public string NewIfDerived { get; private set; } = string.Empty;

    public string OverrideOrNew { get; private set; } = string.Empty;

    public Arc.Visceral.NullableAnnotation NullableAnnotationIfReferenceType
    {
        get
        {
            if (this.TypeObject?.Kind.IsReferenceType() == true)
            {
                if (this.symbol is IFieldSymbol fs)
                {
                    return (Arc.Visceral.NullableAnnotation)fs.NullableAnnotation;
                }
                else if (this.symbol is IPropertySymbol ps)
                {
                    return (Arc.Visceral.NullableAnnotation)ps.NullableAnnotation;
                }
            }

            return Arc.Visceral.NullableAnnotation.None;
        }
    }

    public string QuestionMarkIfReferenceType
    {
        get
        {
            if (this.Kind.IsReferenceType())
            {
                return "?";
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public void Configure()
    {
        if (this.ObjectFlag.HasFlag(BigMachinesObjectFlag.Configured))
        {
            return;
        }

        this.ObjectFlag |= BigMachinesObjectFlag.Configured;

        // Open generic type is not supported.
        /*var genericsType = this.Generics_Kind;
        if (genericsType == VisceralGenericsKind.OpenGeneric)
        {
            return;
        }*/

        if (this.Generics_Kind == VisceralGenericsKind.ClosedGeneric)
        {
            if (this.OriginalDefinition != null && this.OriginalDefinition.ClosedGenericHint == null)
            {
                this.OriginalDefinition.ClosedGenericHint = this;
            }
        }

        foreach (var attribute in this.AllAttributes)
        {
            if (attribute.FullName == MachineObjectAttributeMock.FullName)
            {// MachineObjectAttribute
                // this.Location = attribute.Location;
                try
                {
                    this.ObjectAttribute = MachineObjectAttributeMock.FromArray(attribute.ConstructorArguments, attribute.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(BigMachinesBody.Error_AttributePropertyError, attribute.Location);
                }
            }
            else if (attribute.FullName == TinyhandObjectAttributeMock.FullName)
            {// TinyhandObjectAttribute
                try
                {
                    this.TinyhandAttribute = TinyhandObjectAttributeMock.FromArray(attribute.ConstructorArguments, attribute.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(BigMachinesBody.Error_AttributePropertyError, attribute.Location);
                }

                this.ObjectFlag |= BigMachinesObjectFlag.TinyhandObject;
                if (this.TinyhandAttribute?.Structual == true)
                {
                    this.ObjectFlag |= BigMachinesObjectFlag.StructualEnabled;
                }
            }
        }

        if (this.ObjectAttribute != null)
        {
            this.ConfigureObject();
        }
    }

    private void ConfigureObject()
    {
        if (this.ObjectAttribute is null)
        {
            return;
        }

        // Used keywords
        this.Identifier = new VisceralIdentifier("__gen_bm_identifier__");
        foreach (var x in this.AllMembers.Where(a => a.ContainingObject == this))
        {
            this.Identifier.Add(x.SimpleName);
        }

        // Default constructor
        if (this.ObjectAttribute.UseServiceProvider == true ||
            this.TinyhandAttribute?.UseServiceProvider == true)
        {// ServiceProvider
            this.UseServiceProvider = true;
        }
        else
        {
            if (!this.HasDefaultConstructor())
            {// Default constructor required.
                this.Body.ReportDiagnostic(BigMachinesBody.Error_NoDefaultConstructor, this.Location);
            }
        }

        // Machine<TIdentifier>
        var machineObject = this.BaseObject;
        var derivedMachine = false;
        while (machineObject != null)
        {
            if (machineObject.OriginalDefinition?.FullName == $"{BigMachinesBody.BigMachineNamespace}.Machine<TIdentifier>")
            {
                break;
            }
            else if (machineObject.OriginalDefinition?.FullName == $"{BigMachinesBody.BigMachineNamespace}.Machine")
            {
                break;
            }
            else
            {
                machineObject.Configure();
                if (machineObject.ObjectAttribute != null)
                {
                    derivedMachine = true;
                }
            }

            machineObject = machineObject.BaseObject;
        }

        if (machineObject is not null)
        {
            this.MachineObject = machineObject;
            this.StateName = this.FullName + "." + BigMachinesBody.StateIdentifier;
            this.CommandName = this.FullName + "." + BigMachinesBody.CommandIdentifier;

            if (machineObject.Generics_Arguments.Length == 1)
            {
                this.IdentifierObject = machineObject.Generics_Arguments[0];
            }
        }

        if (derivedMachine)
        {
            this.NewIfDerived = "new ";
            this.OverrideOrNew = "new";
        }
        else
        {
            this.OverrideOrNew = "override";
        }

        // MachineControl
        if (this.ObjectAttribute.Control == MachineControlKind.Single)
        {// Single
        }
        else if (this.ObjectAttribute.Control == MachineControlKind.Unordered ||
            this.ObjectAttribute.Control == MachineControlKind.Sequential)
        {// Multi
            if (this.IdentifierObject is null)
            {// Change to the single control.
                this.ObjectAttribute.Control = MachineControlKind.Single;
                this.Body.AddDiagnostic(BigMachinesBody.Warning_MachineWithoutIdentifier, this.Location);
            }
        }
        else
        {// Default
            if (this.IdentifierObject is null)
            {
                this.ObjectAttribute.Control = MachineControlKind.Single;
            }
            else
            {
                this.ObjectAttribute.Control = MachineControlKind.Unordered;
            }
        }

        // Machine id
        /*if (this.ObjectAttribute.MachineId == 0)
        {
            this.ObjectAttribute.MachineId = (uint)FarmHash.Hash64(this.FullName);
        }*/
    }

    public void ConfigureRelation()
    {// Create an object tree.
        if (this.ObjectFlag.HasFlag(BigMachinesObjectFlag.RelationConfigured))
        {
            return;
        }

        this.ObjectFlag |= BigMachinesObjectFlag.RelationConfigured;

        if (!this.Kind.IsType())
        {// Not type
            return;
        }

        var cf = this.OriginalDefinition;
        if (cf == null)
        {
            return;
        }
        else if (cf != this)
        {
            cf.ConfigureRelation();
        }

        if (cf.ContainingObject == null)
        {// Root object
            List<BigMachinesObject>? list;
            if (!this.Body.Namespaces.TryGetValue(this.Namespace, out list))
            {// Create a new namespace.
                list = new();
                this.Body.Namespaces[this.Namespace] = list;
            }

            if (!list.Contains(cf))
            {
                list.Add(cf);
            }
        }
        else
        {// Child object
            var parent = cf.ContainingObject;
            parent.ConfigureRelation();
            if (parent.Children == null)
            {
                parent.Children = new();
            }

            if (!parent.Children.Contains(cf))
            {
                parent.Children.Add(cf);
            }
        }

        if (cf.ConstructedObjects == null)
        {
            cf.ConstructedObjects = new();
        }

        if (!cf.ConstructedObjects.Contains(this))
        {
            cf.ConstructedObjects.Add(this);
            this.GenericsNumber = cf.ConstructedObjects.Count;
        }
    }

    public void CheckObject()
    {
        if (this.ObjectAttribute is null)
        {
            return;
        }

        if (!this.IsAbstractOrInterface)
        {
            this.ObjectFlag |= BigMachinesObjectFlag.CanCreateInstance;
        }

        /*if (this.Generics_Kind == VisceralGenericsKind.OpenGeneric)
        {
            this.Body.ReportDiagnostic(BigMachinesBody.Error_OpenGenericClass, this.Location, this.FullName);
            return;
        }*/

        // if (this.ObjectFlag.HasFlag(BigMachinesObjectFlag.CanCreateInstance))
        {// Type which can create an instance
            // partial class required.
            if (!this.IsPartial)
            {
                this.Body.ReportDiagnostic(BigMachinesBody.Error_NotPartial, this.Location, this.FullName);
            }

            // Parent class also needs to be a partial class.
            var parent = this.ContainingObject;
            while (parent != null)
            {
                if (!parent.IsPartial)
                {
                    this.Body.ReportDiagnostic(BigMachinesBody.Error_NotPartialParent, parent.Location, parent.FullName);
                }

                parent = parent.ContainingObject;
            }

            if (this.symbol?.DeclaredAccessibility == Accessibility.Private)
            {// Private machine
                this.Body.ReportDiagnostic(BigMachinesBody.Error_PrivateMachine, this.Location);
            }
        }

        /*if (this.ContainingObject is not null &&
            this.ObjectAttribute is not null)
        {
            this.ObjectAttribute.Private = true;
        }*/

        /*var id = this.ObjectAttribute!.MachineId;
        if (this.Body.Machines.ContainsKey(id))
        {
            this.Body.ReportDiagnostic(BigMachinesBody.Error_DuplicateTypeId, this.Location, id);
        }
        else
        {
            this.Body.Machines.Add(id, this);
        }*/

        if (this.MachineObject == null)
        {
            this.Body.ReportDiagnostic(BigMachinesBody.Error_NotDerived, this.Location);
            return;
        }

        if (this.IdentifierObject != null && this.IdentifierObject.Kind != VisceralObjectKind.TypeParameter)
        {
            if (this.IdentifierObject.Location.IsInSource)
            {
                if (!this.IdentifierObject.AllAttributes.Any(x => x.FullName == "Tinyhand.TinyhandObjectAttribute"))
                {
                    this.Body.AddDiagnostic(BigMachinesBody.Error_IdentifierIsNotSerializable, this.IdentifierObject.Location, this.IdentifierObject.FullName);
                }
            }
        }

        this.CheckKeyword(BigMachinesBody.StateIdentifier, this.Location);
        this.CheckKeyword(BigMachinesBody.InterfaceIdentifier, this.Location);
        this.CheckKeyword(BigMachinesBody.CreateInterfaceIdentifier, this.Location);
        this.CheckKeyword(BigMachinesBody.InternalRunIdentifier, this.Location);
        this.CheckKeyword(BigMachinesBody.ChangeState, this.Location);
        this.CheckKeyword(BigMachinesBody.GetCurrentState, this.Location);
        this.CheckKeyword(BigMachinesBody.InternalChangeState, this.Location);
        this.CheckKeyword(BigMachinesBody.InternalCommand, this.Location);
        this.CheckKeyword(BigMachinesBody.RegisterBM, this.Location);
        // this.CheckKeyword(BigMachinesBody.IntInitState, this.Location);
        this.CheckKeyword(BigMachinesBody.CommandIdentifier, this.Location);

        // State method, Command method
        this.StateMethodList = new();
        var idToStateMethod = new Dictionary<uint, StateMethod>();
        this.CommandMethodList = new();
        // var idToCommandMethod = new Dictionary<uint, CommandMethod>();
        foreach (var x in this.GetMembers(VisceralTarget.Method))
        {
            if (x.AllAttributes.FirstOrDefault(x => x.FullName == StateMethodAttributeMock.FullName) is { } attribute)
            {// State method
                var stateMethod = StateMethod.Create(this, x, attribute);
                if (stateMethod != null)
                {// Add
                    this.StateMethodList.Add(stateMethod);

                    if (idToStateMethod.TryGetValue(stateMethod.Id, out var s))
                    {// Duplicated
                        stateMethod.DuplicateId = true;
                        s.DuplicateId = true;
                    }
                    else
                    {
                        idToStateMethod.Add(stateMethod.Id, stateMethod);
                    }
                }
            }
            else if (x.AllAttributes.FirstOrDefault(x => x.FullName == CommandMethodAttributeMock.FullName) is { } attribute2)
            {// Command method
                var commandMethod = CommandMethod.Create(this, x, attribute2);
                if (commandMethod != null)
                {// Add
                    this.CommandMethodList.Add(commandMethod);
                    if (commandMethod.All &&
                        (this.ObjectAttribute.Control == MachineControlKind.Unordered ||
                        this.ObjectAttribute.Control == MachineControlKind.Sequential))
                    {
                        this.Body.AddAllCommand(this, commandMethod);
                    }

                    /*if (idToCommandMethod.TryGetValue(commandMethod.CommandId, out var s))
                    {// Duplicated
                        commandMethod.DuplicateId = true;
                        s.DuplicateId = true;
                    }
                    else
                    {
                        idToCommandMethod.Add(commandMethod.CommandId, commandMethod);
                    }*/
                }
            }
        }

        if (this.StateMethodList.Count > 0 && !idToStateMethod.ContainsKey(0))
        {// No default state method
            this.Body.AddDiagnostic(BigMachinesBody.Error_NoDefaultStateMethod, this.Location);
        }

        foreach (var x in this.StateMethodList.Where(a => a.DuplicateId))
        {// Duplicate state method
            this.Body.AddDiagnostic(BigMachinesBody.Error_DuplicateStateId, x.Location);
        }

        /*foreach (var x in this.CommandMethodList.Where(a => a.DuplicateId))
        {// Duplicate command method
            this.Body.AddDiagnostic(BigMachinesBody.Error_DuplicateCommandId, x.Location);
        }*/
    }

    public bool CheckKeyword(string keyword, Location? location = null)
    {
        if (!this.Identifier.Add(keyword))
        {
            this.Body.AddDiagnostic(BigMachinesBody.Error_KeywordUsed, location ?? Location.None, this.SimpleName, keyword);
            return false;
        }

        return true;
    }

    public void Check()
    {
        if (this.ObjectFlag.HasFlag(BigMachinesObjectFlag.Checked))
        {
            return;
        }

        if (this.Generics_Kind == VisceralGenericsKind.ClosedGeneric)
        {// Close generic is not necessary.
            return;
        }

        this.ObjectFlag |= BigMachinesObjectFlag.Checked;

        this.Body.DebugAssert(this.ObjectAttribute != null, "this.ObjectAttribute != null");
        this.CheckObject();
    }

    public static void GenerateLoader(ScopingStringBuilder ssb, GeneratorInformation info, BigMachinesObject? parent, List<BigMachinesObject> list)
    {
        if (parent?.Generics_Kind == VisceralGenericsKind.OpenGeneric)
        {
            return;
        }

        // var classFormat = "__gen__bm__{0:D4}";
        var list2 = list.SelectMany(x => x.ConstructedObjects).Where(x => x.ObjectAttribute != null).ToArray();

        /*if (list2.Length > 0 && list2[0].ContainingObject is { } containingObject)
        {// Add ModuleInitializerClass
            string? initializerClassName = null;
            if (containingObject.ClosedGenericHint != null)
            {// ClosedGenericHint
                initializerClassName = containingObject.ClosedGenericHint.FullName;
                goto ModuleInitializerClass_Added;
            }

            var constructedList = containingObject.ConstructedObjects;
            if (constructedList != null)
            {// Closed generic
                for (var n = 0; n < constructedList.Count; n++)
                {
                    if (constructedList[n].Generics_Kind != VisceralGenericsKind.OpenGeneric)
                    {
                        initializerClassName = constructedList[n].FullName;
                        goto ModuleInitializerClass_Added;
                    }
                }
            }

            // Open generic
            // (initializerClassName, _) = containingObject.GetClosedGenericName("object");

ModuleInitializerClass_Added:
            if (initializerClassName != null)
            {
                info.ModuleInitializerClass.Add(initializerClassName);
            }
        }*/

        /*string? loaderIdentifier = null;
        var list3 = list2.Where(x => x.ObjectFlag.HasFlag(BigMachinesObjectFlag.IsSimpleGenericMachine)).ToArray();
        if (list3.Length > 0)
        {
            ssb.AppendLine();
            if (parent == null)
            {
                loaderIdentifier = string.Format(classFormat, 0);
            }
            else
            {
                parent.LoaderNumber = info.FormatterCount++;
                loaderIdentifier = string.Format(classFormat, parent.LoaderNumber);
            }

            ssb.AppendLine($"public class {loaderIdentifier}<TIdentifier> : IMachineLoader<TIdentifier>");
            using (var scope = ssb.ScopeBrace($"    where TIdentifier : notnull"))
            {
                using (var scope2 = ssb.ScopeBrace("public void Load()"))
                {
                    foreach (var x in list3)
                    {
                        ssb.AppendLine($"{x.FullName}.RegisterBM({x.ObjectAttribute!.MachineId});");
                    }
                }
            }
        }*/

        if (parent != null)
        {
            parent.ObjectFlag |= BigMachinesObjectFlag.HasRegisterBM;
        }

        using (var m = ssb.ScopeBrace("internal static void RegisterBM()"))
        {
            foreach (var x in list2)
            {
                if (x.ObjectAttribute == null)
                {
                    continue;
                }

                if (x.Generics_Kind != VisceralGenericsKind.OpenGeneric)
                {// Register fixed types.
                    // ssb.AppendLine($"{x.FullName}.RegisterBM();"); // {x.ObjectAttribute.MachineId}
                    x.Generate_RegisterBM(ssb, info, false);
                }
            }

            foreach (var x in list.Where(a => a.ObjectFlag.HasFlag(BigMachinesObjectFlag.HasRegisterBM)))
            {// Children
                ssb.AppendLine($"{x.FullName}.RegisterBM();");
            }

            /*if (loaderIdentifier != null)
            {// Loader
                ssb.AppendLine($"MachineLoader.Add(typeof({loaderIdentifier}<>));");
            }*/
        }
    }

    internal void Generate(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.ConstructedObjects == null)
        {
            return;
        }

        /*else if (this.IsAbstractOrInterface)
        {
            return;
        }*/

        using (var cls = ssb.ScopeBrace($"{this.AccessibilityName} partial {this.KindName} {this.LocalName}"))
        {
            if (this.ObjectAttribute != null)
            {
                this.Generate2(ssb, info);
            }

            if (this.Children?.Count > 0)
            {// Generate children and loader.
                ssb.AppendLine();
                foreach (var x in this.Children)
                {
                    x.Generate(ssb, info);
                }

                ssb.AppendLine();
                GenerateLoader(ssb, info, this, this.Children);
            }
        }
    }

    internal void Generate2(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        this.Generate_State(ssb, info);
        this.Generate_InterfaceInstance(ssb, info);
        this.Generate_Interface(ssb, info);
        this.Generate_InternalRun(ssb, info);
        this.Generate_ChangeStateInternal(ssb, info);
        if (this.Generics_Kind == VisceralGenericsKind.OpenGeneric)
        {
            this.Generate_RegisterBM(ssb, info, true);
        }

        return;
    }

    internal void Generate_State(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.StateMethodList == null)
        {
            return;
        }

        using (var scope = ssb.ScopeBrace($"public {this.NewIfDerived}enum {BigMachinesBody.StateIdentifier} : uint"))
        {
            foreach (var x in this.StateMethodList)
            {
                ssb.AppendLine($"{x.Name} = {x.Id},");
            }
        }

        ssb.AppendLine();
    }

    internal void Generate_InterfaceInstance(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.MachineObject == null)
        {
            return;
        }

        string? controlType = default;
        if (this.ObjectAttribute?.Control == MachineControlKind.Single)
        {
            controlType = $"SingleMachineControl<{this.FullName}, {this.FullName}.Interface>";
        }
        else if (this.ObjectAttribute?.Control == MachineControlKind.Unordered)
        {
            if (this.IdentifierObject is not null)
            {
                controlType = $"UnorderedMachineControl<{this.IdentifierObject.FullName}, {this.FullName}, {this.FullName}.Interface>";
            }
        }
        else if (this.ObjectAttribute?.Control == MachineControlKind.Sequential)
        {
            if (this.IdentifierObject is not null)
            {
                controlType = $"SequentialMachineControl<{this.IdentifierObject.FullName}, {this.FullName}, {this.FullName}.Interface>";
            }
        }

        if (controlType is not null)
        {
            // ssb.AppendLine($"public override MachineControl MachineControl => ({controlType})this.__machineControl__;");
            // ssb.AppendLine($"public {this.OverrideOrNew} {controlType} MachineControl => ({controlType})this.__machineControl__;");
            ssb.AppendLine($"public {this.OverrideOrNew} {controlType}? MachineControl => this.__machineControl__ as {controlType};");
        }

        ssb.AppendLine($"public override ManMachineInterface InterfaceInstance => (Interface)(this.__interfaceInstance__ ??= new Interface(this));");

        // ssb.AppendLine($"public override ManMachineInterface CreateInterface() => new Interface(this);");

        /*if (this.FullName == "Advanced.DerivedMachine")
        {
            ssb.AppendLine($"public override Advanced.IntermittentMachine.Interface InterfaceInstance => (Interface)(this.__interfaceInstance__ ??= new Advanced.DerivedMachine.Interface(this));");
            // ssb.AppendLine($"public new Interface InterfaceInstance => (Interface)(this.__interfaceInstance__ ??= new Advanced.DerivedMachine.Interface(this));");
        }
        else
        {
            ssb.AppendLine($"public {this.OverrideOrNew} Interface InterfaceInstance => (Interface)(this.__interfaceInstance__ ??= new Interface(this));");
        }*/

        // ssb.AppendLine($"public {this.OverrideOrNew} Interface InterfaceInstance => (Interface)(this.__interfaceInstance__ ??= this.CreateInterface());");

        /*ssb.AppendLine("public override ManMachineInterface InterfaceInstance");
        ssb.AppendLine("{");
        ssb.IncrementIndent();

        ssb.AppendLine("get");
        ssb.AppendLine("{");
        ssb.IncrementIndent();

        ssb.AppendLine("if (this.__interfaceInstance__ is not Interface obj)");
        ssb.AppendLine("{");
        ssb.IncrementIndent();

        ssb.AppendLine("obj = new(this);");
        ssb.AppendLine("this.__interfaceInstance__ = obj;");

        ssb.DecrementIndent();
        ssb.AppendLine("}");

        ssb.AppendLine();
        ssb.AppendLine("return obj;");

        ssb.DecrementIndent();
        ssb.AppendLine("}");
        ssb.DecrementIndent();
        ssb.AppendLine("}");*/
    }

    internal void Generate_Interface(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.MachineObject == null)
        {
            return;
        }

        string interfaceName;
        if (this.IdentifierObject is null)
        {
            interfaceName = $"public {this.NewIfDerived}class Interface : ManMachineInterface<{this.StateName}>";
        }
        else
        {
            interfaceName = $"public {this.NewIfDerived}class Interface : ManMachineInterface<{this.IdentifierObject.FullName}, {this.StateName}>";
        }

        using (var scopeInterface = ssb.ScopeBrace(interfaceName))
        {
            if (this.FullName == "Advanced.DerivedMachine")
            {
                // ssb.AppendLine($"public Interface({this.LocalName} machine) : base(machine) {{ throw new Exception(); }}");
                ssb.AppendLine($"public Interface({this.LocalName} machine) : base(machine) {{ }}");
            }
            else
            {
                ssb.AppendLine($"public Interface({this.LocalName} machine) : base(machine) {{ }}");
            }

            // ssb.AppendLine($"private new {this.LocalName} Machine => ({this.LocalName})((ManMachineInterface)this).Machine;");

            this.Generate_CommandList(ssb, info);
        }

        ssb.AppendLine();
    }

    internal void Generate_CommandList(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.CommandMethodList is null || this.CommandMethodList.Count == 0)
        {
            return;
        }

        ssb.AppendLine($"public CommandList Command => new(({this.LocalName})this.Machine);");

        using (var scopeCommandList = ssb.ScopeBrace("public readonly struct CommandList"))
        {
            ssb.AppendLine($"public CommandList({this.LocalName} machine) {{ this.machine = machine; }}");
            ssb.AppendLine($"private readonly {this.LocalName} machine;");
            ssb.AppendLine();

            // Generate commands
            foreach (var x in this.CommandMethodList)
            {
                x.GenerateCommand(ssb, info);
            }
        }
    }

    internal void Generate_InternalRun(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.MachineObject == null || this.StateMethodList == null)
        {
            return;
        }

        using (var scope = ssb.ScopeBrace($"protected override Task<StateResult> {BigMachinesBody.InternalRunIdentifier}(StateParameter parameter)"))
        {
            ssb.AppendLine($"var state = Unsafe.As<int, {this.StateName}>(ref this.__machineState__);");
            ssb.AppendLine("return state switch");
            ssb.AppendLine("{");
            ssb.IncrementIndent();

            foreach (var x in this.StateMethodList)
            {
                if (x.ReturnTask)
                {
                    // ssb.AppendLine($"State.{x.Name} => await this.{x.Name}(parameter).ConfigureAwait(false),");
                    ssb.AppendLine($"State.{x.Name} => this.{x.Name}(parameter),");
                }
                else
                {
                    // ssb.AppendLine($"State.{x.Name} => this.{x.Name}(parameter),");
                    ssb.AppendLine($"State.{x.Name} => Task<StateResult>.FromResult(this.{x.Name}(parameter)),");
                }
            }

            ssb.AppendLine("_ => Task<StateResult>.FromResult(StateResult.Terminate),");
            ssb.DecrementIndent();
            ssb.AppendLine("};");
        }

        ssb.AppendLine();
    }

    internal void Generate_ChangeStateInternal(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.MachineObject == null || this.StateMethodList == null)
        {
            return;
        }

        using (var scope = ssb.ScopeBrace($"protected override ChangeStateResult {BigMachinesBody.InternalChangeState}(int state, bool rerun)"))
        {
            using (var scopeElse = ssb.ScopeBrace("if (this.__machineState__ == state)"))
            {
                ssb.AppendLine("return ChangeStateResult.Success;");
            }

            ssb.AppendLine();
            ssb.AppendLine($"var current = Unsafe.As<int, {this.StateName}>(ref this.__machineState__);");
            ssb.AppendLine("bool canExit = current switch");
            ssb.AppendLine("{");
            ssb.IncrementIndent();
            foreach (var x in this.StateMethodList)
            {
                if (x.CanExit)
                {
                    ssb.AppendLine($"State.{x.Name} => this.{x.Name}{StateMethod.CanExitName}(),");
                }
            }

            ssb.AppendLine("_ => true,");
            ssb.DecrementIndent();
            ssb.AppendLine("};");
            ssb.AppendLine();

            ssb.AppendLine($"var next = Unsafe.As<int, {this.StateName}>(ref state);");
            ssb.AppendLine("bool canEnter = next switch");
            ssb.AppendLine("{");
            ssb.IncrementIndent();
            foreach (var x in this.StateMethodList)
            {
                if (x.CanEnter)
                {
                    ssb.AppendLine($"State.{x.Name} => this.{x.Name}{StateMethod.CanEnterName}(),");
                }
                else
                {
                    ssb.AppendLine($"State.{x.Name} => true,");
                }
            }

            ssb.AppendLine("_ => false,");
            ssb.DecrementIndent();
            ssb.AppendLine("};");
            ssb.AppendLine();

            using (var scope2 = ssb.ScopeBrace("if (!canExit)"))
            {
                ssb.AppendLine("return ChangeStateResult.UnableToExit;");
            }

            using (var scope2 = ssb.ScopeBrace("else if (!canEnter)"))
            {
                ssb.AppendLine("return ChangeStateResult.UnableToEnter;");
            }

            using (var scope2 = ssb.ScopeBrace("else"))
            {
                ssb.AppendLine($"this.MachineState = state;");
                using (var scope3 = ssb.ScopeBrace("if (rerun)"))
                {
                    ssb.AppendLine($"this.__requestRerun__ = true;");
                }

                ssb.AppendLine();
                ssb.AppendLine("return ChangeStateResult.Success;");
            }
        }

        ssb.AppendLine();
        ssb.AppendLine($"protected ChangeStateResult ChangeState({this.StateName} state, bool rerun = false) => this.{BigMachinesBody.InternalChangeState}(Unsafe.As<{this.StateName}, int>(ref state), rerun);");
        ssb.AppendLine($"protected {this.NewIfDerived}{this.StateName} GetState() => Unsafe.As<int, {this.StateName}>(ref this.__machineState__);");
        ssb.AppendLine();
    }

    internal void Generate_RegisterBM(ScopingStringBuilder ssb, GeneratorInformation info, bool method)
    {
        if (this.MachineObject == null || this.ObjectAttribute == null)
        {
            return;
        }

        ScopingStringBuilder.IScope? scope = method ? ssb.ScopeBrace($"public static {this.NewIfDerived}void RegisterBM()") : null;

        // MachineInformation
        var machineType = $"typeof({this.FullName})";
        var constructor = this.UseServiceProvider ? "null" : $"() => new {this.FullName}()";
        var serializable = this.TinyhandAttribute is not null ? "true" : "false";
        var identifierType = this.IdentifierObject is not null ? $"typeof({this.IdentifierObject.FullName})" : "null";
        var numberOfTasks = this.ObjectAttribute.NumberOfTasks;
        ssb.AppendLine($"MachineRegistry.Register(new({machineType}, {constructor}, {serializable}, {identifierType}, {numberOfTasks}));");

        scope?.Dispose();
    }

    internal string[] Method_ParameterNames()
    {
        if (this.symbol is IMethodSymbol ms)
        {
            var names = new string[ms.Parameters.Length];
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = ms.Parameters[i].Name;
            }

            return names;
        }

        return Array.Empty<string>();
    }

    internal bool HasExplicitDefaultConstructor()
        => this.GetMembers(VisceralTarget.Method).Any(a => a.Method_IsConstructor && a.Method_Parameters.Length == 0 && a.ContainingObject == this && a.symbol?.IsImplicitlyDeclared == false);

    internal MachineObjectAttributeMock? TryGetObjectAttribute()
    {
        var attribute = this.ObjectAttribute;
        if (attribute is not null)
        {
            return attribute;
        }

        if (this.Generics_IsGeneric && this.symbol is INamedTypeSymbol nts)
        {
            var cf = this.Body.Add(nts.ConstructedFrom);
            if (cf is null)
            {
                return default;
            }

            cf.Configure();
            return cf.ObjectAttribute;
        }

        return default;
    }
}
