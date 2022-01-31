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

public enum LoopCheckerMode
{
    /// <summary>
    /// Loop checker is enabled and throws an exception when a recursive command is detected.
    /// </summary>
    EnabledAndThrowException,

    /// <summary>
    /// Loop checker is enabled but does not throw an exception when a recursive command is detected.
    /// </summary>
    EnabledWithoutException,

    /// <summary>
    /// Loop checker is disabled.
    /// </summary>
    Disabled,
}

internal class LoopChecker
{
    public const int InitialArray = 4;

    public static AsyncLocal<LoopChecker> AsyncLocalInstance = new();

    public LoopChecker()
    {
    }

    public LoopChecker(LoopChecker loopChecker)
    {
        this.IdCount = loopChecker.IdCount;
        if (loopChecker.IdArray != null)
        {
            this.IdArray = new ulong[loopChecker.IdArray.Length];
            for (var n = 0; n < loopChecker.IdCount; n++)
            {
                this.IdArray[n] = loopChecker.IdArray[n];
            }
        }
        else
        {
            this.Id0 = loopChecker.Id0;
            this.Id1 = loopChecker.Id1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddId(ulong id)
    {
        if (this.IdCount == 0)
        {
            this.Id0 = id;
            this.IdCount = 1;
        }
        else if (this.IdCount == 1)
        {
            this.Id1 = id;
            this.IdCount = 2;
        }
        else
        {
            if (this.IdArray == null)
            {
                this.IdArray = new ulong[InitialArray];
                this.IdArray[0] = this.Id0;
                this.IdArray[1] = this.Id1;
            }
            else if (this.IdCount >= this.IdArray.Length)
            {
                Array.Resize(ref this.IdArray, this.IdArray.Length + InitialArray);
            }

            this.IdArray[this.IdCount++] = id;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool FindId(ulong id)
    {
        if (this.IdCount == 0)
        {
            return false;
        }
        else if (this.IdCount == 1)
        {
            return this.Id0 == id;
        }
        else if (this.IdCount == 2)
        {
            return this.Id0 == id || this.Id1 == id;
        }
        else
        {
            for (var n = 0; n < this.IdCount; n++)
            {
                if (this.IdArray![n] == id)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public IEnumerable<ulong> EnumerateId()
    {
        if (this.IdCount == 0)
        {
            yield break;
        }
        else if (this.IdCount == 1)
        {
            yield return this.Id0;
        }
        else if (this.IdCount == 2)
        {
            yield return this.Id0;
            yield return this.Id1;
        }
        else
        {
            for (var n = 0; n < this.IdCount; n++)
            {
                yield return this.IdArray![n];
            }
        }
    }

    internal int IdCount;
    internal ulong Id0;
    internal ulong Id1;
    internal ulong[]? IdArray;

    public LoopChecker Clone() => new(this);

    public override string ToString() => $"Ids {this.IdCount}";
}

#pragma warning restore SA1401 // Fields should be private
