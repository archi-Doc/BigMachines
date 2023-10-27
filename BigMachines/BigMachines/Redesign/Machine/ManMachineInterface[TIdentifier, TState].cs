// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace BigMachines.Redesign;

public partial class Machine
{
    /// <summary>
    /// An interface class for users to interact with machines.
    /// </summary>
    /// <typeparam name="TIdentifier">The type of the identifier.</typeparam>
    /// <typeparam name="TState">The type of the machine state.</typeparam>
    public abstract class ManMachineInterface<TIdentifier, TState> : ManMachineInterface<TState>
        where TIdentifier : notnull
        where TState : struct
    {
        public ManMachineInterface(Machine<TIdentifier> machine)
            : base(machine)
        {
        }

        /// <summary>
        /// Gets the identifier of the machine.
        /// </summary>
        public TIdentifier Identifier => ((Machine<TIdentifier>)this.Machine).Identifier;
    }
}
