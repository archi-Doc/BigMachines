﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines.Control;

namespace BigMachines;

public partial class BigMachineBase
{
    public class BigMachineCore : TaskCore
    {
        public int TimeIntervalInMilliseconds { get; set; } = 500; // 500 ms

        public BigMachineCore(BigMachineBase bigMachine)
            : base(null, Process, false)
        {
            this.bigMachine = bigMachine;
        }

        private readonly BigMachineBase bigMachine;

        private static async Task Process(object? parameter)
        {
            var core = (BigMachineCore)parameter!;
            var bigMachine = core.bigMachine;
            var controls = core.bigMachine.GetArray();

            while (!core.IsTerminated)
            {
                if (await core.Delay(core.TimeIntervalInMilliseconds) == false)
                {// Terminated
                    break;
                }

                while (core.bigMachine.exceptionQueue.TryDequeue(out var exception))
                {
                    bigMachine.exceptionHandler(exception);
                }

                var now = DateTime.UtcNow;
                if (bigMachine.lastRun == default)
                {
                    bigMachine.lastRun = now;
                }

                var elapsed = now - bigMachine.lastRun;
                if (elapsed.Ticks < 0)
                {
                    elapsed = default;
                }

                foreach (var x in controls)
                {
                    x.Process(now, elapsed);
                }

                bigMachine.lastRun = now;
            }

            return;
        }
    }
}
