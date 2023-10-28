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
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Benchmark.Test;

public class LockClass
{
    private object cs = new();
    private int x;

    public void Test()
    {
        lock (cs)
        {
            unchecked
            {
                this.x++;
            }
        }
    }
}

public class SemaphoreSlimClass
{
    private SemaphoreSlim semaphore = new(1, 1);
    private int x;

    public void Test()
    {
        this.semaphore.Wait();
        try
        {
            unchecked
            {
                this.x++;
            }
        }
        finally
        {
            this.semaphore.Release();
        }
    }
}

public class AsyncSemaphoreSlimClass
{
    private SemaphoreSlim semaphore = new(1, 1);
    private int x;

    public async Task Test()
    {
        await this.semaphore.WaitAsync();
        try
        {
            unchecked
            {
                this.x++;
            }
        }
        finally
        {
            this.semaphore.Release();
        }
    }
}

[Config(typeof(BenchmarkConfig))]
public class LockingBenchmark
{
    public LockClass LockClass { get; } = new();

    public SemaphoreSlimClass SemaphoreSlimClass { get; } = new();

    public AsyncSemaphoreSlimClass AsyncSemaphoreSlimClass { get; } = new();

    public LockingBenchmark()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark]
    public void Lock() => this.LockClass.Test();

    [Benchmark]
    public void SemaphoreSlim() => this.SemaphoreSlimClass.Test();

    [Benchmark]
    public Task AsyncSemaphoreSlim() => this.AsyncSemaphoreSlimClass.Test();
}
