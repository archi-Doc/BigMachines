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
        public int TimeIntervalInMilliseconds { get; set; } = 500; // 500 ms

        public BigMachineCore(ExecutionGroup group, BigMachineBase bigMachine)
            : base(group, Process, ExecutionCoreOptions.DelayedStart)
        {
            this.bigMachine = bigMachine;
        }

        private readonly BigMachineBase bigMachine;

        private static async Task Process(BigMachineCore core)
        {
            var bigMachine = core.bigMachine;
            var controls = core.bigMachine.GetArray();
            var runner = new MachineRunner();
            while (!core.IsTerminated)
            {
                if (await core.Delay(core.TimeIntervalInMilliseconds) == false)
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

            return;
        }
    }
}
