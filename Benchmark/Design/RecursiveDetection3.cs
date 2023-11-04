// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace BigMachines;

internal readonly struct RecursiveDetection3
{
    public static AsyncLocal<RecursiveDetection3> AsyncLocalInstance = new();

    public readonly ulong Id0;
    public readonly ulong Id1;
    public readonly ulong Id2;
    public readonly ulong Id3;

    public RecursiveDetection3(ulong id0, ulong id1 = 0, ulong id2 = 0, ulong id3 = 0)
    {
        this.Id0 = id0;
        this.Id1 = id1;
        this.Id2 = id2;
        this.Id3 = id3;
    }

    public int TryAdd(uint machineSerial, ulong id, out RecursiveDetection3 newDetetcion)
    {// -1: Id collision, 0: Machine collision, 1: No collision
        var result = 1;
        if (this.Id0 == 0)
        {
            newDetetcion = new(id);
            return 1;
        }
        else if (this.Id1 == 0)
        {
            if ((this.Id0 >> 32) == machineSerial)
            {
                if (this.Id0 == id)
                {
                    newDetetcion = default;
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            newDetetcion = new(this.Id0, id);
            return result;
        }
        else if (this.Id2 == 0)
        {
            if ((this.Id0 >> 32) == machineSerial)
            {
                if (this.Id0 == id)
                {
                    newDetetcion = default;
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            if ((this.Id1 >> 32) == machineSerial)
            {
                if (this.Id1 == id)
                {
                    newDetetcion = default;
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            newDetetcion = new(this.Id0, this.Id1, id);
            return result;
        }
        else if (this.Id3 == 0)
        {
            if ((this.Id0 >> 32) == machineSerial)
            {
                if (this.Id0 == id)
                {
                    newDetetcion = default;
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            if ((this.Id1 >> 32) == machineSerial)
            {
                if (this.Id1 == id)
                {
                    newDetetcion = default;
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            if ((this.Id2 >> 32) == machineSerial)
            {
                if (this.Id2 == id)
                {
                    newDetetcion = default;
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            newDetetcion = new(this.Id0, this.Id1, this.Id2, id);
            return result;
        }
        else
        {
            if ((this.Id0 >> 32) == machineSerial)
            {
                if (this.Id0 == id)
                {
                    newDetetcion = default;
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            if ((this.Id1 >> 32) == machineSerial)
            {
                if (this.Id1 == id)
                {
                    newDetetcion = default;
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            if ((this.Id2 >> 32) == machineSerial)
            {
                if (this.Id2 == id)
                {
                    newDetetcion = default;
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            if ((this.Id3 >> 32) == machineSerial)
            {
                if (this.Id3 == id)
                {
                    newDetetcion = default;
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            newDetetcion = this;
            return result;
        }
    }

    public override string ToString()
    {
        const string IdToString = "x4";
        return $"{((ushort)this.Id0).ToString(IdToString)}, {((ushort)this.Id1).ToString(IdToString)}, {((ushort)this.Id2).ToString(IdToString)}, {((ushort)this.Id3).ToString(IdToString)},";
    }
}
