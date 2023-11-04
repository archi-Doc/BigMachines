// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines.Control;
using Tinyhand;

namespace BigMachines.Control;

public sealed partial class SequentialMachineControl<TIdentifier, TMachine, TInterface>
    where TIdentifier : notnull
    where TMachine : Machine<TIdentifier>
    where TInterface : Machine.ManMachineInterface
{
    private class SequentialCore : TaskCore
    {
        public const double TimeIntervalInMilliseconds = 2_000;

        public SequentialCore(SequentialMachineControl<TIdentifier, TMachine, TInterface> control, ThreadCoreBase? parent)
            : base(parent, Process, false)
        {
            this.control = control;
        }

        public void Pulse() => this.updateEvent.Pulse();

        private readonly SequentialMachineControl<TIdentifier, TMachine, TInterface> control;
        private readonly AsyncPulseEvent updateEvent = new();

        private static async Task Process(object? parameter)
        {
            var core = (SequentialCore)parameter!;
            var control = core.control;

            while (!core.IsTerminated)
            {
                /*if (await core.Delay(core.TimeIntervalInMilliseconds) == false)
                {// Terminated
                    break;
                }*/

                await core.updateEvent.WaitAsync(TimeSpan.FromMilliseconds(TimeIntervalInMilliseconds), core.CancellationToken);
                if (core.IsTerminated)
                {
                    break;
                }

                while (!core.IsTerminated)
                {
                    var machine = control.GetMachineToProcess();
                    if (machine is null)
                    {
                        break;
                    }

                    machine.ProcessImmediately(DateTime.UtcNow);
                }
            }

            return;
        }
    }
}
