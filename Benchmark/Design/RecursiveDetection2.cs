// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;

namespace BigMachines;

internal class RecursiveDetection2
{
    public static AsyncLocal<RecursiveDetection2> AsyncLocalInstance = new();

    public ulong Id0;
    public ulong Id1;
    public ulong Id2;
    public ulong Id3;
    public ulong Id4;
    public ulong Id5;

    public int TryAdd(uint machineSerial, ulong id)
    {// -1: Id collision, 0: Machine collision, 1: No collision
        var result = 1;
        if (this.Id0 == 0)
        {
            this.Id0 = id;
            return 1;
        }
        else if (this.Id1 == 0)
        {
            if ((this.Id0 >> 32) == machineSerial)
            {
                if (this.Id0 == id)
                {
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            this.Id1 = id;
            return result;
        }
        else if (this.Id2 == 0)
        {
            if ((this.Id0 >> 32) == machineSerial)
            {
                if (this.Id0 == id)
                {
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
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            this.Id2 = id;
            return result;
        }
        else if (this.Id3 == 0)
        {
            if ((this.Id0 >> 32) == machineSerial)
            {
                if (this.Id0 == id)
                {
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
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            this.Id3 = id;
            return result;
        }
        else if (this.Id4 == 0)
        {
            if ((this.Id0 >> 32) == machineSerial)
            {
                if (this.Id0 == id)
                {
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
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            this.Id4 = id;
            return result;
        }
        else if (this.Id5 == 0)
        {
            if ((this.Id0 >> 32) == machineSerial)
            {
                if (this.Id0 == id)
                {
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
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            if ((this.Id4 >> 32) == machineSerial)
            {
                if (this.Id4 == id)
                {
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            this.Id5 = id;
            return result;
        }
        else
        {
            if ((this.Id0 >> 32) == machineSerial)
            {
                if (this.Id0 == id)
                {
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
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            if ((this.Id4 >> 32) == machineSerial)
            {
                if (this.Id4 == id)
                {
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            if ((this.Id5 >> 32) == machineSerial)
            {
                if (this.Id5 == id)
                {
                    return -1;
                }
                else
                {
                    result = 0;
                }
            }

            return result;
        }
    }

    public override string ToString()
    {
        const string IdToString = "x4";
        return $"{((ushort)this.Id0).ToString(IdToString)}, {((ushort)this.Id1).ToString(IdToString)}, {((ushort)this.Id2).ToString(IdToString)}, {((ushort)this.Id3).ToString(IdToString)}, {((ushort)this.Id4).ToString(IdToString)}, {((ushort)this.Id5).ToString(IdToString)}, ";
    }
}
