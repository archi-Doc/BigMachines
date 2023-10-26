// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BigMachines.Redesign;

/// <summary>
///  The top-level abstract class for managing groups of machines.
/// </summary>
public abstract class BigMachineBase
{
    public BigMachineBase()
    {
    }

    internal Machine? CreateMachine(MachineInformation? information)
    {
        Machine? machine = null;

        if (information is null)
        {
            return default;
        }

        if (this.ServiceProvider != null)
        {
            machine = this.ServiceProvider.GetService(information.MachineType) as Machine;
        }

        if (machine == null)
        {
            if (information.Constructor != null)
            {
                machine = information.Constructor(this);
            }
            else
            {
                if (this.ServiceProvider == null)
                {
                    throw new InvalidOperationException("ServiceProvider is required to create an instance of machine which does not have default constructor.");
                }
                else
                {
                    throw new InvalidOperationException("ServiceProvider could not create an instance of machine (machine is not registered).");
                }
            }
        }


        if (machine.DefaultTimeout != TimeSpan.Zero && machine.TimeToStart == long.MaxValue)
        {
            Volatile.Write(ref machine.Timeout, 0);
        }

        if (group.Info.Continuous)
        {
            this.Continuous.AddMachine(machine);
        }

        return machine;
    }

    #region FieldAndProperty

    /// <summary>
    /// Gets or sets a value to specify the operation mode of the loop checker.
    /// </summary>
    public LoopCheckerMode LoopCheckerMode { get; set; } = LoopCheckerMode.EnabledAndThrowException;

    /// <summary>
    /// Gets <see cref="IServiceProvider"/> used to create instances of <see cref="Machine{TIdentifier}"/>.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; }

    private ExceptionHandlerDelegate exceptionHandler = DefaultExceptionHandler;
    private ConcurrentQueue<BigMachineException> exceptionQueue = new();

    #endregion

    #region Exception

    /// <summary>
    /// Gets the number of exceptions queued.
    /// </summary>
    /// <returns>The number of exceptions queued.</returns>
    public int GetExceptionCount()
        => this.exceptionQueue.Count;

    /// <summary>
    /// Add the exception to BigMachine's exception queue.
    /// </summary>
    /// <param name="exception">The exception to be queued.</param>
    public void ReportException(BigMachineException exception)
        => this.exceptionQueue.Enqueue(exception);

    /// <summary>
    /// Sets an exception handler.
    /// </summary>
    /// <param name="handler">The exception handler.</param>
    public void SetExceptionHandler(ExceptionHandlerDelegate handler)
        => Volatile.Write(ref this.exceptionHandler, handler);

    /// <summary>
    /// Process queued exceptions using the exception handler.
    /// </summary>
    public void ProcessException()
    {
        while (this.exceptionQueue.TryDequeue(out var exception))
        {
            this.exceptionHandler(exception);
        }
    }

    private static void DefaultExceptionHandler(BigMachineException exception)
        => throw exception.Exception;

    #endregion
}
