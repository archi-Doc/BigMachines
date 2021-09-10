// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BigMachines;
using Tinyhand;

namespace Advanced;

[TinyhandObject]
public partial class IdentifierClass : IEquatable<IdentifierClass>
{
    public static IdentifierClass Default { get; } = new();

    public IdentifierClass()
    {
        this.Name = string.Empty;
    }

    public IdentifierClass(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    [Key(0)]
    public int Id { get; private set; }

    [Key(1)]
    public string Name { get; private set; }

    public bool Equals(IdentifierClass? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Id == other.Id && this.Name == other.Name;
    }

    public override int GetHashCode() => HashCode.Combine(this.Id, this.Name);

    public override string ToString() => $"Id: {this.Id} Name: {this.Name}";
}

// Alternative
[TinyhandObject(ImplicitKeyAsName = true)]
public partial record IdentifierClass2(int Id, string Name);

[MachineObject(0x084dabdd)]
public partial class IdentifierMachine : Machine<IdentifierClass>
{
    public IdentifierMachine(BigMachine<IdentifierClass> bigMachine)
        : base(bigMachine)
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"Identifier ({this.Identifier.ToString()}) - {this.Count++}");
        if (this.Count > 5)
        {
            return StateResult.Terminate;
        }

        return StateResult.Continue;
    }
}

[MachineObject(0x8b5d4156)]
public partial class IdentifierMachine2 : Machine<IdentifierClass2>
{
    public IdentifierMachine2(BigMachine<IdentifierClass2> bigMachine)
        : base(bigMachine)
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"Identifier2 ({this.Identifier.ToString()}) - {this.Count++}");
        if (this.Count > 5)
        {
            return StateResult.Terminate;
        }

        return StateResult.Continue;
    }
}
