using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using BigMachines.Control;
using Xunit;

namespace BigMachines.Tests;

public class RuntimeReviewTests
{
    [Theory]
    [InlineData(7)]
    [InlineData(12)]
    [InlineData(32)]
    public async Task RecursiveDetectionTracksCallsBeyondInlineCapacity(uint depth)
    {
        var root = new ExecutionRoot();
        var api = (IBigMachine)new RuntimeReviewBigMachine(root);
        try
        {
            for (uint serial = 1; serial <= depth; serial++)
            {
                Assert.Equal(1, api.CheckCircularCommand(serial, ((ulong)serial << 32) | 1));
            }

            Assert.Equal(0, api.CheckCircularCommand(depth, ((ulong)depth << 32) | 2));
            Assert.Throws<CircularCommandException>(() => api.CheckCircularCommand(depth, ((ulong)depth << 32) | 1));
            Assert.Throws<CircularCommandException>(() => api.CheckCircularCommand(depth, ((ulong)depth << 32) | 2));
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task RecursiveDetectionKeepsOverflowLocalToEachExecutionContext()
    {
        var root = new ExecutionRoot();
        var api = (IBigMachine)new RuntimeReviewBigMachine(root);
        try
        {
            for (uint serial = 1; serial <= 8; serial++)
            {
                Assert.Equal(1, api.CheckCircularCommand(serial, ((ulong)serial << 32) | 1));
            }

            await Task.WhenAll(Task.Run(CheckChild, TestContext.Current.CancellationToken), Task.Run(CheckChild, TestContext.Current.CancellationToken));
            Assert.Equal(1, api.CheckCircularCommand(9, (9UL << 32) | 1));

            void CheckChild()
            {
                Assert.Equal(1, api.CheckCircularCommand(9, (9UL << 32) | 1));
                Assert.Throws<CircularCommandException>(() => api.CheckCircularCommand(9, (9UL << 32) | 1));
                Assert.Throws<CircularCommandException>(() => api.CheckCircularCommand(8, (8UL << 32) | 1));
            }
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task StateResultTasksAreReusedAndPreserveUnknownValues()
    {
        foreach (var result in new[] { StateResult.Continue, StateResult.Terminate })
        {
            var first = ResultTaskAccess.Get(result);
            Assert.Same(first, ResultTaskAccess.Get(result));
            Assert.Equal(result, await first);
        }

        Assert.Equal((StateResult)42, await ResultTaskAccess.Get((StateResult)42));
    }

    [Fact]
    public async Task SynchronousStateDoesNotBlockOtherTimerMachines()
    {
        var root = new ExecutionRoot();
        var machines = new RuntimeReviewBigMachine(root);
        ((IBigMachine)machines).Core.TimeIntervalInMilliseconds = 10;
        using var signals = new BlockingSignals();
        BlockingTimerMachine.Handle? blocking = null;
        try
        {
            blocking = machines.BlockingTimerMachine.GetOrCreate(signals);
            machines.Start();
            await signals.Entered.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            machines.TimerProbeMachine.GetOrCreate(completed);
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        }
        finally
        {
            signals.Release.Set();
            if (blocking is not null)
            {
                await blocking.RunAsync();
            }

            await Stop(root);
        }
    }

    [Fact]
    public async Task DedicatedSequentialQueueCountsAsPendingWithoutATimer()
    {
        var root = new ExecutionRoot();
        var machines = new TestBigMachine(root);
        var api = (IBigMachine)machines;
        try
        {
            var queued = machines.SequentialIdleMachine.GetOrCreate(1);
            Assert.Equal(TimeSpan.MaxValue, queued.GetTimeUntilRun());
            Assert.True(machines.SequentialIdleMachine.ContainsActiveMachine());
            Assert.True(api.HasPendingWork());
            Assert.False(api.HasPendingWork(typeof(SequentialIdleMachine)));
            Assert.True(queued.Pause());
            Assert.True(api.HasPendingWork());
            Assert.True(queued.Terminate());
            Assert.False(machines.SequentialIdleMachine.ContainsActiveMachine());
            Assert.False(api.HasPendingWork());
        }
        finally
        {
            await Stop(root);
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public async Task PopulatedControlsCheckActivityWithoutAllocatingEnumerators(int kind, bool active)
    {
        var root = new ExecutionRoot();
        var machines = new ReviewBigMachine(root);
        try
        {
            MachineControl control;
            Machine.MachineHandle handle;
            switch (kind)
            {
                case 0:
                    control = machines.DisposableUnorderedMachine;
                    handle = machines.DisposableUnorderedMachine.GetOrCreate(1);
                    break;
                case 1:
                    control = machines.DisposableSequentialMachine;
                    handle = machines.DisposableSequentialMachine.GetOrCreate(1);
                    break;
                default:
                    control = machines.JournalSequentialMachine;
                    handle = machines.JournalSequentialMachine.GetOrCreate(1);
                    break;
            }

            if (active && kind != 2)
            {
                handle.SetTimeUntilRun(TimeSpan.Zero);
            }

            for (var i = 0; i < 100; i++)
            {
                _ = control.ContainsActiveMachine();
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            var observed = false;
            for (var i = 0; i < 1000; i++)
            {
                observed |= control.ContainsActiveMachine();
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.Equal(active, observed);
            Assert.Equal(0, allocated);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task SequentialTimerPreservesFifoAndExpiresWaitingMachines()
    {
        var root = new ExecutionRoot();
        var machines = new RuntimeReviewBigMachine(root);
        ((IBigMachine)machines).Core.TimeIntervalInMilliseconds = 10;
        var queue = new QueueSignals();
        try
        {
            var first = machines.TimerQueueMachine.GetOrCreate(1, queue);
            first.Pause();
            var expired = machines.TimerQueueMachine.GetOrCreate(2, queue);
            expired.SetLifespan(TimeSpan.Zero);
            machines.TimerQueueMachine.GetOrCreate(3, queue);
            machines.Start();
            await queue.Expired.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            Assert.Empty(queue.Order);
            Assert.True(expired.IsTerminated);
            Assert.True(first.Resume());
            await queue.Completed.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            Assert.Equal(new[] { 1, 3 }, queue.Order.ToArray());
        }
        finally
        {
            await Stop(root);
        }
    }

    private static async Task Stop(ExecutionRoot root)
    {
        root.RequestTermination();
        await root.WaitForTerminationAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class ResultTaskAccess : Machine
    {
        public static Task<StateResult> Get(StateResult result) => __FromStateResult__(result);
    }
}

[BigMachineObject]
[AddMachine<BlockingTimerMachine>]
[AddMachine<TimerProbeMachine>]
[AddMachine<TimerQueueMachine>]
public partial class RuntimeReviewBigMachine;

public sealed class BlockingSignals : IDisposable
{
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ManualResetEventSlim Release { get; } = new();

    public void Dispose() => this.Release.Dispose();
}

[MachineObject]
public partial class BlockingTimerMachine : Machine
{
    private BlockingSignals signals = default!;

    protected override void OnCreate(object? createParameter)
    {
        this.signals = (BlockingSignals)createParameter!;
        this.TimeUntilRun = TimeSpan.Zero;
    }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        this.signals.Entered.TrySetResult();
        if (!this.signals.Release.Wait(TimeSpan.FromSeconds(10)))
        {
            throw new TimeoutException("The blocking timer was not released.");
        }

        return StateResult.Terminate;
    }
}

[MachineObject]
public partial class TimerProbeMachine : Machine
{
    private TaskCompletionSource completed = default!;

    protected override void OnCreate(object? createParameter)
    {
        this.completed = (TaskCompletionSource)createParameter!;
        this.TimeUntilRun = TimeSpan.Zero;
    }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        this.completed.TrySetResult();
        return StateResult.Terminate;
    }
}

public sealed class QueueSignals
{
    public ConcurrentQueue<int> Order { get; } = new();

    public TaskCompletionSource Expired { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

[MachineObject(Control = MachineControlKind.Sequential)]
public partial class TimerQueueMachine : Machine<int>
{
    private QueueSignals signals = default!;

    protected override void OnCreate(object? createParameter)
    {
        this.signals = (QueueSignals)createParameter!;
        this.TimeUntilRun = TimeSpan.Zero;
    }

    protected override void OnTerminate()
    {
        if (this.Identifier == 2)
        {
            this.signals.Expired.TrySetResult();
        }
        else if (this.Identifier == 3)
        {
            this.signals.Completed.TrySetResult();
        }
    }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        this.signals.Order.Enqueue(this.Identifier);
        return StateResult.Terminate;
    }
}
