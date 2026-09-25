using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using Tinyhand;
using Xunit;

namespace BigMachines.Tests;

public class MachineRegressionTests
{
    [Fact]
    public async Task ScheduledRunWaitsUntilSpecifiedTime()
    {
        ScheduledMachine.Runs = 0;
        var (root, bigMachine) = StartBigMachine();
        try
        {
            var machine = bigMachine.ScheduledMachine.GetOrCreate();
            machine.SetNextRunTimeFromNow(TimeSpan.FromMilliseconds(250));

            await Task.Delay(100, TestContext.Current.CancellationToken);
            Assert.Equal(0, Volatile.Read(ref ScheduledMachine.Runs));
            await WaitUntilAsync(() => Volatile.Read(ref ScheduledMachine.Runs) == 1);
        }
        finally
        {
            await StopAsync(root);
        }
    }

    [Fact]
    public async Task PausedMachineDoesNotRunWhenDue()
    {
        PausedMachine.Runs = 0;
        var (root, bigMachine) = StartBigMachine();
        try
        {
            var machine = bigMachine.PausedMachine.GetOrCreate();
            Assert.True(machine.Pause());
            machine.SetTimeUntilRun(TimeSpan.Zero);

            await machine.RunAsync();
            Assert.Equal(0, Volatile.Read(ref PausedMachine.Runs));

            await Task.Delay(150, TestContext.Current.CancellationToken);
            Assert.Equal(0, Volatile.Read(ref PausedMachine.Runs));
        }
        finally
        {
            await StopAsync(root);
        }
    }

    [Fact]
    public async Task IsRunningReflectsExecutionState()
    {
        LongRunningMachine.Reset();
        var (root, bigMachine) = StartBigMachine();
        try
        {
            var machine = bigMachine.LongRunningMachine.GetOrCreate();
            var runTask = machine.RunAsync();
            await LongRunningMachine.Entered.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

            Assert.True(machine.IsRunning);
            LongRunningMachine.Release();
            await runTask.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            Assert.False(machine.IsRunning);
        }
        finally
        {
            LongRunningMachine.Release();
            await StopAsync(root);
        }
    }

    [Fact]
    public async Task ExpiredBusyMachineDoesNotStallOtherTimers()
    {
        LongRunningMachine.Reset();
        ScheduledMachine.Runs = 0;
        var (root, bigMachine) = StartBigMachine();
        try
        {
            var busy = bigMachine.LongRunningMachine.GetOrCreate();
            busy.SetLifespan(TimeSpan.FromMilliseconds(50));
            var runTask = busy.RunAsync();
            await LongRunningMachine.Entered.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            await WaitUntilAsync(() => busy.GetLifespan() <= TimeSpan.Zero);

            // The timer loop must keep serving other machines while the expired machine holds its semaphore.
            bigMachine.ScheduledMachine.GetOrCreate().SetTimeUntilRun(TimeSpan.Zero);
            await WaitUntilAsync(() => Volatile.Read(ref ScheduledMachine.Runs) > 0);
            Assert.False(busy.IsTerminated);

            LongRunningMachine.Release();
            await runTask.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            await WaitUntilAsync(() => busy.IsTerminated && bigMachine.LongRunningMachine.Count == 0);
        }
        finally
        {
            LongRunningMachine.Release();
            await StopAsync(root);
        }
    }

    [Fact]
    public async Task GeneratedCommandsAcceptParameterNamesUsedByGeneratedCode()
    {
        var root = new ExecutionRoot();
        var bigMachine = new CreationBigMachine(root);
        try
        {
            var first = bigMachine.ParameterNameMachine.GetOrCreate(1);
            bigMachine.ParameterNameMachine.GetOrCreate(2);

            Assert.Equal(654_321, (await first.Command.Encode(1, 2, 3, 4, 5, 6)).Response);
            var encoded = await bigMachine.ParameterNameMachine.AllEncode(1, 2, 3, 4, 5, 6);
            Assert.Equal(new[] { 1, 2 }, encoded.Select(x => x.Identifier).Order().ToArray());
            Assert.All(encoded, x => Assert.Equal(654_321, x.Result.Response));

            Assert.Equal(CommandStatus.Success, await first.Command.Check("class", 1));
            Assert.All(await bigMachine.ParameterNameMachine.AllCheck("class", 1), x => Assert.Equal(CommandStatus.Success, x.Status));
        }
        finally
        {
            await StopAsync(root);
        }
    }

    [Fact]
    public async Task TerminatedFlagIsRecognizedWhenCombinedWithPaused()
    {
        TerminationMachine.Commands = 0;
        var root = new ExecutionRoot();
        var bigMachine = new TestBigMachine(root);
        var machine = bigMachine.TerminationMachine.GetOrCreate();

        Assert.True(machine.Pause());
        machine.Terminate();
        Assert.Equal(OperationalFlags.Paused | OperationalFlags.Terminated, machine.GetOperationalState());
        Assert.False(machine.TryGetState(out _));
        Assert.Equal(CommandStatus.Terminated, await machine.Command.Ping());
        Assert.Equal(0, Volatile.Read(ref TerminationMachine.Commands));

        await StopAsync(root);
    }

    [Fact]
    public async Task SerializingEmptySingleControlDoesNotCreateMachine()
    {
        SerializableSingleMachine.Starts = 0;
        var root = new ExecutionRoot();
        var bigMachine = new TestBigMachine(root);

        _ = TinyhandSerializer.Serialize(bigMachine);

        Assert.Equal(0, bigMachine.SerializableSingleMachine.Count);
        Assert.Equal(0, Volatile.Read(ref SerializableSingleMachine.Starts));
        await StopAsync(root);
    }

    [Fact]
    public async Task SequentialWorkerRemainsAvailableAfterIdlePeriod()
    {
        SequentialIdleMachine.Runs = 0;
        var (root, bigMachine) = StartBigMachine();
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(2200), TestContext.Current.CancellationToken);
            Assert.NotNull(bigMachine.SequentialIdleMachine.TryCreate(1));
            await WaitUntilAsync(() => Volatile.Read(ref SequentialIdleMachine.Runs) == 1);
        }
        finally
        {
            await StopAsync(root);
        }
    }

    [Fact]
    public async Task DedicatedSequentialWorkerPreservesConfiguredConcurrency()
    {
        SequentialCoordinatedMachine.Reset();
        var (root, bigMachine) = StartBigMachine();
        try
        {
            Assert.NotNull(bigMachine.SequentialCoordinatedMachine.TryCreate(1));
            Assert.NotNull(bigMachine.SequentialCoordinatedMachine.TryCreate(2));
            await SequentialCoordinatedMachine.FirstEntered.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

            await Task.Delay(100, TestContext.Current.CancellationToken);
            Assert.Equal(1, Volatile.Read(ref SequentialCoordinatedMachine.Starts));
            Assert.Equal(1, Volatile.Read(ref SequentialCoordinatedMachine.MaxConcurrency));

            SequentialCoordinatedMachine.ReleaseFirst();
            await WaitUntilAsync(() => Volatile.Read(ref SequentialCoordinatedMachine.Starts) == 2);
            Assert.Equal(1, Volatile.Read(ref SequentialCoordinatedMachine.MaxConcurrency));
        }
        finally
        {
            SequentialCoordinatedMachine.ReleaseFirst();
            await StopAsync(root);
        }
    }

    [Fact]
    public async Task UnpausingSequentialMachineWakesDedicatedWorker()
    {
        SequentialCoordinatedMachine.Reset();
        var root = new ExecutionRoot();
        var bigMachine = new TestBigMachine(root);
        ((IBigMachine)bigMachine).Core.TimeIntervalInMilliseconds = 10;
        var machine = bigMachine.SequentialCoordinatedMachine.TryCreate(3);
        Assert.NotNull(machine);
        Assert.True(machine.Pause());
        bigMachine.Start();
        try
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
            Assert.Equal(0, Volatile.Read(ref SequentialCoordinatedMachine.Starts));

            Assert.True(machine.Resume());
            await WaitUntilAsync(() => Volatile.Read(ref SequentialCoordinatedMachine.Starts) == 1);
        }
        finally
        {
            await StopAsync(root);
        }
    }

    [Fact]
    public async Task UnorderedControlSupportsLookupReplacementAndBulkRun()
    {
        var root = new ExecutionRoot();
        var bigMachine = new TestBigMachine(root);
        var first = bigMachine.UnorderedTestMachine.GetOrCreate(1);
        var same = bigMachine.UnorderedTestMachine.GetOrCreate(1);
        var second = bigMachine.UnorderedTestMachine.GetOrCreate(2, "create parameter");

        Assert.Same(first, same);
        Assert.True(bigMachine.UnorderedTestMachine.TryGet(2, out var found));
        Assert.Same(second, found);
        Assert.False(bigMachine.UnorderedTestMachine.TryGet(3, out _));
        Assert.Equal(new[] { 1, 2 }, bigMachine.UnorderedTestMachine.GetIdentifiers().Order().ToArray());

        await bigMachine.UnorderedTestMachine.RunAllAsync();
        var firstResult = await first.Command.GetRuns();
        var secondResult = await second.Command.GetRuns();
        Assert.Equal(CommandStatus.Success, firstResult.Status);
        Assert.Equal(1, firstResult.Response);
        Assert.Equal(1, secondResult.Response);

        var replacement = bigMachine.UnorderedTestMachine.CreateOrReplace(1);
        Assert.NotSame(first, replacement);
        Assert.True(first.IsTerminated);
        Assert.Equal(0, (await replacement.Command.GetRuns()).Response);
        Assert.True(replacement.Terminate());
        Assert.False(bigMachine.UnorderedTestMachine.TryGet(1, out _));
        await StopAsync(root);
    }

    [Fact]
    public async Task ManualControlCreatesAndRemovesPrivateMachine()
    {
        ManualTestMachine.CreateParameter = null;
        var root = new ExecutionRoot();
        var bigMachine = new TestBigMachine(root);

        Assert.Null(bigMachine.ManualControl.Find<ManualTestMachine>());
        var machine = bigMachine.ManualControl.TryCreate<ManualTestMachine>("manual parameter");
        Assert.NotNull(machine);
        Assert.Equal("manual parameter", ManualTestMachine.CreateParameter);
        Assert.Same(machine, bigMachine.ManualControl.Find<ManualTestMachine>());
        Assert.Null(bigMachine.ManualControl.TryCreate<ManualTestMachine>());
        Assert.Equal(1, bigMachine.ManualControl.Count);

        Assert.True(machine.Terminate());
        Assert.Equal(0, bigMachine.ManualControl.Count);
        await StopAsync(root);
    }

    [Fact]
    public async Task CommandExceptionIsReportedAndProcessed()
    {
        var root = new ExecutionRoot();
        var bigMachine = new TestBigMachine(root);
        var machine = bigMachine.ThrowingMachine.GetOrCreate();
        MachineExceptionInfo? reported = null;
        ((IBigMachine)bigMachine).SetExceptionHandler(exception => reported = exception);

        Assert.Equal(CommandStatus.Failure, await machine.Command.Throw());
        Assert.Equal(1, ((IBigMachine)bigMachine).GetExceptionCount());
        ((IBigMachine)bigMachine).ProcessExceptions();

        Assert.NotNull(reported);
        Assert.Same(typeof(ThrowingMachine), reported.Machine.GetType());
        Assert.IsType<InvalidOperationException>(reported.Exception);
        Assert.Contains("Expected test exception.", reported.ToString());
        Assert.Equal(0, ((IBigMachine)bigMachine).GetExceptionCount());
        await StopAsync(root);
    }

    [Fact]
    public async Task SerializableControlsRoundTripMachineState()
    {
        SerializableSingleMachine.Starts = 0;
        var sourceRoot = new ExecutionRoot();
        var source = new TestBigMachine(sourceRoot);
        var single = source.SerializableSingleMachine.GetOrCreate();
        Assert.Equal(CommandStatus.Success, await single.Command.SetValue(42));
        var unordered = source.UnorderedTestMachine.GetOrCreate(7);
        await unordered.RunAsync();
        Assert.NotNull(source.SequentialIdleMachine.TryCreate(8));
        var data = TinyhandSerializer.Serialize(source);

        var destinationRoot = new ExecutionRoot();
        var previousServiceProvider = TinyhandSerializer.ServiceProvider;
        TinyhandSerializer.ServiceProvider = new RootServiceProvider(destinationRoot);
        try
        {
            var restored = TinyhandSerializer.Deserialize<TestBigMachine>(data);
            Assert.NotNull(restored);
            Assert.True(restored.SerializableSingleMachine.TryGet(out var restoredSingle));
            Assert.Equal(42, (await restoredSingle.Command.GetValue()).Response);
            Assert.True(restored.UnorderedTestMachine.TryGet(7, out var restoredUnordered));
            Assert.Equal(1, (await restoredUnordered.Command.GetRuns()).Response);
            Assert.NotNull(restored.SequentialIdleMachine.Find(8));
            Assert.Equal(2, Volatile.Read(ref SerializableSingleMachine.Starts));
        }
        finally
        {
            TinyhandSerializer.ServiceProvider = previousServiceProvider;
            await StopAsync(destinationRoot);
            await StopAsync(sourceRoot);
        }
    }

    [Fact]
    public async Task RecursiveDetectionHandlesCapacityAndRejectsDuplicateCall()
    {
        var root = new ExecutionRoot();
        var bigMachine = new TestBigMachine(root);
        var api = (IBigMachine)bigMachine;

        for (uint serial = 1; serial <= 7; serial++)
        {
            var id = ((ulong)serial << 32) | serial;
            Assert.Equal(1, api.CheckCircularCommand(serial, id));
        }

        var alternateId = (1UL << 32) | 100UL;
        Assert.Equal(0, api.CheckCircularCommand(1, alternateId));
        var duplicateId = (1UL << 32) | 1UL;
        var exception = Assert.Throws<CircularCommandException>(() => api.CheckCircularCommand(1, duplicateId));
        Assert.Contains("Circular commands detected", exception.Message);
        await StopAsync(root);
    }

    private static (ExecutionRoot Root, TestBigMachine BigMachine) StartBigMachine()
    {
        var root = new ExecutionRoot();
        var bigMachine = new TestBigMachine(root);
        ((IBigMachine)bigMachine).Core.TimeIntervalInMilliseconds = 10;
        bigMachine.Start();
        return (root, bigMachine);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > TimeSpan.FromSeconds(3))
            {
                throw new TimeoutException("The expected machine state was not reached.");
            }

            await Task.Delay(10);
        }
    }

    private static async Task StopAsync(ExecutionRoot root)
    {
        root.RequestTermination();
        await root.WaitForTerminationAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class RootServiceProvider(ExecutionRoot root) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(ExecutionRoot) ? root : null;
    }
}
