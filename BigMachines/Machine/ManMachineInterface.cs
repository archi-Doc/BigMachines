// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1401

namespace BigMachines;

public partial class Machine
{
    /// <summary>
    /// Provides a user-facing handle for controlling a machine.
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
        /// <returns>The operational state of the machine.</returns>
        public OperationalFlag GetOperationalState()
            => this.Machine.__operationalState__;

        /// <summary>
        /// Terminates and removes this instance. Termination callbacks run once; failures are queued on the root.
        /// </summary>
        /// <returns>Whether this call removed the instance from its control.</returns>
        public bool TerminateMachine()
        {
            using (this.Machine.Semaphore.EnterScope())
            {
                if (this.Machine.__operationalState__.HasFlag(OperationalFlag.Terminated))
                {
                    return false;
                }

                this.Machine.Terminate();
            }

            return this.Machine.RemoveFromControl();
        }

        /// <summary>
        /// Waits for the current operation and pauses state dispatch. Commands remain available.
        /// </summary>
        /// <returns>Whether the machine has not terminated.</returns>
        public bool PauseMachine()
        {
            using (this.Machine.Semaphore.EnterScope())
            {
                if (this.Machine.__operationalState__.HasFlag(OperationalFlag.Terminated))
                {
                    return false;
                }

                this.Machine.__operationalState__ |= OperationalFlag.Paused;
            }

            return true;
        }

        /// <summary>
        /// Resumes state dispatch and wakes dedicated sequential workers.
        /// </summary>
        /// <returns>Whether the machine has not terminated.</returns>
        public bool UnpauseMachine()
        {
            using (this.Machine.Semaphore.EnterScope())
            {
                if (this.Machine.__operationalState__.HasFlag(OperationalFlag.Terminated))
                {
                    return false;
                }

                this.Machine.__operationalState__ &= ~OperationalFlag.Paused;
                this.Machine.MachineControl?.OnMachineUnpaused();
            }

            return true;
        }

        /// <summary>
        /// Gets a value indicating whether a state handler is executing.
        /// </summary>
        public bool IsRunning => this.Machine.IsRunning;

        /// <summary>
        /// Gets a value indicating whether the machine is running or has a pending or periodic execution.
        /// </summary>
        public bool IsActive => this.Machine.IsActive;

        /// <summary>
        /// Gets a value indicating whether the machine is terminated.
        /// </summary>
        public bool IsTerminated => this.Machine.IsTerminated;

        /// <summary>
        /// Gets the remaining timer delay. <see cref="TimeSpan.MaxValue"/> disables this timer.
        /// </summary>
        /// <returns>The remaining delay.</returns>
        public TimeSpan GetTimeUntilRun()
            => this.Machine.TimeUntilRun;

        /// <summary>
        /// Sets the timer delay while holding the machine semaphore.
        /// </summary>
        /// <param name="timeUntilRun">The delay, or <see cref="TimeSpan.MaxValue"/> to disable the timer.</param>
        public void SetTimeUntilRun(TimeSpan timeUntilRun)
        {
            using (this.Machine.Semaphore.EnterScope())
            {
                this.Machine.TimeUntilRun = timeUntilRun;
            }
        }

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
        /// Sets the next UTC execution time while holding the machine semaphore.
        /// </summary>
        /// <param name="nextRunTime">The next scheduled execution time.</param>
        public void SetNextRunTime(DateTime nextRunTime)
        {
            using (this.Machine.Semaphore.EnterScope())
            {
                this.Machine.NextRunTime = nextRunTime;
            }
        }

        /// <summary>
        /// Schedules the next execution relative to the current UTC time.
        /// </summary>
        /// <param name="timeFromNow">The time interval from now (DateTime.UtcNow).</param>
        public void SetNextRunTimeFromNow(TimeSpan timeFromNow)
            => this.SetNextRunTime(DateTime.UtcNow + timeFromNow);

        /// <summary>
        /// Gets the remaining lifespan of the machine.<br/>
        /// When it reaches 0, the machine will terminate.
        /// </summary>
        /// <returns>The remaining lifespan of the machine.</returns>
        public TimeSpan GetLifespan()
            => this.Machine.Lifespan;

        /// <summary>
        /// Sets the remaining lifespan of the machine.
        /// </summary>
        /// <param name="lifespan">The remaining lifespan of the machine.</param>
        public void SetLifespan(TimeSpan lifespan)
        {
            using (this.Machine.Semaphore.EnterScope())
            {
                this.Machine.Lifespan = lifespan;
            }
        }

        /// <summary>
        /// Gets the time for the machine to shut down automatically.
        /// </summary>
        /// <returns>The time for the machine to shut down automatically.</returns>
        public DateTime GetTerminationTime()
            => this.Machine.TerminationTime;

        /// <summary>
        /// Sets the time at which the machine terminates automatically.
        /// </summary>
        /// <param name="terminationTime">The time for the machine to shut down automatically.</param>
        public void SetTerminationTime(DateTime terminationTime)
        {
            using (this.Machine.Semaphore.EnterScope())
            {
                this.Machine.TerminationTime = terminationTime;
            }
        }

        /// <summary>
        /// Schedules automatic termination relative to the current UTC time.
        /// </summary>
        /// <param name="timeFromNow">The time interval from now (DateTime.UtcNow).</param>
        public void SetTerminationTimeFromNow(TimeSpan timeFromNow)
            => this.SetTerminationTime(DateTime.UtcNow + timeFromNow);

        /// <summary>
        /// Gets the default timeout of the machine.
        /// </summary>
        /// <returns>The default timeout of the machine.</returns>
        public TimeSpan GetDefaultTimeout()
            => this.Machine.DefaultTimeout;

        /// <summary>
        /// Runs a state handler while holding the machine semaphore; paused or terminated machines are skipped.
        /// </summary>
        /// <returns>A task that completes after dispatch and any resulting termination.</returns>
        /// <remarks>Do not call this method from a handler that already holds the same machine semaphore.</remarks>
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
                    this.Machine.Terminate();
                }
            }
            finally
            {
                this.Machine.Semaphore.Exit();

                if (this.Machine.__operationalState__.HasFlag(OperationalFlag.Terminated))
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
