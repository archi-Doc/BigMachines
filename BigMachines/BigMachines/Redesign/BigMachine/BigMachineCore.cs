// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Arc.Threading;

namespace BigMachines.Redesign;

public partial class BigMachineBase
{
    internal class BigMachineCore : TaskCore
    {
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
                if (await core.Delay(500) == false)
                {// Terminated
                    break;
                }

                while (core.bigMachine.exceptionQueue.TryDequeue(out var exception))
                {
                    bigMachine.exceptionHandler(exception);
                }

                // tempcode
                // bigMachine.Continuous.Process();

                var now = DateTime.UtcNow;
                if (bigMachine.LastRun == default)
                {
                    bigMachine.LastRun = now;
                }

                var elapsed = now - bigMachine.LastRun;
                if (elapsed.Ticks < 0)
                {
                    elapsed = default;
                }

                bool canRun;
                foreach (var x in controls)
                {
                    canRun = true;
                    if (x.MachineInformation.Continuous)
                    {// Omit continuous machines
                        continue;
                    }
                    else if (x.MachineInformation.ControlType == typeof(QueueGroup<>))
                    {
                        canRun = x.GetMachines().All(a => !a.operationalState.HasFlag(OperationalFlag.Running));
                    }

                    foreach (var y in x.GetMachines())
                    {
                        await y.Process(now, elapsed);
                    }
                }
            }

            return;
        }
    }
}
