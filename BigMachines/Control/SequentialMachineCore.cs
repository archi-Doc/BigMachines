﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Arc.Threading;

namespace BigMachines.Control;

public sealed partial class SequentialMachineControl<TIdentifier, TMachine, TInterface>
    where TIdentifier : notnull
    where TMachine : Machine<TIdentifier>
    where TInterface : Machine.ManMachineInterface
{
    private class SequentialCore : TaskCore
    {
        public const double TimeIntervalInMilliseconds = 2_000;

        public SequentialCore(SequentialMachineControl<TIdentifier, TMachine, TInterface> control)
            : base(null, Process, false)
        {
            this.control = control;
        }

        public bool Start(ThreadCoreBase? parent)
        {
            if (this.started)
            {
                return false;
            }

            this.ChangeParent(parent);
            this.Start();
            this.started = true;
            return true;
        }

        public void Pulse() => this.updateEvent.Pulse();

        private readonly SequentialMachineControl<TIdentifier, TMachine, TInterface> control;
        private readonly AsyncPulseEvent updateEvent = new();
        private bool started;

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

                await core.updateEvent.WaitAsync(TimeSpan.FromMilliseconds(TimeIntervalInMilliseconds), core.CancellationToken).ConfigureAwait(false);
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

                    await machine.ProcessImmediately(DateTime.UtcNow).ConfigureAwait(false);
                }
            }

            return;
        }
    }
}
