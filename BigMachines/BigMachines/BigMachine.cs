// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arc.Threading;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines
{
    public class BigMachine<TIdentifier> : IDisposable
        where TIdentifier : notnull
    {
        public BigMachine(ThreadCoreBase parent)
        {
            this.CommandPost = new(parent);
            this.CommandPost.Open(this.DistributeCommand);
        }

        public CommandPost<TIdentifier> CommandPost { get; }

        public ManMachineInterface<TIdentifier, TState>? GetMachine<TState>(TIdentifier identifier)
        {
            if (this.identificationToStateType.TryGetValue(identifier, out var type))
            {
                if (type != typeof(TState))
                {
                    throw new InvalidOperationException();
                }

                return new ManMachineInterface<TIdentifier, TState>(this, identifier);
            }

            return null;
        }

        private void DistributeCommand(CommandPost<TIdentifier>.Command command)
        {
            if (this.identificationToMachine.TryGetValue(command.Identifier, out var machine))
            {
                lock (machine)
                {
                    machine.ProcessCommand(command);
                }
            }
        }

        private ConcurrentDictionary<TIdentifier, MachineBase<TIdentifier>> identificationToMachine = new();
        private ConcurrentDictionary<TIdentifier, Type> identificationToStateType = new();

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls.

        /// <summary>
        /// Finalizes an instance of the <see cref="BigMachine{TIdentifier}"/> class.
        /// </summary>
        ~BigMachine()
        {
            this.Dispose(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// free managed/native resources.
        /// </summary>
        /// <param name="disposing">true: free managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // free managed resources.
                    this.CommandPost.Dispose();
                }

                // free native resources here if there are any.
                this.disposed = true;
            }
        }
        #endregion
    }
}
