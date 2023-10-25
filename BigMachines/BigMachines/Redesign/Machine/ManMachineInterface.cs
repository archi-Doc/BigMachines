﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
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
                if (this.machine.OperationalState == OperationalState.Terminated)
                {
                    this.machine.RemoveFromControl();
                }
            }

            return true;
        }

        /// <summary>
        /// Indicates whether the machine is running (in state methods).
        /// </summary>
        /// <returns><see langword="true"/>: The machine is running (in state methods).</returns>
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

        public TimeSpan GetRemainingToRun()
            => new(this.machine.remainingToRun);

        public void SetRemainingToRun(TimeSpan remainingToRun)
            => this.machine.RemainingToRun = remainingToRun.Ticks;

        /// <summary>
        /// Gets the last run time of the machine.
        /// </summary>
        /// <returns>The last run time of the machine.</returns>
        public DateTime GetLastRunTime()
            => this.machine.LastRunTime;

        /// <summary>
        /// Gets the default timeout of the machine.
        /// </summary>
        /// <returns>The default timeout of the machine.<br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public TimeSpan GetDefaultTimeout()
            => this.machine.DefaultTimeout;

        /// <summary>
        /// Set the next scheduled execution time.<br/>
        /// </summary>
        /// <param name="nextRunTime">The next scheduled execution time.</param>
        public void SetNextRunTime(DateTime nextRunTime)
            => this.machine.NextRunTime = nextRunTime;

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