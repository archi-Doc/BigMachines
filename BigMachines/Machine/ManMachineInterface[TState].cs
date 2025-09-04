// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace BigMachines;

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
        /// <see langword="true"/>: the state is successfully retrieved; otherwise <see langword="false"/> (the machine is terminated).</returns>
        public bool TryGetState(out TState state)
        {
            if (this.Machine.__operationalState__ == OperationalFlag.Terminated)
            {
                state = default;
                return false;
            }

            state = Unsafe.As<int, TState>(ref this.Machine.__machineState__);
            return true;
        }

        /// <summary>
        /// Changes the state of the machine.
        /// </summary>
        /// <param name="state">The next machine state.</param>
        /// <returns><see cref="ChangeStateResult"/>.</returns>
        public ChangeStateResult ChangeState(TState state)
        {
            var result = ChangeStateResult.Terminated;
            var i = Unsafe.As<TState, int>(ref state);

            using (this.Machine.Semaphore.EnterScope())
            {
                if (this.Machine.__operationalState__ == OperationalFlag.Terminated)
                {// Terminated
                    result = ChangeStateResult.Terminated;
                }
                else
                {
                    result = this.Machine.__InternalChangeState__(i, false);
                }
            }

            return result;
        }
    }
}
