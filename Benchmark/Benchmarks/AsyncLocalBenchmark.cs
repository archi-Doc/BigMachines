// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace Benchmark.Test;

[Config(typeof(BenchmarkConfig))]
public class AsyncLocalBenchmark
{
    private const string Name = "bmLP";
    static private AsyncLocal<string> asyncLocal = new();

    public AsyncLocalBenchmark()
    {
        asyncLocal.Value = "test";
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public string? AsyncLocal_Get()
    {
        return asyncLocal.Value;
    }

    [Benchmark]
    public string? AsyncLocal_GetSet()
    {
        var st = asyncLocal.Value;
        asyncLocal.Value = "1234";
        return st;
    }
}
