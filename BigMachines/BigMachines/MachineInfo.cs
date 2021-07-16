// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigMachines
{
    /// <summary>
    /// Contains information of <see cref="MachineGroup{TIdentifier}"/>.
    /// </summary>
    /// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
    public class MachineInfo<TIdentifier>
        where TIdentifier : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MachineInfo{TIdentifier}"/> class.
        /// </summary>
        /// <param name="machineType"><see cref="Type"/> of machine.</param>
        /// <param name="typeId">Type id (unique identifier for serialization).</param>
        /// <param name="constructor">Constructor delegate of <see cref="MachineBase{TIdentifier}"/>.</param>
        public MachineInfo(Type machineType, int typeId, Func<BigMachine<TIdentifier>, MachineBase<TIdentifier>>? constructor)
        {
            this.MachineType = machineType;
            this.TypeId = typeId;
            this.Constructor = constructor;
        }

        /// <summary>
        /// Gets <see cref="Type"/> of machine.
        /// </summary>
        public Type MachineType { get; }

        /// <summary>
        /// Gets Type id (unique identifier for serialization).
        /// </summary>
        public int TypeId { get; }

        /// <summary>
        /// Gets a constructor delegate of <see cref="MachineBase{TIdentifier}"/>.
        /// </summary>
        public Func<BigMachine<TIdentifier>, MachineBase<TIdentifier>>? Constructor { get; }
    }
}
