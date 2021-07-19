﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1405 // Debug.Assert should provide message text
#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines.Generator
{
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

        CanCreateInstance = 1 << 12, // Can create an instance
    }

    public class BigMachinesObject : VisceralObjectBase<BigMachinesObject>
    {
        public BigMachinesObject()
        {
        }

        public new BigMachinesBody Body => (BigMachinesBody)((VisceralObjectBase<BigMachinesObject>)this).Body;

        public BigMachinesObjectFlag ObjectFlag { get; private set; }

        public StateMachineAttributeMock? ObjectAttribute { get; private set; }

        public BigMachinesObject? MachineObject { get; private set; }

        public BigMachinesObject? IdentifierObject { get; private set; }

        public string StateName { get; private set; } = string.Empty;

        public List<StateMethod>? StateMethodList { get; private set; }

        public bool IsAbstractOrInterface => this.Kind == VisceralObjectKind.Interface || (this.symbol is INamedTypeSymbol nts && nts.IsAbstract);

        public List<BigMachinesObject>? Children { get; private set; } // The opposite of ContainingObject

        public List<BigMachinesObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

        public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

        public int GenericsNumber { get; private set; }

        public string GenericsNumberString => this.GenericsNumber > 1 ? this.GenericsNumber.ToString() : string.Empty;

        public BigMachinesObject? ClosedGenericHint { get; private set; }

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

            // Closed generic type is not supported.
            if (this.Generics_Kind == VisceralGenericsKind.ClosedGeneric)
            {
                if (this.OriginalDefinition != null && this.OriginalDefinition.ClosedGenericHint == null)
                {
                    this.OriginalDefinition.ClosedGenericHint = this;
                }

                return;
            }

            // StateMachineAttribute
            if (this.AllAttributes.FirstOrDefault(x => x.FullName == StateMachineAttributeMock.FullName) is { } objectAttribute)
            {
                this.Location = objectAttribute.Location;
                try
                {
                    this.ObjectAttribute = StateMachineAttributeMock.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);
                }
                catch (InvalidCastException)
                {
                    this.Body.ReportDiagnostic(BigMachinesBody.Error_AttributePropertyError, objectAttribute.Location);
                }
            }

            if (this.ObjectAttribute != null)
            {
                this.ConfigureObject();
            }
        }

        private void ConfigureObject()
        {
            // Used keywords
            this.Identifier = new VisceralIdentifier("__gen_bm_identifier__");
            foreach (var x in this.AllMembers.Where(a => a.ContainingObject == this))
            {
                this.Identifier.Add(x.SimpleName);
            }
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
            if (!this.IsAbstractOrInterface)
            {
                this.ObjectFlag |= BigMachinesObjectFlag.CanCreateInstance;
            }

            if (this.Generics_Kind == VisceralGenericsKind.OpenGeneric)
            {
                this.Body.ReportDiagnostic(BigMachinesBody.Error_OpenGenericClass, this.Location, this.FullName);
                return;
            }

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
            }

            var id = this.ObjectAttribute!.MachineTypeId;
            if (this.Body.Machines.ContainsKey(id))
            {
                this.Body.ReportDiagnostic(BigMachinesBody.Error_DuplicateTypeId, this.Location, id);
            }
            else
            {
                this.Body.Machines.Add(id, this);
            }

            // Machine<TIdentifier, TState>
            var machineObject = this.BaseObject;
            while (machineObject != null)
            {
                if (machineObject.OriginalDefinition?.FullName == "BigMachines.Machine<TIdentifier, TState>")
                {
                    break;
                }

                machineObject = machineObject.BaseObject;
            }

            if (machineObject == null)
            {
                this.Body.ReportDiagnostic(BigMachinesBody.Error_NotDerived, this.Location);
                return;
            }
            else
            {
                if (machineObject.Generics_Arguments.Length == 2)
                {
                    var identifier = machineObject.Generics_Arguments[0];
                    var state = machineObject.Generics_Arguments[1];

                    this.MachineObject = machineObject;
                    this.IdentifierObject = machineObject.Generics_Arguments[0];
                    // this.StateName = machineObject.Generics_Arguments[1];
                    this.StateName = this.FullName + ".State";
                }
            }

            this.CheckKeyword(BigMachinesBody.StateIdentifier, this.Location);
            this.CheckKeyword(BigMachinesBody.InterfaceIdentifier, this.Location);
            this.CheckKeyword(BigMachinesBody.CreateInterfaceIdentifier, this.Location);
            this.CheckKeyword(BigMachinesBody.RunInternalIdentifier, this.Location);
            this.CheckKeyword(BigMachinesBody.ChangeStateInternal, this.Location);

            this.StateMethodList = new();
            foreach (var x in this.GetMembers(VisceralTarget.Method))
            {
                if (x.AllAttributes.FirstOrDefault(x => x.FullName == StateMethodAttributeMock.FullName) is { } attribute)
                {
                    var stateMethod = StateMethod.Create(x, attribute);
                    if (stateMethod != null)
                    {// Add
                        this.StateMethodList.Add(stateMethod);
                    }
                }
            }
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

        public static void GenerateLoader(ScopingStringBuilder ssb, GeneratorInformation info, List<BigMachinesObject> list)
        {
            var list2 = list.SelectMany(x => x.ConstructedObjects);

            using (var m = ssb.ScopeBrace("internal static void __gen__bm()"))
            {
                foreach (var x in list2)
                {
                }
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
                    GenerateLoader(ssb, info, this.Children);
                }
            }
        }

        internal void Generate2(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            this.Generate_State(ssb, info);
            this.Generate_Interface(ssb, info);
            this.Generate_CreateInterface(ssb, info);
            this.Generate_RunInternal(ssb, info);
            this.Generate_ChangeStateInternal(ssb, info);

            return;
        }

        internal void Generate_State(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (this.StateMethodList == null)
            {
                return;
            }

            using (var scope = ssb.ScopeBrace("public enum State"))
            {
                foreach (var x in this.StateMethodList)
                {
                    ssb.AppendLine($"{x.Name},");
                }
            }

            ssb.AppendLine();
        }

        internal void Generate_Interface(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (this.MachineObject == null)
            {
                return;
            }

            var identifierName = this.IdentifierObject!.FullName;
            using (var scope = ssb.ScopeBrace($"public class Interface : ManMachineInterface<{identifierName}, {this.StateName}>"))
            {
                using (var scope2 = ssb.ScopeBrace($"public Interface(IMachineGroup<{identifierName}> group, {identifierName} identifier) : base(group, identifier)"))
                {
                }
            }

            ssb.AppendLine();
        }

        internal void Generate_CreateInterface(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (this.MachineObject == null)
            {
                return;
            }

            var identifierName = this.IdentifierObject!.FullName;
            using (var scope = ssb.ScopeBrace($"protected override void CreateInterface({identifierName} identifier)"))
            {
                using (var scope2 = ssb.ScopeBrace("if (this.InterfaceInstance == null)"))
                {
                    ssb.AppendLine("this.Identifier = identifier;");
                    ssb.AppendLine("this.InterfaceInstance = new Interface(this.Group, identifier);");
                }
            }

            ssb.AppendLine();
        }

        internal void Generate_RunInternal(ScopingStringBuilder ssb, GeneratorInformation info)
        {
            if (this.MachineObject == null || this.StateMethodList == null)
            {
                return;
            }

            using (var scope = ssb.ScopeBrace("protected override StateResult RunInternal(StateParameter parameter)"))
            {
                ssb.AppendLine($"var state = Unsafe.As<int, {this.StateName}>(ref this.CurrentState);");
                ssb.AppendLine("return state switch");
                ssb.AppendLine("{");
                ssb.IncrementIndent();

                foreach (var x in this.StateMethodList)
                {
                    ssb.AppendLine($"State.{x.Name} => this.{x.Name}(parameter),");
                }

                ssb.AppendLine("_ => StateResult.Terminate,");
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

            using (var scope = ssb.ScopeBrace("protected override bool ChangeStateInternal(int state)"))
            {
                using (var scopeTerminated = ssb.ScopeBrace("if (this.Status == MachineStatus.Terminated)"))
                {
                    ssb.AppendLine("return false;");
                }

                using (var scopeElse = ssb.ScopeBrace("else if (this.CurrentState == state)"))
                {
                    ssb.AppendLine("return true;");
                }

                ssb.AppendLine();
                ssb.AppendLine($"var current = Unsafe.As<int, {this.StateName}>(ref this.CurrentState);");
                ssb.AppendLine("bool canExit = current switch");
                ssb.AppendLine("{");
                ssb.IncrementIndent();
                foreach (var x in this.StateMethodList.Where(a => a.CheckStateChange))
                {
                    ssb.AppendLine($"State.{x.Name} => this.{x.Name}(new StateParameter(RunType.CanExit)) != StateResult.Deny,");
                }

                ssb.AppendLine("_ => true,");
                ssb.DecrementIndent();
                ssb.AppendLine("};");
                ssb.AppendLine();

                ssb.AppendLine($"var next = Unsafe.As<int, {this.StateName}>(ref state);");
                ssb.AppendLine("bool canEnter = next switch");
                ssb.AppendLine("{");
                ssb.IncrementIndent();
                foreach (var x in this.StateMethodList.Where(a => a.CheckStateChange))
                {
                    ssb.AppendLine($"State.{x.Name} => this.{x.Name}(new StateParameter(RunType.CanEnter)) != StateResult.Deny,");
                }

                ssb.AppendLine("_ => true,");
                ssb.DecrementIndent();
                ssb.AppendLine("};");
                ssb.AppendLine();

                using (var scope2 = ssb.ScopeBrace("if (canExit && canEnter)"))
                {
                    ssb.AppendLine($"this.CurrentState = state;");
                    ssb.AppendLine("this.StateChanged = true;");
                    ssb.AppendLine("return true;");
                }

                using (var scope2 = ssb.ScopeBrace("else"))
                {
                    ssb.AppendLine("return false;");
                }
            }

            ssb.AppendLine();
            ssb.AppendLine($"protected bool ChangeStateInternal({this.StateName} state) => this.ChangeStateInternal(Unsafe.As<{this.StateName}, int>(ref state));");
        }
    }
}
