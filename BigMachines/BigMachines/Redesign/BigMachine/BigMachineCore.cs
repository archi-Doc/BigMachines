// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Threading;
using System;
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
            var array = core.bigMachine.GetArray();

            while (!core.IsTerminated)
            {
                if (!core.Sleep(bigMachine.timerInterval, TimeSpan.FromMilliseconds(10)))
                {// Terminated
                    break;
                }

                while (core.bigMachine.exceptionQueue.TryDequeue(out var exception))
                {
                    bigMachine.exceptionHandler(exception);
                }

                bigMachine.Continuous.Process();

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
                foreach (var x in array)
                {
                    canRun = true;
                    if (x.MachineInformation.Continuous)
                    {// Omit continuous machines
                        continue;
                    }
                    else if (x.MachineInformation.GroupType == typeof(QueueGroup<>))
                    {
                        canRun = x.GetMachines().All(a => a.RunType == RunType.NotRunning);
                    }

                    foreach (var y in x.GetMachines())
                    {
                        Interlocked.Add(ref y.lifespan, -elapsed.Ticks);
                        if (y.Status == OperationalFlag.Running)
                        {
                            Interlocked.Add(ref y.timeToStart, -elapsed.Ticks);
                        }

                        if (y.lifespan <= 0 || y.TerminationDate <= now)
                        {// Terminate
                            y.TerminateAndRemoveFromGroup().Wait();
                        }
                        else if (canRun && (y.timeToStart <= 0 || y.nextRunTime >= now) && y.RunType == RunType.NotRunning)
                        {// Screening
                            if (x.MachineInformation.GroupType == typeof(QueueGroup<>))
                            {
                                canRun = false;
                            }

                            Task.Run(() => // taskrun
                            {
                                try
                                {
                                    y.LockMachine();
                                    TryRun(y);
                                }
                                finally
                                {
                                    y.UnlockMachine();
                                    if (y.Status == OperationalFlag.Terminated)
                                    {
                                        y.RemoveFromGroup();
                                    }
                                }
                            });
                        }
                    }
                }

                bigMachine.LastRun = now;

                void TryRun(Machine machine)
                {// locked
                    var runFlag = false;
                    if (machine.timeToStart <= 0)
                    {// Timeout
                        if (machine.DefaultTimeout <= TimeSpan.Zero)
                        {
                            Volatile.Write(ref machine.timeToStart, long.MinValue);
                        }
                        else
                        {
                            Volatile.Write(ref machine.timeToStart, machine.DefaultTimeout.Ticks);
                        }

                        runFlag = true;
                    }

                    if (machine.nextRunTime >= now)
                    {
                        machine.nextRunTime = default;
                        runFlag = true;
                    }

                    if (!runFlag)
                    {
                        return;
                    }

                    if (machine.RunMachine(null, RunType.Timer, now).Result == StateResult.Terminate)
                    {
                        machine.operationalState |= OperationalFlag.Terminated;
                        machine.OnTerminated();
                    }
                }
            }

            return;
        }
    }
}
