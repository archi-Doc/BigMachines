// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BigMachines;

#pragma warning disable SA1401 // Fields should be private

public class LoopChecker
{
    public const int InitialArray = 4;

    public static AsyncLocal<LoopChecker> AsyncLocalInstance = new();

    public LoopChecker()
    {
    }

    public LoopChecker(LoopChecker loopChecker)
    {
        this.RunIdCount = loopChecker.RunIdCount;
        if (loopChecker.RunId != null)
        {
            this.RunId = new uint[loopChecker.RunId.Length];
            for (var n = 0; n < loopChecker.RunIdCount; n++)
            {
                this.RunId[n] = loopChecker.RunId[n];
            }
        }
        else
        {
            this.RunId0 = loopChecker.RunId0;
            this.RunId1 = loopChecker.RunId1;
        }

        this.CommandIdCount = loopChecker.CommandIdCount;
        if (loopChecker.CommandId != null)
        {
            this.CommandId = new uint[loopChecker.CommandId.Length];
            for (var n = 0; n < loopChecker.CommandIdCount; n++)
            {
                this.CommandId[n] = loopChecker.CommandId[n];
            }
        }
        else
        {
            this.CommandId0 = loopChecker.CommandId0;
            this.CommandId1 = loopChecker.CommandId1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRunId(uint id)
    {
        if (this.RunIdCount == 0)
        {
            this.RunId0 = id;
            this.RunIdCount = 1;
        }
        else if (this.RunIdCount == 1)
        {
            this.RunId1 = id;
            this.RunIdCount = 2;
        }
        else
        {
            if (this.RunId == null)
            {
                this.RunId = new uint[InitialArray];
                this.RunId[0] = this.RunId0;
                this.RunId[1] = this.RunId1;
            }
            else if (this.RunIdCount >= this.RunId.Length)
            {
                Array.Resize(ref this.RunId, this.RunId.Length + InitialArray);
            }

            this.RunId[this.RunIdCount++] = id;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddCommandId(uint id)
    {
        if (this.CommandIdCount == 0)
        {
            this.CommandId0 = id;
            this.CommandIdCount = 1;
        }
        else if (this.CommandIdCount == 1)
        {
            this.CommandId1 = id;
            this.CommandIdCount = 2;
        }
        else
        {
            if (this.CommandId == null)
            {
                this.CommandId = new uint[InitialArray];
                this.CommandId[0] = this.CommandId0;
                this.CommandId[1] = this.CommandId1;
            }
            else if (this.CommandIdCount >= this.CommandId.Length)
            {
                Array.Resize(ref this.CommandId, this.CommandId.Length + InitialArray);
            }

            this.CommandId[this.CommandIdCount++] = id;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool FindCommandId(uint id)
    {
        if (this.CommandIdCount == 0)
        {
            return false;
        }
        else if (this.CommandIdCount == 1)
        {
            return this.CommandId0 == id;
        }
        else if (this.CommandIdCount == 2)
        {
            return this.CommandId0 == id || this.CommandId1 == id;
        }
        else
        {
            for (var n = 0; n < this.CommandIdCount; n++)
            {
                if (this.CommandId![n] == id)
                {
                    return true;
                }
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool FindRunId(uint id)
    {
        if (this.RunIdCount == 0)
        {
            return false;
        }
        else if (this.RunIdCount == 1)
        {
            return this.RunId0 == id;
        }
        else if (this.RunIdCount == 2)
        {
            return this.RunId0 == id || this.RunId1 == id;
        }
        else
        {
            for (var n = 0; n < this.RunIdCount; n++)
            {
                if (this.RunId![n] == id)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public IEnumerable<uint> EnumerateCommandId()
    {
        if (this.CommandIdCount == 0)
        {
            yield break;
        }
        else if (this.CommandIdCount == 1)
        {
            yield return this.CommandId0;
        }
        else if (this.CommandIdCount == 2)
        {
            yield return this.CommandId0;
            yield return this.CommandId1;
        }
        else
        {
            for (var n = 0; n < this.CommandIdCount; n++)
            {
                yield return this.CommandId![n];
            }
        }
    }

    public IEnumerable<uint> EnumerateRunId()
    {
        if (this.RunIdCount == 0)
        {
            yield break;
        }
        else if (this.RunIdCount == 1)
        {
            yield return this.RunId0;
        }
        else if (this.RunIdCount == 2)
        {
            yield return this.RunId0;
            yield return this.RunId1;
        }
        else
        {
            for (var n = 0; n < this.RunIdCount; n++)
            {
                yield return this.RunId![n];
            }
        }
    }

    internal int RunIdCount;
    internal uint RunId0;
    internal uint RunId1;
    internal uint[]? RunId;

    internal int CommandIdCount;
    internal uint CommandId0;
    internal uint CommandId1;
    internal uint[]? CommandId;

    public LoopChecker Clone() => new(this);

    public override string ToString() => $"Run {this.RunIdCount}, Command {this.CommandIdCount}";
}

#pragma warning restore SA1401 // Fields should be private
