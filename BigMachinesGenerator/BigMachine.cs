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
                // MachineObjectAttribute
                if (x.AllAttributes.FirstOrDefault(x => x.FullName.StartsWith(start) && x.FullName.EndsWith(">")) is { } objectAttribute)
                {
                    var machineName = objectAttribute.FullName.Substring(start.Length, objectAttribute.FullName.Length - start.Length - 1);
                    if (this.Body.TryGet(machineName, out var obj))
                    {
                        AddMachineAttributeMock? attribute = null;
                        this.Location = objectAttribute.Location;
                        try
                        {
                            attribute = AddMachineAttributeMock.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);
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

    public BigMachinesBody Body { get; }

    public BigMachinesObject? Object { get; }

    public bool Default { get; set; }

    public Location? Location { get; private set; }

    public Dictionary<BigMachinesObject, AddMachineAttributeMock?> AddedMachines { get; } = new();

    public override int GetHashCode()
        => this.Object is null ? 0 : this.Object.GetHashCode();

    bool IEquatable<BigMachine>.Equals(BigMachine other)
        => this.Object == other.Object;
}
