// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Arc.Threading;

namespace BigMachines.Control;

public sealed partial class SequentialMachineControl<TIdentifier, TMachine, THandle>
    where TIdentifier : notnull
    where TMachine : Machine<TIdentifier>
    where THandle : Machine.MachineHandle
{
    private class SequentialCore : TaskCore<SequentialCore>
    {
        public SequentialCore(ExecutionGroup group, SequentialMachineControl<TIdentifier, TMachine, THandle> control)
            : base(group, Process, ExecutionCoreOptions.DelayedStart)
        {
            this.control = control;
        }

        public void Start()
        {
            this.SendSignal(ExecutionSignal.Start);
        }

        public void Pulse() => this.updateEvent.Pulse();

        private readonly SequentialMachineControl<TIdentifier, TMachine, THandle> control;
        private readonly AsyncPulseEvent updateEvent = new();

        private static async Task Process(SequentialCore core)
        {
            var control = core.control;
            while (core.CanContinue)
            {
                /*if (await core.TryDelay(core.TimeIntervalInMilliseconds) == false)
                {// Terminated
                    break;
                }*/

                if (!await core.updateEvent.WaitAsync(core.CancellationToken).ConfigureAwait(false))
                {
                    break;
                }

                while (core.CanContinue)
                {
                    var machine = control.GetMachineToProcess();
                    if (machine is null)
                    {
                        break;
                    }

                    await machine.ProcessImmediately(DateTime.UtcNow).ConfigureAwait(false);
                }
            }

            return;
        }
    }
}
