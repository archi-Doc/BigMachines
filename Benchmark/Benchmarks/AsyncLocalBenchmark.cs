// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using BenchmarkDotNet.Attributes;
using BigMachines;

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

    // [Benchmark]
    public string? AsyncLocal_Get()
    {
        return asyncLocal.Value;
    }

    // [Benchmark]
    public string? AsyncLocal_GetSet()
    {
        var st = asyncLocal.Value;
        asyncLocal.Value = "1234";
        return st;
    }

    [Benchmark]
    public ulong RecursiveDetection_GetSet()
    {
        var r = RecursiveDetection.AsyncLocalInstance.Value;
        r.TryAdd(123, ((ulong)123 << 32) | 456, out var r2);
        RecursiveDetection.AsyncLocalInstance.Value = r2;

        return r2.Id0;
    }

    [Benchmark]
    public ulong RecursiveDetection2_GetSet()
    {
        var r = RecursiveDetection2.AsyncLocalInstance.Value ?? new();
        r.TryAdd(123, ((ulong)123 << 32) | 456);
        RecursiveDetection2.AsyncLocalInstance.Value = r;

        return r.Id0;
    }

    [Benchmark]
    public ulong RecursiveDetection3_GetSet()
    {
        var r = RecursiveDetection3.AsyncLocalInstance.Value;
        r.TryAdd(123, ((ulong)123 << 32) | 456, out var r2);
        RecursiveDetection3.AsyncLocalInstance.Value = r2;

        return r2.Id0;
    }
}
