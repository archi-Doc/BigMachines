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
        /// <param name="hasAsync">True if the machine has async method.</param>
        /// <param name="continuous">True if the machine is continuous.</param>
        /// <param name="constructor">Constructor delegate of <see cref="Machine{TIdentifier}"/>.</param>
        /// <param name="groupType"><see cref="Type"/> of machine group (if you want to use customized <see cref="MachineGroup{TIdentifier}"/>).</param>
        public MachineInfo(Type machineType, uint typeId, bool hasAsync, bool continuous, Func<BigMachine<TIdentifier>, Machine<TIdentifier>>? constructor, Type? groupType = null)
        {
            this.MachineType = machineType;
            this.TypeId = typeId;
            this.HasAsync = hasAsync;
            this.Continuous = continuous;
            this.Constructor = constructor;
            this.GroupType = groupType;
        }

        /// <summary>
        /// Gets <see cref="Type"/> of machine.
        /// </summary>
        public Type MachineType { get; }

        /// <summary>
        /// Gets Type id (unique identifier for serialization).
        /// </summary>
        public uint TypeId { get; }

        /// <summary>
        /// Gets a value indicating whether or not the machine has async method.
        /// </summary>
        public bool HasAsync { get; }

        /// <summary>
        /// Gets a value indicating whether or not the machine is continuous machine.
        /// </summary>
        public bool Continuous { get; }

        /// <summary>
        /// Gets a constructor delegate of <see cref="Machine{TIdentifier}"/>.
        /// </summary>
        public Func<BigMachine<TIdentifier>, Machine<TIdentifier>>? Constructor { get; }

        /// <summary>
        /// Gets <see cref="Type"/> of machine group.
        /// </summary>
        public Type? GroupType { get; }
    }
}
