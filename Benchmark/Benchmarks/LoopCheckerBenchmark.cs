// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[Config(typeof(BenchmarkConfig))]
public class LoopCheckerBenchmark
{
    public uint[] Data { get; }

    public uint IdToFind { get; }

    public uint[] IdArray { get; }

    public int IdArrayCount = 0;

    public List<uint> IdList { get; }

    public HashSet<uint> IdSet { get; }

    public Stack<uint> IdStack { get; }

    public LoopCheckerBenchmark()
    {
        this.Data = new uint[]
        {
            0xc0526b51,
            0x1a5bb41e,
            0x594578d5,
            0x8bd1198b,
            0x68daeb41,
        };

        this.IdToFind = this.Data[3];
        this.IdArray = this.Prepare_Array();
        this.IdList = this.Prepare_List();
        this.IdSet = this.Prepare_HashSet();
        this.IdStack = this.Prepare_Stack();
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public uint[] Prepare_Array()
    {
        var array = new uint[4];
        var count = 0;
        foreach (var x in this.Data)
        {
            if (count >= array.Length)
            {
                Array.Resize(ref array, array.Length + 4);
            }

            array[count++] = x;
        }

        this.IdArrayCount = count;
        return array;
    }

    [Benchmark]
    public List<uint> Prepare_List()
    {
        var list = new List<uint>();
        foreach (var x in this.Data)
        {
            list.Add(x);
        }

        return list;
    }

    [Benchmark]
    public HashSet<uint> Prepare_HashSet()
    {
        var set = new HashSet<uint>();
        foreach (var x in this.Data)
        {
            set.Add(x);
        }

        return set;
    }

    [Benchmark]
    public Stack<uint> Prepare_Stack()
    {
        var s = new Stack<uint>();
        foreach (var x in this.Data)
        {
            s.Push(x);
        }

        return s;
    }

    [Benchmark]
    public bool Find_Array()
    {
        for (var n = 0; n < this.IdArrayCount ; n++)
        {
            if (this.IdArray[n] == this.IdToFind)
            {
                return true;
            }
        }

        return false;
    }

    [Benchmark]
    public bool Find_List() => this.IdList.Contains(this.IdToFind);

    [Benchmark]
    public bool Find_HashSet() => this.IdSet.Contains(this.IdToFind);

    [Benchmark]
    public bool Find_Stack() => this.IdStack.Contains(this.IdToFind);
}
