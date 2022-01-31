// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BigMachines;

#pragma warning disable SA1401 // Fields should be private
internal class LoopCheckerObsolete
{
    public const int InitialArray = 4;

    public static AsyncLocal<LoopCheckerObsolete> AsyncLocalInstance = new();

    public LoopCheckerObsolete()
    {
        this.RunId = new uint[InitialArray];
        this.CommandId = new uint[InitialArray];
    }

    public LoopCheckerObsolete(LoopCheckerObsolete loopChecker)
    {
        this.RunId = new uint[loopChecker.RunId.Length];
        this.RunIdCount = loopChecker.RunIdCount;
        for (var n = 0; n < loopChecker.RunIdCount; n++)
        {
            this.RunId[n] = loopChecker.RunId[n];
        }

        this.CommandId = new uint[loopChecker.CommandId.Length];
        this.CommandIdCount = loopChecker.CommandIdCount;
        for (var n = 0; n < loopChecker.CommandIdCount; n++)
        {
            this.CommandId[n] = loopChecker.CommandId[n];
        }
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
    public void RemoveRunId()
    {
        if (this.RunIdCount == 0)
        {
            throw new InvalidOperationException();
        }

        this.RunIdCount--;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveCommandId()
    {
        if (this.CommandIdCount == 0)
        {
            throw new InvalidOperationException();
        }

        this.CommandIdCount--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool FindCommandId(uint id)
    {
        for (var n = 0; n < this.CommandIdCount; n++)
        {
            if (this.CommandId[n] == id)
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool FindRunId(uint id)
    {
        for (var n = 0; n < this.RunIdCount; n++)
        {
            if (this.RunId[n] == id)
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<uint> EnumerateCommandId()
    {
        for (var n = 0; n < this.CommandIdCount; n++)
        {
            yield return this.CommandId[n];
        }
    }

    public IEnumerable<uint> EnumerateRunId()
    {
        for (var n = 0; n < this.RunIdCount; n++)
        {
            yield return this.RunId[n];
        }
    }

    internal uint[] RunId;

    internal int RunIdCount;

    internal uint[] CommandId;

    internal int CommandIdCount;

    public LoopCheckerObsolete Clone() => new(this);

    public override string ToString() => $"Run {this.RunIdCount}, Command {this.CommandIdCount}";
}
#pragma warning restore SA1401 // Fields should be private
