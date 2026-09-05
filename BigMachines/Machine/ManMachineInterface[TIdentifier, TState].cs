// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace BigMachines;

public partial class Machine
{
    /// <summary>
    /// Provides a user-facing handle for an identified, stateful machine.
    /// </summary>
    /// <typeparam name="TIdentifier">The machine identifier type.</typeparam>
    /// <typeparam name="TState">The machine state type.</typeparam>
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
