// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1401

namespace BigMachines;

public partial class Machine
{
    /// <summary>
    /// An interface class for users to interact with machines.
    /// </summary>
    public abstract class ManMachineInterface
    {// MANMACHINE INTERFACE by Shirow.
        public ManMachineInterface(Machine machine)
        {
            this.Machine = machine;
        }

        protected internal readonly Machine Machine;

        /// <summary>
        /// Gets the operational state of the machine.
        /// </summary>
        /// <returns>The operational state of the machine.<br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public OperationalFlag GetOperationalState()
            => this.Machine.operationalState;

        public bool TerminateMachine()
        {
            using (this.Machine.Semaphore.Lock())
            {
                if (this.Machine.operationalState.HasFlag(OperationalFlag.Terminated))
                {
                    return false;
                }

                this.Machine.OnTermination();
                this.Machine.operationalState |= OperationalFlag.Terminated;
            }

            return this.Machine.RemoveFromControl();
        }

        public bool PauseMachine()
        {
            using (this.Machine.Semaphore.Lock())
            {
                if (this.Machine.operationalState == OperationalFlag.Terminated)
                {
                    return false;
                }

                this.Machine.operationalState |= OperationalFlag.Paused;
            }

            return true;
        }

        public bool UnpauseMachine()
        {
            using (this.Machine.Semaphore.Lock())
            {
                if (this.Machine.operationalState == OperationalFlag.Terminated)
                {
                    return false;
                }

                this.Machine.operationalState &= ~OperationalFlag.Paused;
            }

            return true;
        }

        /// <summary>
        /// Indicates whether the machine is running (in state methods).
        /// </summary>
        /// <returns><see langword="true"/>: The machine is running (in state methods).</returns>
        public bool IsRunning()
            => this.Machine.operationalState.HasFlag(OperationalFlag.Running) &&
            !this.Machine.operationalState.HasFlag(OperationalFlag.Terminated);

        /// <summary>
        /// Indicates whether the machine is active (in state methods or waiting to execute).
        /// </summary>
        /// <returns><see langword="true"/>: The machine is active (in state methods or waiting to execute).</returns>
        public bool IsActive()
            => !this.Machine.operationalState.HasFlag(OperationalFlag.Terminated) &&
            (this.Machine.operationalState.HasFlag(OperationalFlag.Running) ||
            (this.Machine.DefaultTimeout is TimeSpan ts && ts > TimeSpan.Zero));

        /// <summary>
        /// Indicates whether the machine is terminated.
        /// </summary>
        /// <returns><see langword="true"/>: The machine is terminated.</returns>
        public bool IsTerminated()
            => this.Machine.operationalState.HasFlag(OperationalFlag.Terminated);

        public TimeSpan GetTimeUntilRun()
            => this.Machine.TimeUntilRun;

        public void SetTimeUntilRun(TimeSpan timeUntilRun)
            => this.Machine.TimeUntilRun = timeUntilRun;

        /// <summary>
        /// Gets the last run time of the machine.
        /// </summary>
        /// <returns>The last run time of the machine.</returns>
        public DateTime GetLastRunTime()
            => this.Machine.LastRunTime;

        /// <summary>
        /// Gets the next scheduled execution time.
        /// </summary>
        /// <returns>The next scheduled execution time.</returns>
        public DateTime GetNextRunTime()
            => this.Machine.NextRunTime;

        /// <summary>
        /// Set the next scheduled execution time.
        /// </summary>
        /// <param name="nextRunTime">The next scheduled execution time.</param>
        public void SetNextRunTime(DateTime nextRunTime)
            => this.Machine.NextRunTime = nextRunTime;

        /// <summary>
        /// Set the time interval from now and schedule the next execution time.
        /// </summary>
        /// <param name="timeFromNow">The time interval from now (DateTime.UtcNow).</param>
        public void SetNextRunTimeFromNow(TimeSpan timeFromNow)
            => this.Machine.NextRunTime = DateTime.UtcNow + timeFromNow;

        /// <summary>
        /// Gets the remaining lifespan of the machine.<br/>
        /// When it reaches 0, the machine will terminate.
        /// </summary>
        /// <returns>The remaining lifespan of the machine.</returns>
        public TimeSpan GetLifespan()
            => this.Machine.Lifespan;

        /// <summary>
        /// Set the remaining lifespan of the machine.
        /// </summary>
        /// <param name="lifespan">The remaining lifespan of the machine.</param>
        public void SetLifespan(TimeSpan lifespan)
            => this.Machine.Lifespan = lifespan;

        /// <summary>
        /// Gets the time for the machine to shut down automatically.
        /// </summary>
        /// <returns>The time for the machine to shut down automatically.</returns>
        public DateTime GetTerminationTime()
            => this.Machine.TerminationTime;

        /// <summary>
        /// Set the time for the machine to shut down automatically.
        /// </summary>
        /// <param name="terminationTime">The time for the machine to shut down automatically.</param>
        public void SetTerminationTime(DateTime terminationTime)
            => this.Machine.TerminationTime = terminationTime;

        /// <summary>
        /// Set the time for the machine to shut down automatically.
        /// </summary>
        /// <param name="timeFromNow">The time interval from now (DateTime.UtcNow).</param>
        public void SetTerminationTimeFromNow(TimeSpan timeFromNow)
            => this.Machine.TerminationTime = DateTime.UtcNow + timeFromNow;

        /// <summary>
        /// Gets the default timeout of the machine.
        /// </summary>
        /// <returns>The default timeout of the machine.<br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public TimeSpan GetDefaultTimeout()
            => this.Machine.DefaultTimeout;

        public async Task RunAsync()
        {
            if (CheckRecursive(1))
            {// Recursive command
                return;
            }

            await this.Machine.Semaphore.EnterAsync().ConfigureAwait(false);
            try
            {
                if (await this.Machine.RunMachine(RunType.Manual, DateTime.UtcNow).ConfigureAwait(false) == StateResult.Terminate)
                {
                    this.Machine.operationalState |= OperationalFlag.Terminated;
                    this.Machine.OnTermination();
                }
            }
            finally
            {
                this.Machine.Semaphore.Exit();

                if (this.Machine.operationalState.HasFlag(OperationalFlag.Terminated))
                {
                    this.Machine.RemoveFromControl();
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
