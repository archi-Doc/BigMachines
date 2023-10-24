// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1401

namespace BigMachines.Redesign;

public partial class Machine
{
    /// <summary>
    /// An interface class for users to interact with machines.
    /// </summary>
    public abstract class ManMachineInterface
    {// MANMACHINE INTERFACE by Shirow.
        public ManMachineInterface(Machine machine)
        {
            this.machine = machine;
        }

        protected readonly Machine machine;

        /// <summary>
        /// Gets the operational state of the machine.
        /// </summary>
        /// <returns>The operational state of the machine.<br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public OperationalState GetOperationalState()
            => this.machine.OperationalState;

        /// <summary>
        /// Changes the operational state of the machine.
        /// </summary>
        /// <param name="state">The operational state.</param>
        /// <returns><see langword="true"/>: The status is successfully changed.<br/>
        /// <see langword="false"/>: Machine is not available.</returns>
        public bool SetOperationalState(OperationalState state)
        {
            using (this.machine.Semaphore.Lock())
            {
                if (this.machine.OperationalState == OperationalState.Terminated)
                {
                    return false;
                }

                this.machine.OperationalState = state;
            }

            return true;
        }

        /// <summary>
        /// Indicates whether the machine is running (in a Run method).
        /// </summary>
        /// <returns><see langword="true"/>: The machine is running (in a Run method).</returns>
        public bool IsRunning()
            => this.machine.RunType != RunType.NotRunning;

        /// <summary>
        /// Indicates whether the machine is active (in a Run method or waiting to execute).
        /// </summary>
        /// <returns><see langword="true"/>: The machine is active (in a Run method or waiting to execute).</returns>
        public bool IsActive()
            => this.machine.RunType != RunType.NotRunning ||
                    (this.machine.DefaultTimeout is TimeSpan ts && ts > TimeSpan.Zero);

        /// <summary>
        /// Indicates whether the machine is terminated.
        /// </summary>
        /// <returns><see langword="true"/>: The machine is terminated.</returns>
        public bool IsTerminated()
            => this.machine.OperationalState == OperationalState.Terminated;

        /// <summary>
        /// Gets the default timeout of the machine.
        /// </summary>
        /// <returns>The default timeout of the machine.<br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public TimeSpan GetDefaultTimeout()
            => this.machine.DefaultTimeout;

        /// <summary>
        /// Set the timeout of the machine.<br/>
        /// The time decreases while the program is running, and the machine will run when it reaches zero.
        /// </summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="absoluteDateTime">Set <see langword="true"></see> to specify the next execution time by adding the current time and timeout.</param>
        public void SetTimeout(TimeSpan timeout, bool absoluteDateTime = false)
            => this.machine.SetTimeout(timeout, absoluteDateTime);

        public void SetTimeout(TimeSpan timeSpan)
        {
            Volatile.Write(ref this.machine.Timeout, timeSpan.Ticks);
        }

        public async Task RunAsync()
        {
            if (CheckRecursive(1))
            {// Recursive command
                return;
            }

            await this.machine.Semaphore.EnterAsync().ConfigureAwait(false);
            try
            {
                if (await this.machine.RunMachine(RunType.Manual, DateTime.UtcNow).ConfigureAwait(false) == StateResult.Terminate)
                {
                    this.machine.OperationalState = OperationalState.Terminated;
                    this.machine.OnTerminated();
                }
            }
            finally
            {
                this.machine.Semaphore.Exit();

                if (this.machine.OperationalState == OperationalState.Terminated)
                {
                    this.machine.RemoveFromControl();
                }
            }

            bool CheckRecursive(ulong run)
            {
                /*if (command.LoopChecker is { } checker)
                {
                    const uint MachineNumberMask = ~(1u << 31);
                    var id = (run << 63) | (ulong)(this.machine.machineNumber & MachineNumberMask) << 32 | this.TypeId; // Not a perfect solution, though it works in most cases.
                    if (checker.FindId(id))
                    {
                        if (this.machine.Control.BigMachine.LoopCheckerMode != LoopCheckerMode.EnabledAndThrowException)
                        {
                            return true;
                        }

                        var s = string.Join('-', checker.EnumerateId().Select(x => this.BigMachine.GetMachineInfoFromTypeId((uint)x)?.MachineType.Name + "." + IdToString(x)));
                        throw new CircularCommandException($"Circular commands detected ({s})");
                    }

                    checker = checker.Clone();
                    checker.AddId(id);
                    LoopChecker.AsyncLocalInstance.Value = checker;
                }

                return false;

                static string IdToString(ulong id) => (id & (1ul << 63)) == 0 ? "Command" : "Run";*/

                return false;
            }
        }
    }
}
