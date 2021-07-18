// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

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
            foreach (var x in this.AllMembers)
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
            return;
        }
    }
}
