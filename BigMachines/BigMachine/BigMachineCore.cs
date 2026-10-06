// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;

namespace BigMachines;

public partial class BigMachineBase
{
    /// <summary>
    /// Runs periodic processing for a <see cref="BigMachineBase"/> instance.
    /// </summary>
    public class BigMachineCore : TaskCore<BigMachineCore>
    {
        /// <summary>
        /// Gets or sets the polling interval in milliseconds. The default is 500.
        /// </summary>
        public int TimeIntervalInMilliseconds { get; set; } = 500; // 500 ms

        /// <summary>
        /// Initializes a new instance of the <see cref="BigMachineCore"/> class with a delayed start.
        /// </summary>
        /// <param name="group">The execution group that owns the timer.</param>
        /// <param name="bigMachine">The root to process.</param>
        public BigMachineCore(ExecutionGroup group, BigMachineBase bigMachine)
            : base(group, Process, ExecutionCoreOptions.DelayedStart)
        {
            this.bigMachine = bigMachine;
        }

        private readonly BigMachineBase bigMachine;

        private static async Task Process(BigMachineCore core)
        {
            var bigMachine = core.bigMachine;
            var controls = bigMachine.GetControlsCore();
            var runner = new MachineRunner();
            while (!core.IsTerminated)
            {
                if (await core.TryDelay(core.TimeIntervalInMilliseconds).ConfigureAwait(false) == false)
                {// Terminated
                    break;
                }

                while (core.bigMachine.exceptionQueue.TryDequeue(out var exception))
                {
                    Volatile.Read(ref bigMachine.exceptionHandler)(exception);
                }

                var utcNow = DateTime.UtcNow;
                if (bigMachine.lastRun == default)
                {
                    bigMachine.lastRun = utcNow;
                }

                var elapsed = utcNow - bigMachine.lastRun;
                if (elapsed.Ticks < 0)
                {
                    elapsed = default;
                }

                runner.Prepare(utcNow, elapsed);
                foreach (var x in controls)
                {
                    x.Process(runner);
                }

                runner.RunAndClear();

                bigMachine.lastRun = utcNow;
            }
        }
    }
}
