// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace BigMachines.Redesign;

public partial class Machine
{
    /// <summary>
    /// An interface class for users to interact with machines.
    /// </summary>
    /// <typeparam name="TState">The type of the machine state.</typeparam>
    public abstract class ManMachineInterface<TState> : ManMachineInterface
        where TState : struct
    {
        public ManMachineInterface(Machine machine)
            : base(machine)
        {
        }

        /// <summary>
        /// Gets the current state of the machine.
        /// </summary>
        /// <param name="state">The state of the machine.</param>
        /// <returns>
        /// <see langword="true"/>: the state is successfully retrieved; otherwise <see langword="false"/>.</returns>
        public bool TryGetState(out TState state)
        {
            if (this.machine.OperationalState != OperationalState.Terminated)
            {
                state = Unsafe.As<int, TState>(ref this.machine.machineState);
                return true;
            }

            state = default;
            return false;
        }

        /// <summary>
        /// Changes the state of the machine.
        /// </summary>
        /// <param name="state">The next machine state.</param>
        /// <returns><see langword="true"/> if the state is successfully changed.</returns>
        public ChangeStateResult ChangeState(TState state)
        {
            var result = ChangeStateResult.Terminated;
            var i = Unsafe.As<TState, int>(ref state);

            using (this.machine.Semaphore.Lock())
            {
                if (this.machine.OperationalState == OperationalState.Terminated)
                {// Terminated
                    result = ChangeStateResult.Terminated;
                }
                else
                {
                    result = this.machine.InternalChangeState(i, false);
                }
            }

            return result;
        }
    }
}
