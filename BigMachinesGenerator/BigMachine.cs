// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace BigMachines.Generator;

internal class BigMachine : IEquatable<BigMachine>
{
    public BigMachine(BigMachinesBody body, BigMachinesObject obj)
    {
        this.Body = body;
        this.Object = obj;
        this.Namespace = this.Object.Namespace; // this.Object is null ? BigMachinesBody.BigMachineNamespace : this.Object.Namespace;
        this.SimpleName = this.Object.SimpleName; // this.Object is null ? BigMachinesBody.DefaultBigMachineObject : this.Object.SimpleName;
    }

    internal class Machine
    {
        public static Machine? Create(string name, BigMachinesObject machineObject, AddMachineAttributeMock? attribute)
        {
            if (machineObject.ObjectAttribute is null)
            {
                return null;
            }

            if (machineObject.Generics_Kind == VisceralGenericsKind.OpenGeneric)
            {
                return null;
            }

            var machine = new Machine();
            machine.Name = name;
            machine.FullName = machineObject.FullName;
            machine.Control = machineObject.ObjectAttribute.Control;
            machine.MachineObject = machineObject;
            machine.IdentifierObject = machineObject.IdentifierObject;

            if (machine.Control == MachineControlKind.Single)
            {
                machine.ControlType = $"SingleMachineControl<{machine.FullName}, {machine.FullName}.Interface>";
            }
            else if (machine.Control == MachineControlKind.Unordered)
            {
                if (machine.IdentifierObject is null)
                {
                    return null;
                }

                machine.ControlType = $"UnorderedMachineControl<{machine.IdentifierObject.FullName}, {machine.FullName}, {machine.FullName}.Interface>";
            }
            else if (machine.Control == MachineControlKind.Sequential)
            {
                if (machine.IdentifierObject is null)
                {
                    return null;
                }

                machine.ControlType = $"SequentialMachineControl<{machine.IdentifierObject.FullName}, {machine.FullName}, {machine.FullName}.Interface>";
            }
            else
            {
                return null;
            }

            machine.Key = (int)FarmHash.Hash64(machine.ControlType);

            return machine;
        }

        public string Name { get; private set; } = string.Empty;

        public string FullName { get; private set; } = string.Empty;

        public MachineControlKind Control { get; private set; }

        public BigMachinesObject MachineObject { get; private set; } = default!;

        public BigMachinesObject? IdentifierObject { get; private set; }

        public string ControlType { get; private set; } = string.Empty;

        public int Key { get; private set; }
    }

    public BigMachinesBody Body { get; }

    public BigMachinesObject Object { get; }

    public BigMachineObjectAttributeMock? Attribute { get; private set; }

    public string Namespace { get; }

    public string SimpleName { get; }

    public bool Inclusive { get; set; }

    public Dictionary<BigMachinesObject, AddMachineAttributeMock?> AddedMachines { get; } = new();

    public Dictionary<string, Machine> Machines { get; } = new();

    public override int GetHashCode()
        => this.Object is null ? 0 : this.Object.GetHashCode();

    bool IEquatable<BigMachine>.Equals(BigMachine other)
        => this.Object == other.Object;

    public void Prepare()
    {
        if (this.Object?.AllAttributes.FirstOrDefault(x => x.FullName == BigMachineObjectAttributeMock.FullName) is { } attr)
        {
            try
            {
                this.Attribute = BigMachineObjectAttributeMock.FromArray(attr.ConstructorArguments, attr.NamedArguments);
            }
            catch (InvalidCastException)
            {
                this.Body.ReportDiagnostic(BigMachinesBody.Error_AttributePropertyError, attr.Location);
            }
        }

        if (this.Object is null)
        {
            this.Inclusive = true;
        }
        else
        {
            this.Inclusive = this.Attribute?.Inclusive == true;
        }

        var start = AddMachineAttributeMock.FullName + "<";

        if (this.Inclusive)
        {// Inclusive big machine
            var array = this.Body.FullNameToObject.Values.Where(x => x.ObjectFlag.HasFlag(BigMachinesObjectFlag.MachineObject) && x.ObjectAttribute?.Private == false).ToArray();
            foreach (var x in array)
            {
                this.AddedMachines[x] = null;
            }
        }

        if (this.Object is not null)
        {
            foreach (var objectAttribute in this.Object.AllAttributes)
            {
                if (objectAttribute.FullName.StartsWith(start) && objectAttribute.FullName.EndsWith(">"))
                {// AddMachineAttribute
                    // var machineName = objectAttribute.FullName.Substring(start.Length, objectAttribute.FullName.Length - start.Length - 1);

                    BigMachinesObject? obj = default;
                    MachineObjectAttributeMock? atr = default;
                    var args = objectAttribute.AttributeData?.AttributeClass?.TypeArguments;
                    if (args.HasValue && args.Value.Length > 0 && args.Value[0] is INamedTypeSymbol machineType)
                    {// AddMachineAttribute<machineType>
                        obj = this.Body.Add(machineType);
                        if (obj is null)
                        {
                            continue;
                        }

                        obj.Configure();
                        atr = obj.TryGetObjectAttribute();
                        if (obj.ObjectAttribute is null)
                        {
                            obj.ObjectAttribute = atr;
                        }
                    }

                    if (obj is not null)
                    {
                        AddMachineAttributeMock? attribute = null;
                        try
                        {
                            attribute = AddMachineAttributeMock.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);
                            attribute.Location = objectAttribute.Location;
                        }
                        catch (InvalidCastException)
                        {
                            this.Body.ReportDiagnostic(BigMachinesBody.Error_AttributePropertyError, objectAttribute.Location);
                        }

                        this.AddedMachines[obj] = attribute;
                    }
                    else
                    {
                    }
                }
            }
        }
    }

    public void Check()
    {
        if (this.Object is not null)
        {
            if (this.Object.ContainingObject is not null)
            {
                this.Body.ReportDiagnostic(BigMachinesBody.Error_BigMachineClass, this.Object.Location);
            }

            if (!this.Object.IsPartial)
            {
                this.Body.ReportDiagnostic(BigMachinesBody.Error_NotPartial, this.Object.Location, this.Object.FullName);
            }

            // Parent class also needs to be a partial class.
            var parent = this.Object.ContainingObject;
            while (parent != null)
            {
                if (!parent.IsPartial)
                {
                    this.Body.ReportDiagnostic(BigMachinesBody.Error_NotPartialParent, parent.Location, parent.FullName);
                }

                parent = parent.ContainingObject;
            }

            if (this.Object.HasExplicitDefaultConstructor())
            {
                this.Body.ReportDiagnostic(BigMachinesBody.Error_ExplicitDefaultConstructor, this.Object.Location);
            }
        }

        foreach (var x in this.AddedMachines)
        {
            var name = string.IsNullOrEmpty(x.Value?.Name) ? x.Key.SimpleName : x.Value!.Name;
            if (this.Machines.ContainsKey(name))
            {
                this.Body.ReportDiagnostic(BigMachinesBody.Error_DuplicateMachineControl, x.Value?.Location, name);
            }
            else
            {
                var machine = Machine.Create(name, x.Key, x.Value);
                if (machine is not null)
                {
                    this.Machines.Add(name, machine);
                }
            }
        }
    }

    public void Generate(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        ssb.AppendLine("[TinyhandObject]");
        using (var scopeClass = ssb.ScopeBrace($"public partial class {this.SimpleName} : BigMachineBase, ITinyhandSerializable<{this.SimpleName}>, ITinyhandReconstructable<{this.SimpleName}>, ITinyhandCloneable<{this.SimpleName}>, IStructuralObject"))
        {
            this.GenerateMembers(ssb, info);
            ssb.AppendLine();

            this.GenerateConstructor(ssb, info);
            ssb.AppendLine();

            this.GenerateSerialize(ssb, info);
            this.GenerateDeserialize(ssb, info);
            ssb.AppendLine($"static void ITinyhandReconstructable<{this.SimpleName}>.Reconstruct([NotNull] scoped ref {this.SimpleName}? value, TinyhandSerializerOptions options) => value ??= new();");
            ssb.AppendLine($"static {this.SimpleName}? ITinyhandCloneable<{this.SimpleName}>.Clone(scoped ref {this.SimpleName}? v, TinyhandSerializerOptions options) => v == null ? null : TinyhandSerializer.Deserialize<{this.SimpleName}>(TinyhandSerializer.Serialize(v));");
            ssb.AppendLine();

            this.GenerateStructural(ssb, info);
            ssb.AppendLine();

            this.GenerateStartByDefault(ssb, info);
        }
    }

    public void GenerateStartByDefault(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        using (var scopeMethod = ssb.ScopeBrace($"protected override void StartBigMachine()"))
        {
            foreach (var x in this.Machines.Values.Where(a => a.MachineObject.ObjectAttribute?.StartByDefault == true && a.MachineObject.ObjectAttribute.Control == MachineControlKind.Single))
            {
                ssb.AppendLine($"this.{x.Name}.GetOrCreate();");
            }

            foreach (var x in this.Machines.Values.Where(a => a.MachineObject.ObjectAttribute?.Control == MachineControlKind.Sequential))
            {
                ssb.AppendLine($"this.{x.Name}.Start();");
            }
        }
    }

    public void GenerateConstructor(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        using (var scopeMethod = ssb.ScopeBrace($"public {this.SimpleName}()"))
        {
            var sb = new StringBuilder();
            sb.Append("this.controls = new MachineControl[] { this.ManualControl, ");

            foreach (var x in this.Machines.Values)
            {
                if (x.MachineObject.Generics_Kind == VisceralGenericsKind.ClosedGeneric)
                {
                    ssb.AppendLine($"{x.FullName}.RegisterBM();");
                }

                ssb.AppendLine($"this._{x.Name} = new();");
                ssb.AppendLine($"this.{x.Name}.Prepare(this);");
                ssb.AppendLine($"((IStructuralObject)this.{x.Name}).SetupStructure(this, {x.Key.ToString()});");

                sb.Append($"this.{x.Name}, ");
            }

            sb.Append("};");
            ssb.AppendLine(sb.ToString());
        }
    }

    public void GenerateMembers(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        ssb.AppendLine("private MachineControl[] controls = Array.Empty<MachineControl>();");
        ssb.AppendLine("public override MachineControl[] GetArray() => controls;");
        foreach (var x in this.Machines.Values)
        {
            ssb.AppendLine($"private {x.ControlType} _{x.Name};");
            ssb.AppendLine($"{x.MachineObject.AccessibilityName} {x.ControlType} {x.Name} => this._{x.Name};");
        }
    }

    public void GenerateSerialize(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        using (var scopeMethod = ssb.ScopeBrace($"static void ITinyhandSerializable<{this.SimpleName}>.Serialize(ref TinyhandWriter writer, scoped ref {this.SimpleName}? value, TinyhandSerializerOptions options)"))
        {
            ssb.AppendLine("if (value == null) { writer.WriteNil(); return; }");
            ssb.AppendLine("var count = value.controls.Count(x => x.MachineInformation.Serializable);");
            ssb.AppendLine("writer.WriteMapHeader(count);");

            foreach (var x in this.Machines.Values)
            {
                ssb.AppendLine();
                using (var scopeSerialize = ssb.ScopeBrace($"if (value.{x.Name}.MachineInformation.Serializable)"))
                {
                    ssb.AppendLine($"writer.Write({x.Key.ToString()});");
                    ssb.AppendLine($"TinyhandSerializer.SerializeObject(ref writer, value.{x.Name}, options);");
                }
            }
        }
    }

    public void GenerateDeserialize(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        using (var scopeMethod = ssb.ScopeBrace($"static void ITinyhandSerializable<{this.SimpleName}>.Deserialize(ref TinyhandReader reader, scoped ref {this.SimpleName}? value, TinyhandSerializerOptions options)"))
        {
            ssb.AppendLine("if (reader.TryReadNil()) return;");
            ssb.AppendLine("value ??= new();");
            ssb.AppendLine("var count = reader.ReadMapHeader2();");

            var trie = new VisceralTrieInt<Machine>(null);
            foreach (var x in this.Machines.Values)
            {
                trie.AddNode(x.Key, x);
            }

            var context = new VisceralTrieInt<Machine>.VisceralTrieContext(
                ssb,
                (ctx, obj, node) =>
                {
                    ssb.AppendLine($"TinyhandSerializer.DeserializeObject(ref reader, ref value._{node.Member.Name}!, options);");
                });

            using (var scopeWhile = ssb.ScopeBrace("while (count-- > 0)"))
            {
                trie.Generate(context);
            }
        }
    }

    public void GenerateStructural(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        ssb.AppendLine("IStructuralRoot? IStructuralObject.StructuralRoot { get; set; }");
        ssb.AppendLine("IStructuralObject? IStructuralObject.StructuralParent { get; set; }");
        ssb.AppendLine("int IStructuralObject.StructuralKey { get; set; }");

        using (var scopeMethod = ssb.ScopeBrace("bool IStructuralObject.ProcessJournalRecord(ref TinyhandReader reader)"))
        {
            ssb.AppendLine("if (!reader.TryReadJournalRecord(out JournalRecord record)) return false;");

            var trie = new VisceralTrieInt<Machine>(null);
            foreach (var x in this.Machines.Values)
            {
                trie.AddNode(x.Key, x);
            }

            var context = new VisceralTrieInt<Machine>.VisceralTrieContext(
                ssb,
                (ctx, obj, node) =>
                {
                    ssb.AppendLine($"return ((IStructuralObject)this.{node.Member.Name}).ProcessJournalRecord(ref reader);");
                });

            using (var scopeKey = ssb.ScopeBrace("if (record == JournalRecord.Key)"))
            {
                trie.Generate(context);
            }

            ssb.AppendLine();
            ssb.AppendLine("return false;");
        }

        /*ssb.AppendLine();
        using (var scopeMethod = ssb.ScopeBrace("void IStructuralObject.SetupStructure(IStructuralObject? parent, int key)"))
        {
            ssb.AppendLine("((IStructuralObject)this).SetParentAndKey(parent, key);");
            foreach (var x in this.Machines.Values)
            {
                ssb.AppendLine($"((IStructuralObject)this.{x.Name})?.SetupStructure(this, {x.Key.ToString()});");
            }
        }*/
    }
}
