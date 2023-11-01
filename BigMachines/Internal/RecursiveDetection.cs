// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace BigMachines;

#pragma warning disable SA1401 // Fields should be private

public enum RecursiveDetectionMode
{
    /// <summary>
    /// Detection of recursive calls is disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// Detection of recursive calls is enabled.
    /// </summary>
    EnabledAndThrowException,
}

internal readonly struct RecursiveDetection
{
    public static AsyncLocal<RecursiveDetection> AsyncLocalInstance = new();
    private const string IdToString = "x4";

    public readonly ulong Id0;
    public readonly ulong Id1;
    public readonly ulong Id2;
    public readonly ulong Id3;
    public readonly ulong Id4;
    public readonly ulong Id5;

    public RecursiveDetection()
    {
    }

    public RecursiveDetection(ulong id0)
    {
        this.Id0 = id0;
    }

    public RecursiveDetection(ulong id0, ulong id1)
    {
        this.Id0 = id0;
        this.Id1 = id1;
    }

    public RecursiveDetection(ulong id0, ulong id1, ulong id2)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
    }

    public RecursiveDetection(ulong id0, ulong id1, ulong id2, ulong id3)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
    }

    public RecursiveDetection(ulong id0, ulong id1, ulong id2, ulong id3, ulong id4)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
        this.Id4 = id4;
    }

    public RecursiveDetection(ulong id0, ulong id1, ulong id2, ulong id3, ulong id4, ulong id5)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
        this.Id4 = id4;
        this.Id5 = id5;
    }

    public bool TryAdd(ulong id, out RecursiveDetection newDetetcion)
    {
        if (this.Id0 == 0)
        {
            newDetetcion = new(id);
        }
        else if (this.Id1 == 0)
        {
            if (id == this.Id0)
            {
                newDetetcion = default;
                return false;
            }

            newDetetcion = new(this.Id0, id);
        }
        else if (this.Id2 == 0)
        {
            if (id == this.Id0 || id == this.Id1)
            {
                newDetetcion = default;
                return false;
            }

            newDetetcion = new(this.Id0, this.Id1, id);
        }
        else if (this.Id3 == 0)
        {
            if (id == this.Id0 || id == this.Id1 || id == this.Id2)
            {
                newDetetcion = default;
                return false;
            }

            newDetetcion = new(this.Id0, this.Id1, this.Id2, id);
        }
        else if (this.Id4 == 0)
        {
            if (id == this.Id0 || id == this.Id1 || id == this.Id2 || id == this.Id3)
            {
                newDetetcion = default;
                return false;
            }

            newDetetcion = new(this.Id0, this.Id1, this.Id2, this.Id3, id);
        }
        else if (this.Id5 == 0)
        {
            if (id == this.Id0 || id == this.Id1 || id == this.Id2 || id == this.Id3 || id == this.Id4)
            {
                newDetetcion = default;
                return false;
            }

            newDetetcion = new(this.Id0, this.Id1, this.Id2, this.Id3, this.Id4, id);
        }
        else
        {
            if (id == this.Id0 || id == this.Id1 || id == this.Id2 || id == this.Id3 || id == this.Id4 || id == this.Id5)
            {
                newDetetcion = default;
                return false;
            }

            newDetetcion = new(this.Id0, this.Id1, this.Id2, this.Id3, this.Id4, this.Id5);
        }

        return true;
    }

    public override string ToString()
        => $"{((ushort)this.Id0).ToString(IdToString)}, {((ushort)this.Id1).ToString(IdToString)}, {((ushort)this.Id2).ToString(IdToString)}, {((ushort)this.Id3).ToString(IdToString)}, {((ushort)this.Id4).ToString(IdToString)}, {((ushort)this.Id5).ToString(IdToString)}, ";
}
