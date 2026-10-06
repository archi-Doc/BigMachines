// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;

namespace BigMachines;

/*public enum RecursiveDetectionMode
{
    /// <summary>
    /// Detection of recursive calls is disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// Detection of recursive calls is enabled.
    /// </summary>
    EnabledAndThrowException,
}*/

internal readonly struct RecursiveChecker
{
    public static readonly AsyncLocal<RecursiveChecker> AsyncLocalInstance = new();

    public readonly ulong Id0;
    public readonly ulong Id1;
    public readonly ulong Id2;
    public readonly ulong Id3;
    public readonly ulong Id4;
    public readonly ulong Id5;

    public RecursiveChecker(ulong id0, ulong id1 = 0, ulong id2 = 0, ulong id3 = 0, ulong id4 = 0, ulong id5 = 0)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
        this.Id4 = id4;
        this.Id5 = id5;
    }

    private readonly AdditionalId? additionalIds;

    private RecursiveChecker(RecursiveChecker checker, ulong additionalId)
    {
        this.Id0 = checker.Id0;
        this.Id1 = checker.Id1;
        this.Id2 = checker.Id2;
        this.Id3 = checker.Id3;
        this.Id4 = checker.Id4;
        this.Id5 = checker.Id5;
        this.additionalIds = new(additionalId, checker.additionalIds);
    }

    public int TryAdd(uint machineSerial, ulong id, out RecursiveChecker newDetection)
    {// -1: Id collision, 0: Machine collision, 1: No collision
        var result = 1;
        ReadOnlySpan<ulong> ids = [this.Id0, this.Id1, this.Id2, this.Id3, this.Id4, this.Id5];
        var count = 0;
        foreach (var previous in ids)
        {
            if (previous == 0)
            {
                break;
            }

            count++;
            if (!CheckId(previous))
            {
                newDetection = default;
                return -1;
            }
        }

        for (var additional = this.additionalIds; additional is not null; additional = additional.Next)
        {
            if (!CheckId(additional.Id))
            {
                newDetection = default;
                return -1;
            }
        }

        // Keep shallow call chains inline. Overflow nodes are immutable so inherited
        // execution contexts cannot modify the chain of a parent or sibling call.
        newDetection = count switch
        {
            0 => new(id),
            1 => new(this.Id0, id),
            2 => new(this.Id0, this.Id1, id),
            3 => new(this.Id0, this.Id1, this.Id2, id),
            4 => new(this.Id0, this.Id1, this.Id2, this.Id3, id),
            5 => new(this.Id0, this.Id1, this.Id2, this.Id3, this.Id4, id),
            _ => new(this, id),
        };
        return result;

        bool CheckId(ulong previous)
        {
            if (previous >> 32 != machineSerial)
            {
                return true;
            }

            result = 0;
            return previous != id;
        }
    }

    private sealed class AdditionalId(ulong id, AdditionalId? next)
    {
        public ulong Id { get; } = id;

        public AdditionalId? Next { get; } = next;
    }

    /*public bool TryAdd(ulong id, out RecursiveDetection newDetection)
    {
        if (this.Id0 == 0)
        {
            newDetection = new(id);
        }
        else if (this.Id1 == 0)
        {
            if (id == this.Id0)
            {
                newDetection = default;
                return false;
            }

            newDetection = new(this.Id0, id);
        }
        else if (this.Id2 == 0)
        {
            if (id == this.Id0 || id == this.Id1)
            {
                newDetection = default;
                return false;
            }

            newDetection = new(this.Id0, this.Id1, id);
        }
        else if (this.Id3 == 0)
        {
            if (id == this.Id0 || id == this.Id1 || id == this.Id2)
            {
                newDetection = default;
                return false;
            }

            newDetection = new(this.Id0, this.Id1, this.Id2, id);
        }
        else if (this.Id4 == 0)
        {
            if (id == this.Id0 || id == this.Id1 || id == this.Id2 || id == this.Id3)
            {
                newDetection = default;
                return false;
            }

            newDetection = new(this.Id0, this.Id1, this.Id2, this.Id3, id);
        }
        else if (this.Id5 == 0)
        {
            if (id == this.Id0 || id == this.Id1 || id == this.Id2 || id == this.Id3 || id == this.Id4)
            {
                newDetection = default;
                return false;
            }

            newDetection = new(this.Id0, this.Id1, this.Id2, this.Id3, this.Id4, id);
        }
        else
        {
            if (id == this.Id0 || id == this.Id1 || id == this.Id2 || id == this.Id3 || id == this.Id4 || id == this.Id5)
            {
                newDetection = default;
                return false;
            }

            newDetection = new(this.Id0, this.Id1, this.Id2, this.Id3, this.Id4, this.Id5);
        }

        return true;
    }*/

    public override string ToString()
    {
        const string IdToString = "x4";
        return $"{((ushort)this.Id0).ToString(IdToString)}, {((ushort)this.Id1).ToString(IdToString)}, {((ushort)this.Id2).ToString(IdToString)}, {((ushort)this.Id3).ToString(IdToString)}, {((ushort)this.Id4).ToString(IdToString)}, {((ushort)this.Id5).ToString(IdToString)}, ";
    }
}
