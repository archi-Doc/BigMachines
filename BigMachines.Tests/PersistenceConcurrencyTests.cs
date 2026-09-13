using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace BigMachines.Tests;

public class PersistenceConcurrencyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task OldHandleCannotRemoveReplacementOrDisposeTwice(int kind)
    {
        DisposableSingleMachine.Disposals = 0;
        DisposableUnorderedMachine.Disposals = 0;
        DisposableSequentialMachine.Disposals = 0;
        var root = new ExecutionRoot();
        var machines = new ReviewBigMachine(root);
        try
        {
            Func<Machine.MachineHandle> create = kind switch
            {
                0 => () => machines.DisposableSingleMachine.GetOrCreate(),
                1 => () => machines.DisposableUnorderedMachine.GetOrCreate(1),
                2 => () => machines.DisposableSequentialMachine.GetOrCreate(1),
                _ => () => machines.ManualControl.GetOrCreate<DisposableSingleMachine>(),
            };
            var old = create();
            Assert.True(old.Terminate());
            var replacement = create();
            await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => old.RunAsync()));
            Assert.False(old.Terminate());
            Assert.Same(replacement, create());
            Assert.False(replacement.IsTerminated);
            Assert.Equal(1, DisposableSingleMachine.Disposals + DisposableUnorderedMachine.Disposals + DisposableSequentialMachine.Disposals);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task ThrowingTerminationCallbackIsReportedAndStillRemovesMachine()
    {
        ThrowingTerminationMachine.Terminations = 0;
        var root = new ExecutionRoot();
        var machines = new ReviewBigMachine(root);
        try
        {
            Assert.Same(machines, machines.ManualControl.BigMachine);
            var machine = machines.ManualControl.GetOrCreate<ThrowingTerminationMachine>();
            Assert.True(machine.Terminate());
            Assert.True(machine.IsTerminated);
            Assert.False(machine.Terminate());
            Assert.Equal(0, machines.ManualControl.Count);
            Assert.Equal(1, ThrowingTerminationMachine.Terminations);
            Assert.Equal(2, ((IBigMachine)machines).GetExceptionCount());
        }
        finally
        {
            await Stop(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task ConcurrentCreationReturnsOneHandleAndSnapshotsRemainIndependent(int kind)
    {
        var root = new ExecutionRoot();
        var machines = new ReviewBigMachine(root);
        try
        {
            Func<Machine.MachineHandle> create = kind switch
            {
                0 => () => machines.DisposableSingleMachine.GetOrCreate(),
                1 => () => machines.DisposableUnorderedMachine.GetOrCreate(1),
                2 => () => machines.DisposableSequentialMachine.GetOrCreate(1),
                _ => () => machines.ManualControl.GetOrCreate<DisposableSingleMachine>(),
            };
            var handles = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(create)));
            Assert.All(handles, handle => Assert.Same(handles[0], handle));
            Assert.Equal(1, machines.GetControls().Sum(control => control.Count));
            var controls = machines.GetControls();
            Array.Clear(controls);
            Assert.All(machines.GetControls(), control => Assert.NotNull(control));
            Assert.Single(machines.GetControls().SelectMany(control => control.GetHandles()));
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task VolatileControlIsExcludedFromSnapshotAndLegacyRestore()
    {
        var root = new ExecutionRoot();
        var source = new TestBigMachine(root);
        var target = new VolatileBigMachine(root);
        try
        {
            await source.SerializableSingleMachine.GetOrCreate().Command.SetValue(42);
            await target.SerializableSingleMachine.GetOrCreate().Command.SetValue(7);
            var bytes = TinyhandSerializer.Serialize(target);
            Assert.Equal(0, ReadMapCount(bytes));
            TinyhandSerializer.DeserializeObject(TinyhandSerializer.Serialize(source), ref target!);
            Assert.Equal(7, (await target.SerializableSingleMachine.GetOrCreate().Command.GetValue()).Response);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task RestoredSequentialQueueStartsWithoutAnAdditionalCreate()
    {
        JournalSequentialMachine.Runs = 0;
        var root = new ExecutionRoot();
        var source = new ReviewBigMachine(root);
        var target = new ReviewBigMachine(root);
        try
        {
            source.JournalSequentialMachine.TryCreate(4);
            source.JournalSequentialMachine.TryCreate(2);
            TinyhandSerializer.DeserializeObject(TinyhandSerializer.Serialize(source), ref target!);
            Assert.Equal(new[] { 4, 2 }, target.JournalSequentialMachine.GetIdentifiers());
            target.Start();
            await WaitUntil(() => Volatile.Read(ref JournalSequentialMachine.Runs) == 2);
            Assert.Equal(0, target.JournalSequentialMachine.Count);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task RestoredMachineJournalsReplayAtTheCorrectIdentifier()
    {
        var root = new ExecutionRoot();
        var source = new ReviewBigMachine(root);
        var restored = new ReviewBigMachine(root);
        var replica = new ReviewBigMachine(root);
        var journal = new MemoryJournal();
        try
        {
            source.JournalMachine.GetOrCreate(1);
            source.JournalMachine.GetOrCreate(2);
            var snapshot = TinyhandSerializer.Serialize(source);
            TinyhandSerializer.DeserializeObject(snapshot, ref restored!);
            TinyhandSerializer.DeserializeObject(snapshot, ref replica!);
            ((IStructuralObject)restored).SetupStructure(journal);
            await restored.JournalMachine.GetOrCreate(2).Command.SetDelay(12345);
            Assert.Single(journal.Records);
            foreach (var record in journal.Records)
            {
                Replay(replica, record);
            }

            Assert.Equal(TimeSpan.MaxValue, replica.JournalMachine.GetOrCreate(1).GetTimeUntilRun());
            Assert.Equal(TimeSpan.FromTicks(12345), replica.JournalMachine.GetOrCreate(2).GetTimeUntilRun());
        }
        finally
        {
            await Stop(root);
        }
    }

    private static int ReadMapCount(byte[] bytes)
    {
        var reader = new TinyhandReader(bytes);
        return reader.ReadMapHeaderOrEmptyArray();
    }

    [Fact]
    public async Task SingleCreationAndDeletionReplayWithoutResurrectingMachine()
    {
        var root = new ExecutionRoot();
        var source = new ReviewBigMachine(root);
        var replica = new ReviewBigMachine(root);
        var journal = new MemoryJournal();
        try
        {
            ((IStructuralObject)source).SetupStructure(journal);
            var machine = source.JournalSingleMachine.GetOrCreate(42);
            Replay(replica, Assert.Single(journal.Records));
            Assert.Equal(42, (await replica.JournalSingleMachine.GetOrCreate().Command.GetValue()).Response);
            journal.Records.Clear();
            Assert.True(machine.Terminate());
            Replay(replica, Assert.Single(journal.Records));
            Assert.False(replica.JournalSingleMachine.TryGet(out _));
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task CollectionAdditionAndDeletionReplayWithInitializedMachine()
    {
        var root = new ExecutionRoot();
        var source = new ReviewBigMachine(root);
        var replica = new ReviewBigMachine(root);
        var journal = new MemoryJournal();
        try
        {
            ((IStructuralObject)source).SetupStructure(journal);
            var machine = source.JournalMachine.GetOrCreate(5);
            Replay(replica, Assert.Single(journal.Records));
            Assert.Equal(CommandStatus.Success, (await replica.JournalMachine.GetOrCreate(5).Command.GetValue()).Status);
            journal.Records.Clear();
            machine.Terminate();
            Replay(replica, Assert.Single(journal.Records));
            Assert.Equal(0, replica.JournalMachine.Count);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task MismatchedStoredIdentifierDoesNotReplaceExistingCollection()
    {
        var root = new ExecutionRoot();
        var source = new ReviewBigMachine(root);
        var target = new ReviewBigMachine(root);
        try
        {
            var original = target.JournalMachine.GetOrCreate(99);
            await source.JournalMachine.GetOrCreate(1).Command.CorruptIdentifier(2);
            var data = TinyhandSerializer.Serialize(source);
            Assert.Throws<TinyhandException>(() => TinyhandSerializer.DeserializeObject(data, ref target!));
            Assert.Same(original, target.JournalMachine.GetOrCreate(99));
            Assert.Equal(1, target.JournalMachine.Count);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task OneShotDelayRunsOnceAndPreservesDisabledSentinel()
    {
        OneShotMachine.Runs = 0;
        var root = new ExecutionRoot();
        var machines = new ReviewBigMachine(root);
        ((IBigMachine)machines).Core.TimeIntervalInMilliseconds = 10;
        try
        {
            var machine = machines.OneShotMachine.GetOrCreate();
            machines.Start();
            await WaitUntil(() => ((IBigMachine)machines).LastRunTime != default);
            Assert.Equal(TimeSpan.MaxValue, machine.GetTimeUntilRun());
            Assert.False(machine.IsActive);
            machine.SetTimeUntilRun(TimeSpan.MinValue);
            Assert.True(machine.IsActive);
            await WaitUntil(() => Volatile.Read(ref OneShotMachine.Runs) == 1);
            await Task.Delay(80, TestContext.Current.CancellationToken);
            Assert.Equal(1, OneShotMachine.Runs);
            Assert.Equal(TimeSpan.MaxValue, machine.GetTimeUntilRun());
            Assert.False(machine.IsActive);
            machine.SetLifespan(TimeSpan.MinValue);
            await WaitUntil(() => machine.IsTerminated && machines.OneShotMachine.Count == 0);
            Assert.Equal(0, machines.OneShotMachine.Count);
        }
        finally
        {
            await Stop(root);
        }
    }

    private static void Replay(IStructuralObject target, byte[] bytes)
    {
        var reader = new TinyhandReader(bytes);
        Assert.True(target.ProcessJournalRecord(ref reader));
        Assert.True(reader.End);
    }

    [Fact]
    public async Task TwoWorkersReserveDifferentMachinesAndRespectConcurrencyLimit()
    {
        TwoWorkerMachine.Starts = 0;
        TwoWorkerMachine.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var root = new ExecutionRoot();
        var machines = new ReviewBigMachine(root);
        try
        {
            for (var i = 0; i < 4; i++)
            {
                machines.TwoWorkerMachine.TryCreate(i);
            }

            machines.Start();
            await WaitUntil(() => Volatile.Read(ref TwoWorkerMachine.Starts) == 2);
            Assert.Equal(2, machines.TwoWorkerMachine.GetHandles().Count(x => x.IsRunning));
            TwoWorkerMachine.Release.TrySetResult();
            await WaitUntil(() => machines.TwoWorkerMachine.Count == 0);
            Assert.Equal(4, TwoWorkerMachine.Starts);
        }
        finally
        {
            TwoWorkerMachine.Release.TrySetResult();
            await Stop(root);
        }
    }

    [Fact]
    public async Task ServiceProviderCannotAttachTheSameMachineToTwoRoots()
    {
        var previous = TinyhandSerializer.ServiceProvider;
        var root = new ExecutionRoot();
        var first = new ReviewBigMachine(root);
        var second = new ReviewBigMachine(root);
        TinyhandSerializer.ServiceProvider = new SingleInstanceProvider(new ReusedServiceMachine());
        try
        {
            var original = first.ManualControl.GetOrCreate<ReusedServiceMachine>();
            Assert.Throws<InvalidOperationException>(() => second.ManualControl.GetOrCreate<ReusedServiceMachine>());
            Assert.Equal(0, second.ManualControl.Count);
            Assert.Same(original, first.ManualControl.GetOrCreate<ReusedServiceMachine>());
        }
        finally
        {
            TinyhandSerializer.ServiceProvider = previous;
            await Stop(root);
        }
    }

    private sealed class SingleInstanceProvider(object value) : IServiceProvider
    {
        public object? GetService(Type serviceType) => value.GetType() == serviceType ? value : null;
    }

    [Fact]
    public async Task ManualStateExceptionIsReportedOnItsOwningRoot()
    {
        var root = new ExecutionRoot();
        var machines = new ReviewBigMachine(root);
        try
        {
            var machine = machines.ManualControl.GetOrCreate<ManualStateFailureMachine>();
            await machine.RunAsync();
            Assert.Equal(1, ((IBigMachine)machines).GetExceptionCount());
            Assert.Equal(0, machines.ManualControl.Count);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task QueuedLifespanExpiresEvenWhenFirstMachineIsPaused()
    {
        var root = new ExecutionRoot();
        var machines = new ReviewBigMachine(root);
        ((IBigMachine)machines).Core.TimeIntervalInMilliseconds = 10;
        try
        {
            var first = machines.DisposableSequentialMachine.GetOrCreate(1);
            var second = machines.DisposableSequentialMachine.GetOrCreate(2);
            first.Pause();
            second.SetLifespan(TimeSpan.Zero);
            machines.Start();
            await WaitUntil(() => machines.DisposableSequentialMachine.Count == 1);
            Assert.True(second.IsTerminated);
            Assert.False(first.IsTerminated);
            Assert.Same(first, machines.DisposableSequentialMachine.PeekFirst());
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task SnapshotUsingMachineSemaphoreWaitsForWholeCommand()
    {
        var root = new ExecutionRoot();
        var machines = new ReviewBigMachine(root);
        var restored = new ReviewBigMachine(root);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var update = machines.SnapshotPairMachine.GetOrCreate().Command.Update(entered, release.Task);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            var snapshot = Task.Run(() => TinyhandSerializer.Serialize(machines));
            await Task.Delay(50, TestContext.Current.CancellationToken);
            Assert.False(snapshot.IsCompleted);
            release.TrySetResult();
            await update;
            TinyhandSerializer.DeserializeObject(await snapshot.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken), ref restored!);
            Assert.Equal((42, 42), (await restored.SnapshotPairMachine.GetOrCreate().Command.GetPair()).Response);
        }
        finally
        {
            release.TrySetResult();
            await Stop(root);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task NilControlClearsPreviousContentsWithoutReplacingControl(int kind)
    {
        var root = new ExecutionRoot();
        var machines = new ReviewBigMachine(root);
        try
        {
            machines.JournalSingleMachine.GetOrCreate();
            machines.JournalMachine.GetOrCreate(1);
            machines.JournalSequentialMachine.TryCreate(2);
            BigMachines.Control.MachineControl control = kind switch
            {
                0 => machines.JournalSingleMachine,
                1 => machines.JournalMachine,
                _ => machines.JournalSequentialMachine,
            };
            var data = NilControl(((IStructuralObject)control).StructuralKey);
            TinyhandSerializer.DeserializeObject(data, ref machines!);
            Assert.Equal(0, control.Count);
            Assert.Contains(control, machines.GetControls());
        }
        finally
        {
            await Stop(root);
        }
    }

    private static byte[] NilControl(int key)
    {
        var writer = TinyhandWriter.CreateFromBytePool();
        try
        {
            writer.WriteMapHeader(1);
            writer.Write(key);
            writer.WriteNil();
            return writer.FlushAndGetArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private static async Task Stop(ExecutionRoot root)
    {
        root.RequestTermination();
        await root.WaitForTerminationAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class MemoryJournal : IStructuralObject, IStructuralRoot
    {
        public List<byte[]> Records { get; } = new();

        public IStructuralRoot? StructuralRoot { get => this; set { } }

        public IStructuralObject? StructuralParent { get; set; }

        public int StructuralKey { get; set; } = -1;

        public bool TryGetJournalWriter(JournalType recordType, out TinyhandWriter writer)
        {
            writer = TinyhandWriter.CreateFromBytePool();
            return true;
        }

        public ulong AddJournalAndDispose(ref TinyhandWriter writer)
        {
            this.Records.Add(writer.FlushAndGetArray());
            writer.Dispose();
            return (ulong)this.Records.Count;
        }

        public void AddToSaveQueue(int delaySeconds = 0) { }
    }
}
