// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace BigMachines.Generator;

internal class BigMachine : IEquatable<BigMachine>
{
    public BigMachine(BigMachinesBody body, BigMachinesObject? obj)
    {
        this.Body = body;
        this.Object = obj;
    }

    internal class Machine
    {
        public static Machine? Create(string name, BigMachinesObject machineObject, AddMachineAttributeMock? attribute)
        {
            if (machineObject.ObjectAttribute is null)
            {
                return null;
            }

            var machine = new Machine();
            machine.Name = name;
            machine.Control = machineObject.ObjectAttribute.Control;
            machine.MachineObject = machineObject;
            machine.IdentifierObject = machineObject.IdentifierObject;

            return machine;
        }

        public string Name { get; private set; } = string.Empty;

        public MachineControlKind Control { get; private set; }

        public BigMachinesObject MachineObject { get; private set; } = default!;

        public BigMachinesObject? IdentifierObject { get; private set; }
    }

    public BigMachinesBody Body { get; }

    public BigMachinesObject? Object { get; }

    public bool Default { get; set; }

    public Dictionary<BigMachinesObject, AddMachineAttributeMock?> AddedMachines { get; } = new();

    public Dictionary<string, Machine> Machines { get; } = new();

    public override int GetHashCode()
        => this.Object is null ? 0 : this.Object.GetHashCode();

    bool IEquatable<BigMachine>.Equals(BigMachine other)
        => this.Object == other.Object;

    public void AddMachines()
    {
        var start = AddMachineAttributeMock.FullName + "<";

        if (this.Default)
        {// Default big machine
            var array = this.Body.FullNameToObject.Values.Where(x => x.ObjectFlag.HasFlag(BigMachinesObjectFlag.MachineObject)).ToArray();
            foreach (var x in array)
            {
                this.AddedMachines[x] = null;
            }
        }

        if (this.Object is not null)
        {
            foreach (var x in this.Object.GetMembers(Arc.Visceral.VisceralTarget.Method).Where(x => x.Method_IsConstructor))
            {
                foreach (var objectAttribute in x.AllAttributes)
                {
                    if (objectAttribute.FullName.StartsWith(start) && objectAttribute.FullName.EndsWith(">"))
                    {// MachineObjectAttribute
                        var machineName = objectAttribute.FullName.Substring(start.Length, objectAttribute.FullName.Length - start.Length - 1);
                        if (this.Body.TryGet(machineName, out var obj))
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
    }

    public void Check()
    {
        if (this.Object is null)
        {
            return;
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
}
