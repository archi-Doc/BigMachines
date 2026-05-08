// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Arc.Threading;

namespace BigMachines.Control;

public sealed partial class SequentialMachineControl<TIdentifier, TMachine, TInterface>
    where TIdentifier : notnull
    where TMachine : Machine<TIdentifier>
    where TInterface : Machine.ManMachineInterface
{
    private class SequentialCore : TaskCore<SequentialCore>
    {
        public const double TimeIntervalInMilliseconds = 2_000;

        public SequentialCore(ExecutionRoot root, SequentialMachineControl<TIdentifier, TMachine, TInterface> control)
            : base(root.UnitGroup(BigMachineBase.GroupName), Process, ExecutionCoreOptions.DelayedStart)
        {
            this.control = control;
        }

        public void Start()
        {
            this.SendSignal(ExecutionSignal.Start);
        }

        public void Pulse() => this.updateEvent.Pulse();

        private readonly SequentialMachineControl<TIdentifier, TMachine, TInterface> control;
        private readonly AsyncPulseEvent updateEvent = new();

        private static async Task Process(SequentialCore core)
        {
            var control = core.control;
            while (core.CanContinue)
            {
                /*if (await core.Delay(core.TimeIntervalInMilliseconds) == false)
                {// Terminated
                    break;
                }*/

                if (await core.updateEvent.WaitAsync(TimeSpan.FromMilliseconds(TimeIntervalInMilliseconds), core.CancellationToken).ConfigureAwait(false) != true)
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
