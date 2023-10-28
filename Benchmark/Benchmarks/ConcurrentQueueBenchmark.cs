// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;

namespace Benchmark.Design;

internal class BaseClass
{
    internal string Name { get; } = "name";
}

internal class TestClass
{
    public TestClass()
    {
        this.obj = new BaseClass();
    }

    internal int id { get; set; }

    internal object? obj { get; set; }
}

internal struct TestStruct
{
    internal int id { get; set; }

    internal object? obj { get; set; }
}

public class TestInterface
{
    public TestInterface(object? obj, int identifier)
    {
        this.obj = obj;
        this.identifier = identifier;
    }

    internal object? obj { get; set; }
    internal int identifier { get; set; }
}

public struct TestInterface2
{
    public TestInterface2(object? obj, int identifier)
    {
        this.obj = obj;
        this.identifier = identifier;
    }

    internal object? obj { get; set; }
    internal int identifier { get; set; }
}

[Config(typeof(BenchmarkConfig))]
public class ConcurrentQueueBenchmark
{
    private ConcurrentQueue<TestClass> queue1 { get; } = new();

    private ConcurrentQueue<TestStruct> queue2 { get; } = new();

    public ConcurrentQueueBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    /*[Benchmark]
    public bool TestClass()
    {
        var t = new Design.TestClass();
        this.queue1.Enqueue(t);
        return this.queue1.TryDequeue(out _);
    }

    [Benchmark]
    public bool TestStruct()
    {
        Design.TestStruct t = default;
        t.obj = new BaseClass();
        this.queue2.Enqueue(t);
        return this.queue2.TryDequeue(out _);
    }*/

    [Benchmark]
    public TestInterface Test()
    {
        return new TestInterface(this, 4);
    }

    [Benchmark]
    public TestInterface2 Test2()
    {
        return new TestInterface2(this, 4);
    }
}
