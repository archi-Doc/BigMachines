// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines.Internal;
using Tinyhand;

#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly
#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1602 // Enumeration items should be documented

namespace BigMachines
{
    /// <summary>
    /// <see cref="BigMachine{TIdentifier}"/> is a container class used to manage and manipulate multiple <see cref="Machine{TIdentifier}"/>.
    /// </summary>
    /// <typeparam name="TIdentifier">The type of an identifier.</typeparam>
    public partial class BigMachine<TIdentifier>
        where TIdentifier : notnull
    {
        /// <summary>
        /// Defines the type of delegate used to handle exceptions.
        /// </summary>
        /// <param name="exception">Exception.</param>
        public delegate void ExceptionHandlerDelegate(BigMachineException exception);

        public class BigMachineException
        {
            public BigMachineException(Machine<TIdentifier> machine, Exception exception)
                : base()
            {
                this.Machine = machine;
                this.Exception = exception;
            }

            public Machine<TIdentifier> Machine { get; }

            public Exception Exception { get; }
        }

        public class CommandLoopException : Exception
        {
            public CommandLoopException(string message)
                : base(message)
            {
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BigMachine{TIdentifier}"/> class.
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> used to create instances of <see cref="Machine{TIdentifier}"/>.</param>
        public BigMachine(IServiceProvider? serviceProvider = null)
        {
            MachineLoader.Load<TIdentifier>(); // Load generic machine information.

            this.Status = new();
            this.Core = new ThreadCoreGroup(ThreadCore.Root);
            var mainCore = new ThreadCore(this.Core, this.MainLoop, false);
            this.CommandPost = new(this, this.DistributeCommand);
            this.ServiceProvider = serviceProvider;
            this.Continuous = new(this);

            var array = new IMachineGroup<TIdentifier>[StaticInfo.Count];
            var n = 0;
            foreach (var x in StaticInfo)
            {
                array[n] = this.CreateGroup(x.Value);
                this.interfaceTypeToGroup.TryAdd(x.Key, array[n]);
                n++;
            }

            this.groupArray = array;
            foreach (var x in this.groupArray)
            {
                if (!this.TypeIdToGroup.TryAdd(x.Info.TypeId, x))
                {
                    throw new InvalidOperationException($"Machine: {x.Info.MachineType} Type id: {x.Info.TypeId} is already registered.");
                }

                if (!this.MachineTypeToGroup.TryAdd(x.Info.MachineType, x))
                {
                    throw new InvalidOperationException($"Machine: {x.Info.MachineType} is already registered.");
                }
            }
        }

        /// <summary>
        /// Gets <see cref="MachineInfo{TIdentifier}"/> from the type of Machine.Interface. Used internally.
        /// </summary>
        public static Dictionary<Type, MachineInfo<TIdentifier>> StaticInfo { get; } = new(); // typeof(Machine.Interface), MachineGroup

        /// <summary>
        /// Gets or sets a value indicating whether to enable loop checker.
        /// </summary>
        public bool EnableLoopChecker { get; set; } = true;

        /// <summary>
        /// Gets the status of <see cref="BigMachine{TIdentifier}"/>.
        /// </summary>
        public BigMachineStatus Status { get; }

        /// <summary>
        /// Gets <see cref="ThreadCoreBase"/> of <see cref="BigMachine{TIdentifier}"/>.
        /// </summary>
        public ThreadCoreBase Core { get; }

        /// <summary>
        /// Gets <see cref="CommandPost"/> (message distributing class).
        /// </summary>
        public CommandPost<TIdentifier> CommandPost { get; }

        /// <summary>
        /// Gets a management class of continuous machines.
        /// </summary>
        public BigMachineContinuous<TIdentifier> Continuous { get; }

        /// <summary>
        /// Gets <see cref="IServiceProvider"/> used to create instances of <see cref="Machine{TIdentifier}"/>.
        /// </summary>
        public IServiceProvider? ServiceProvider { get; }

        /// <summary>
        /// Start BigMachine's <see cref="ThreadCore"/>.
        /// </summary>
        public void Start() => this.Core.Start(true);

        public void ReportException(BigMachineException exception)
        {
            this.exceptionQueue.Enqueue(exception);
        }

        public int GetExceptionCount() => this.exceptionQueue.Count;

        /// <summary>
        /// Gets a collection of <see cref="IMachineGroup{TIdentifier}"/>.
        /// </summary>
        /// <returns>A collection containing <see cref="IMachineGroup{TIdentifier}"/>.</returns>
        public IEnumerable<IMachineGroup<TIdentifier>> GetGroups() => this.groupArray;

        /// <summary>
        /// Gets a machine interface associated with the identifier.
        /// </summary>
        /// <typeparam name="TMachineInterface"><see cref="ManMachineInterface{TIdentifier, TState}"/>of the machine (e.g TestMachine.Interface).</typeparam>
        /// <param name="identifier">The identifier.</param>
        /// <returns>An instance of <see cref="ManMachineInterface{TIdentifier, TState}"/><br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public TMachineInterface? TryGet<TMachineInterface>(TIdentifier identifier)
            where TMachineInterface : ManMachineInterface<TIdentifier>
        {
            this.GetMachineGroup(typeof(TMachineInterface), out var group);
            return group.TryGet<TMachineInterface>(identifier);
        }

        /// <summary>
        /// Gets a machine group associated with <typeparamref name="TMachineInterface"/>.
        /// </summary>
        /// <typeparam name="TMachineInterface"><see cref="ManMachineInterface{TIdentifier, TState}"/>of the machine (e.g TestMachine.Interface).</typeparam>
        /// <returns>An instance of <see cref="IMachineGroup{TIdentifier}"/>.</returns>
        public IMachineGroup<TIdentifier> GetGroup<TMachineInterface>()
            where TMachineInterface : ManMachineInterface<TIdentifier>
        {
            this.GetMachineGroup(typeof(TMachineInterface), out var group);
            return group;
        }

        /// <summary>
        /// Create a machine by specifying the identifier and parameter.<br/>
        /// Returns the existing machine if the machine with the identifier is already created.
        /// </summary>
        /// <typeparam name="TMachineInterface"><see cref="ManMachineInterface{TIdentifier, TState}"/>of the machine (e.g TestMachine.Interface).</typeparam>
        /// <param name="identifier">The identifier.</param>
        /// <param name="createParam">The parameter passed to the machine when the machine is newly created.<br/>
        /// Override <see cref="Machine{TIdentifier}.SetParameter(object?)"/> to receive the parameter.</param>
        /// <returns>An instance of <see cref="ManMachineInterface{TIdentifier, TState}"/><br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public TMachineInterface TryCreate<TMachineInterface>(TIdentifier identifier, object? createParam = null)
            where TMachineInterface : ManMachineInterface<TIdentifier>
        {
            this.GetMachineGroup(typeof(TMachineInterface), out var group);

            Machine<TIdentifier>? machine = null;
            if (group.TryGetMachine(identifier, out machine))
            {
                return (TMachineInterface)machine.InterfaceInstance!;
            }

            machine = this.CreateMachine(group);

            machine.CreateInterface(identifier);
            machine.SetParameter(createParam);

            machine = group.GetOrAddMachine(identifier, machine);
            return (TMachineInterface)machine.InterfaceInstance!;
        }

        /// <summary>
        /// Create a machine by specifying the identifier and parameter.<br/>
        /// Removes the existing machine if the machine with the identifier is already created.
        /// </summary>
        /// <typeparam name="TMachineInterface"><see cref="ManMachineInterface{TIdentifier, TState}"/>of the machine (e.g TestMachine.Interface).</typeparam>
        /// <param name="identifier">The identifier.</param>
        /// <param name="createParam">The parameter passed to the machine when the machine is newly created.<br/>
        /// Override <see cref="Machine{TIdentifier}.SetParameter(object?)"/> to receive the parameter.</param>
        /// <returns>An instance of <see cref="ManMachineInterface{TIdentifier, TState}"/><br/>
        /// <see langword="null"/>: Machine is not available.</returns>
        public TMachineInterface Create<TMachineInterface>(TIdentifier identifier, object? createParam = null)
            where TMachineInterface : ManMachineInterface<TIdentifier>
        {
            this.GetMachineGroup(typeof(TMachineInterface), out var group);

            var machine = this.CreateMachine(group);

            machine.CreateInterface(identifier);
            machine.SetParameter(createParam);

            group.AddMachine(identifier, machine);
            return (TMachineInterface)machine.InterfaceInstance!;
        }

        /// <summary>
        /// Remove a machine associated with the identifier.
        /// </summary>
        /// <typeparam name="TMachineInterface"><see cref="ManMachineInterface{TIdentifier, TState}"/>of the machine (e.g TestMachine.Interface).</typeparam>
        /// <param name="identifier">The identifier.</param>
        /// <returns><see langword="true"/>: The machine is successfully removed.</returns>
        public bool Remove<TMachineInterface>(TIdentifier identifier)
        {
            this.GetMachineGroup(typeof(TMachineInterface), out var group);
            return group.TryRemoveMachine(identifier);
        }

        /// <summary>
        /// Serialize machines to a byte array.
        /// </summary>
        /// <param name="options">Serializer options.</param>
        /// <returns>A byte array with the serialized value.</returns>
        public byte[] Serialize(TinyhandSerializerOptions? options = null)
        {
            options ??= TinyhandSerializer.DefaultOptions;
            var writer = default(Tinyhand.IO.TinyhandWriter);

            foreach (var x in this.groupArray)
            {
                SerializeGroup(ref writer, x);
            }

            void SerializeGroup(ref Tinyhand.IO.TinyhandWriter writer, IMachineGroup<TIdentifier> group)
            {
                foreach (var machine in group.GetMachines().Where(a => a.IsSerializable))
                {
                    if (machine is ITinyhandSerialize serializer)
                    {
                        lock (machine.SyncMachine)
                        {
                            writer.WriteArrayHeader(2); // Header
                            writer.Write(machine.TypeId); // Id
                            serializer.Serialize(ref writer, options!); // Data
                        }
                    }
                }
            }

            return writer.FlushAndGetArray();
        }

        /// <summary>
        /// Serialize machines from a byte array.
        /// </summary>
        /// <param name="byteArray">A byte array.</param>
        public void Deserialize(byte[] byteArray)
        {
            var reader = new Tinyhand.IO.TinyhandReader(byteArray);
            var options = TinyhandSerializer.DefaultOptions;

            while (!reader.End)
            {
                if (reader.TryReadNil())
                {
                    break;
                }

                if (reader.ReadArrayHeader() != 2)
                {
                    throw new TinyhandException("Invalid Union data was detected.");
                }

                if (reader.TryReadNil())
                {
                    reader.ReadNil();
                    continue;
                }

                var key = reader.ReadUInt32();
                if (this.TypeIdToGroup.TryGetValue(key, out var group))
                {
                    var machine = this.CreateMachine(group);
                    if (machine is ITinyhandSerialize serializer)
                    {
                        serializer.Deserialize(ref reader, options);
                        machine.CreateInterface(machine.Identifier);
                        group.GetOrAddMachine(machine.Identifier, machine);
                    }
                    else
                    {
                        reader.Skip();
                        continue;
                    }
                }
                else
                {
                    reader.Skip();
                    continue;
                }
            }
        }

        /// <summary>
        /// Sets a timer interval of <see cref="BigMachine{TIdentifier}"/>.<br/>
        /// The default value is TimeSpan.FromMilliseconds(500).
        /// </summary>
        /// <param name="interval">Timer interval.</param>
        public void SetTimerInterval(TimeSpan interval)
        {
            this.timerInterval = interval;
        }

        /// <summary>
        /// Sets a exception handler.
        /// </summary>
        /// <param name="handler">Exception handler.</param>
        public void SetExceptionHandler(ExceptionHandlerDelegate handler)
        {
            Volatile.Write(ref this.exceptionHandler, handler);
        }

        /// <summary>
        /// Gets <see cref="MachineInfo{TIdentifier}"/> associated with the type id.
        /// </summary>
        /// <param name="typeId">The type id.</param>
        /// <returns>The Machine information.</returns>
        public MachineInfo<TIdentifier>? GetMachineInfoFromTypeId(uint typeId)
        {
            if (this.TypeIdToGroup.TryGetValue(typeId, out var group))
            {
                return group.Info;
            }

            return null;
        }

        private static void DefaultExceptionHandler(BigMachineException exception)
        {
            throw exception.Exception;
        }

        internal Dictionary<uint, IMachineGroup<TIdentifier>> TypeIdToGroup { get; } = new();

        internal ThreadsafeTypeKeyHashTable<IMachineGroup<TIdentifier>> MachineTypeToGroup { get; } = new();

        private Task DistributeCommand(CommandPost<TIdentifier>.Command? command, List<CommandPost<TIdentifier>.Command>? commandList)
        {
            return Task.Run(() => // taskrun
            {
                if (command != null)
                {
                    if (command.Channel is IMachineGroup<TIdentifier> group && group.TryGetMachine(command.Identifier, out var machine))
                    {
                        try
                        {
                            machine.DistributeCommand(group, command);
                        }
                        catch (Exception ex)
                        {
                            this.ReportException(new(machine, ex));
                        }
                    }
                }

                if (commandList != null)
                {
                    foreach (var x in commandList)
                    {
                        if (x.Channel is IMachineGroup<TIdentifier> group && group.TryGetMachine(x.Identifier, out var machine))
                        {
                            try
                            {
                                machine.DistributeCommand(group, x);
                            }
                            catch (Exception ex)
                            {
                                this.ReportException(new(machine, ex));
                            }
                        }
                    }
                }
            });
            /*.ContinueWith(
                t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        throw t.Exception;
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);*/
        }

        private Machine<TIdentifier> CreateMachine(IMachineGroup<TIdentifier> group)
        {
            Machine<TIdentifier>? machine = null;

            if (this.ServiceProvider != null)
            {
                machine = this.ServiceProvider.GetService(group.Info.MachineType) as Machine<TIdentifier>;
            }

            if (machine == null)
            {
                if (group.Info.Constructor != null)
                {
                    machine = group.Info.Constructor(this);
                }
                else
                {
                    if (this.ServiceProvider == null)
                    {
                        throw new InvalidOperationException("ServiceProvider is required to create an instance of machine which does not have default constructor.");
                    }
                    else
                    {
                        throw new InvalidOperationException("ServiceProvider could not create an instance of machine (the machine is not registered).");
                    }
                }
            }

            if (machine.DefaultTimeout != TimeSpan.Zero && machine.Timeout == long.MaxValue)
            {
                Volatile.Write(ref machine.Timeout, 0);
            }

            if (group.Info.Continuous)
            {
                this.Continuous.AddMachine(machine);
            }

            return machine;
        }

        private IMachineGroup<TIdentifier> CreateGroup(MachineInfo<TIdentifier> info)
        {
            IMachineGroup<TIdentifier>? group = null;

            if (info.GroupType != null)
            {// Customized group
                Type type = info.GroupType;
                if (type.IsGenericTypeDefinition || type.ContainsGenericParameters)
                {// Open generic: CustomGroup<> -> CustomGroup<TIdentifier>
                    type = type.MakeGenericType(new Type[] { typeof(TIdentifier) });
                }

                if (this.ServiceProvider != null)
                {
                    group = this.ServiceProvider.GetService(type) as MachineGroup<TIdentifier>;
                }

                if (group == null)
                {
                    try
                    {
                        // var args = new Type[] { typeof(BigMachine<TIdentifier>), };
                        // var constructorInfo = info.GroupType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, CallingConventions.HasThis, args, null);
                        // group = constructorInfo.Invoke(new object[] { this, }) as MachineGroup<TIdentifier>;

                        // var exp = Expression.Parameter(typeof(BigMachine<TIdentifier>));
                        // func = Expression.Lambda<Func<BigMachine<TIdentifier>, MachineGroup<TIdentifier>>>(Expression.New(constructorInfo, exp), exp).CompileFast();

                        group = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { this }, null) as IMachineGroup<TIdentifier>;
                    }
                    catch
                    {
                        throw new InvalidOperationException("Constructor with parameter of BigMachine<TIdentifier> is not found.");
                    }
                }
            }

            if (group == null)
            {// Use MachineGroup<TIdentifier>
                if (this.ServiceProvider != null)
                {
                    group = this.ServiceProvider.GetService(typeof(MachineGroup<TIdentifier>)) as MachineGroup<TIdentifier>;
                }

                if (group == null)
                {
                    group = new MachineGroup<TIdentifier>(this);
                }
            }

            group.Assign(info);

            return group;
        }

        private void MainLoop(object? parameter)
        {
            var core = (ThreadCore)parameter!;

            while (!core.IsTerminated)
            {
                if (!core.Sleep(this.timerInterval, TimeSpan.FromMilliseconds(10)))
                {// Terminated
                    break;
                }

                while (this.exceptionQueue.TryDequeue(out var exception))
                {
                    this.exceptionHandler(exception);
                }

                this.Continuous.Process();

                var now = DateTime.UtcNow;
                if (this.Status.LastRun == default)
                {
                    this.Status.LastRun = now;
                }

                var elapsed = now - this.Status.LastRun;
                if (elapsed.Ticks < 0)
                {
                    elapsed = default;
                }

                foreach (var x in this.groupArray)
                {
                    if (x.Info.Continuous)
                    {// Omit continuous machines
                        continue;
                    }

                    foreach (var y in x.GetMachines())
                    {
                        Interlocked.Add(ref y.Timeout, -elapsed.Ticks);
                        Interlocked.Add(ref y.Lifespan, -elapsed.Ticks);

                        if (y.Lifespan <= 0 || y.TerminationDate <= now)
                        {// Terminate
                            lock (y.SyncMachine)
                            {
                                x.TryRemoveMachine(y.Identifier);
                            }
                        }
                        else if (y.Timeout <= 0 || y.NextRun >= now || y.RunType == RunType.NotRunning)
                        {// Screening
                            Task.Run(() => // taskrun
                            {
                                lock (y.SyncMachine)
                                {
                                    TryRun(y);
                                }
                            });
                        }
                    }
                }

                this.Status.LastRun = now;

                void TryRun(Machine<TIdentifier> machine)
                {// locked
                    var runFlag = false;
                    if (machine.Timeout <= 0)
                    {// Timeout
                        if (machine.DefaultTimeout <= TimeSpan.Zero)
                        {
                            Volatile.Write(ref machine.Timeout, long.MinValue);
                        }
                        else
                        {
                            Volatile.Write(ref machine.Timeout, machine.DefaultTimeout.Ticks);
                        }

                        runFlag = true;
                    }

                    if (machine.NextRun >= now)
                    {
                        machine.NextRun = default;
                        runFlag = true;
                    }

                    if (!runFlag)
                    {
                        return;
                    }

                    machine.RunMachine(null, RunType.Timer, now);
                }
            }

            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetMachineGroup(Type interfaceType, out IMachineGroup<TIdentifier> info)
        {
            if (!this.interfaceTypeToGroup.TryGetValue(interfaceType, out info!))
            {
                /*if (interfaceType.BaseType is { } baseType)
                {
                    if (baseType.GetGenericTypeDefinition() == typeof(ManMachineInterface<,>))
                    {
                        if (baseType.GenericTypeArguments.Length > 0 &&
                            baseType.GenericTypeArguments[0] != typeof(TIdentifier))
                        {// Identifier type mismatch
                        }
                    }
                }*/

                throw new InvalidOperationException($"Machine interface {interfaceType.FullName} is not registered.");
            }
        }

        private ThreadsafeTypeKeyHashTable<IMachineGroup<TIdentifier>> interfaceTypeToGroup = new();
        private IMachineGroup<TIdentifier>[] groupArray = Array.Empty<IMachineGroup<TIdentifier>>();
        private TimeSpan timerInterval = TimeSpan.FromMilliseconds(500);
        private ExceptionHandlerDelegate exceptionHandler = DefaultExceptionHandler;
        private ConcurrentQueue<BigMachineException> exceptionQueue = new();
    }
}
