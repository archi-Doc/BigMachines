// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigMachines
{
    public class MachineGroupInfo<TIdentifier>
        where TIdentifier : notnull
    {
        public MachineGroupInfo(Type machineType, int typeId, Func<BigMachine<TIdentifier>, MachineBase<TIdentifier>>? constructor)
        {
            this.MachineType = machineType;
            this.TypeId = typeId;
            this.Constructor = constructor;
        }

        public Type MachineType { get; }

        public int TypeId { get; }

        public Func<BigMachine<TIdentifier>, MachineBase<TIdentifier>>? Constructor { get; }
    }
}
