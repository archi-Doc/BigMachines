// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace BigMachines;

public partial class Machine
{
    /// <summary>
    /// Provides a user-facing handle for an identified, stateful machine.
    /// </summary>
    /// <typeparam name="TIdentifier">The machine identifier type.</typeparam>
    /// <typeparam name="TState">The machine state type.</typeparam>
    public abstract class MachineHandle<TIdentifier, TState> : MachineHandle<TState>
        where TIdentifier : notnull
        where TState : struct
    {
        public MachineHandle(Machine<TIdentifier> machine)
            : base(machine)
        {
        }

        /// <summary>
        /// Gets the identifier of the machine.
        /// </summary>
        public TIdentifier Identifier => ((Machine<TIdentifier>)this.Machine).Identifier;
    }
}
