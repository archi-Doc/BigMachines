// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Benchmark.Test;

#pragma warning disable SA1401 // Fields should be private

public struct LoopCheckerStruct
{
    public const int InitialArray = 4;

    public static AsyncLocal<LoopCheckerStruct> AsyncLocalInstance = new();

    public LoopCheckerStruct()
    {
        this.RunId = new uint[InitialArray];
        this.RunIdCount = 0;
        this.CommandId = new uint[InitialArray];
        this.CommandIdCount = 0;
    }

    public LoopCheckerStruct(LoopCheckerStruct loopChecker)
    {
        this.RunId = new uint[loopChecker.RunId.Length];
        this.RunIdCount = loopChecker.RunIdCount;
        Array.Copy(loopChecker.RunId, this.RunId, loopChecker.RunIdCount);

        this.CommandId = new uint[loopChecker.CommandId.Length];
        this.CommandIdCount = loopChecker.CommandIdCount;
        Array.Copy(loopChecker.CommandId, this.CommandId, loopChecker.CommandIdCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRunId(uint id)
    {
        if (this.RunIdCount >= this.RunId.Length)
        {
            Array.Resize(ref this.RunId, this.RunId.Length + InitialArray);
        }

        this.RunId[this.RunIdCount++] = id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddCommandId(uint id)
    {
        if (this.CommandIdCount >= this.CommandId.Length)
        {
            Array.Resize(ref this.CommandId, this.CommandId.Length + InitialArray);
        }

        this.CommandId[this.CommandIdCount++] = id;
    }

    internal uint[] RunId;

    internal int RunIdCount;

    internal uint[] CommandId;

    internal int CommandIdCount;

    public LoopCheckerStruct Clone() => new(this);

    public override string ToString() => $"Run {this.RunIdCount}, Command {this.CommandIdCount}";
}
#pragma warning restore SA1401 // Fields should be private
