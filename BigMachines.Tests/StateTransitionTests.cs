using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using Tinyhand;
using Tinyhand.IO;
using Xunit;

namespace BigMachines.Tests;

public class StateTransitionTests
{
    [Fact]
    public async Task StateChangesHonorGuardsAndRejectInvalidOrTerminatedTargets()
    {
        var root = new ExecutionRoot();
        var machines = new StateTransitionBigMachine(root);
        var trace = new TransitionTrace();
        try
        {
            var handle = machines.GuardedTransitionMachine.GetOrCreate(1, trace);
            Assert.True(handle.TryGetState(out var initial));
            Assert.Equal(GuardedTransitionMachine.State.Initial, initial);
            Assert.Equal(ChangeStateResult.UnableToExit, handle.ChangeState(GuardedTransitionMachine.State.Ready));

            trace.AllowExit = true;
            Assert.Equal(ChangeStateResult.UnableToEnter, handle.ChangeState(GuardedTransitionMachine.State.Ready));
            Assert.True(handle.TryGetState(out var unchanged));
            Assert.Equal(initial, unchanged);

            trace.AllowEnter = true;
            Assert.Equal(ChangeStateResult.Success, handle.ChangeState(GuardedTransitionMachine.State.Ready));
            trace.AllowEnter = false;
            Assert.Equal(ChangeStateResult.Success, handle.ChangeState(GuardedTransitionMachine.State.Ready));
            Assert.Equal(ChangeStateResult.UnableToEnter, handle.ChangeState((GuardedTransitionMachine.State)99));
            Assert.True(handle.TryGetState(out var ready));
            Assert.Equal(GuardedTransitionMachine.State.Ready, ready);

            Assert.True(handle.Terminate());
            Assert.Equal(ChangeStateResult.Terminated, handle.ChangeState(initial));
            Assert.False(handle.TryGetState(out _));
        }
        finally
        {
            await Stop(root);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RerunDispatchesTheNewStateOnlyAfterTheCurrentHandlerReturns(bool rerun)
    {
        var root = new ExecutionRoot();
        var machines = new StateTransitionBigMachine(root);
        var trace = new TransitionTrace { AllowExit = true, AllowEnter = true, Advance = true, Rerun = rerun };
        try
        {
            var handle = machines.GuardedTransitionMachine.GetOrCreate(1, trace);
            await handle.RunAsync();
            Assert.Equal(ChangeStateResult.Success, trace.TransitionResult);
            Assert.Equal(
                rerun ? new[] { "Initial:before", "Initial:after", "Ready" } : new[] { "Initial:before", "Initial:after" },
                trace.Events);
            Assert.True(handle.TryGetState(out var state));
            Assert.Equal(GuardedTransitionMachine.State.Ready, state);
            Assert.False(handle.IsRunning);

            if (!rerun)
            {
                await handle.RunAsync();
                Assert.Equal(new[] { "Initial:before", "Initial:after", "Ready" }, trace.Events);
            }
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task SameStateTransitionDoesNotRerunOrRequireGuards()
    {
        var root = new ExecutionRoot();
        var machines = new StateTransitionBigMachine(root);
        var trace = new TransitionTrace { RepeatCurrentState = true, Rerun = true };
        try
        {
            var handle = machines.GuardedTransitionMachine.GetOrCreate(1, trace);
            await handle.RunAsync();
            Assert.Equal(ChangeStateResult.Success, trace.TransitionResult);
            Assert.Equal(1, trace.InitialRuns);
            Assert.Equal(new[] { "Initial:before", "Initial:after" }, trace.Events);
            Assert.False(handle.IsTerminated);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task TerminatingStateDoesNotDispatchARequestedRerun()
    {
        var root = new ExecutionRoot();
        var machines = new StateTransitionBigMachine(root);
        var trace = new TransitionTrace { AllowExit = true, AllowEnter = true, Advance = true, Rerun = true, TerminateAfterChange = true };
        try
        {
            var handle = machines.GuardedTransitionMachine.GetOrCreate(1, trace);
            await handle.RunAsync();
            Assert.Equal(ChangeStateResult.Success, trace.TransitionResult);
            Assert.Equal(new[] { "Initial:before", "Initial:after" }, trace.Events);
            Assert.True(handle.IsTerminated);
            Assert.Equal(0, machines.GuardedTransitionMachine.Count);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task AbsoluteTerminationExpiresAPausedMachine()
    {
        var root = new ExecutionRoot();
        var machines = new StateTransitionBigMachine(root);
        var trace = new TransitionTrace();
        ((IBigMachine)machines).Core.TimeIntervalInMilliseconds = 10;
        try
        {
            var handle = machines.GuardedTransitionMachine.GetOrCreate(1, trace);
            Assert.True(handle.Pause());
            handle.SetTerminationTimeFromNow(TimeSpan.FromSeconds(-1));
            Assert.True(handle.GetTerminationTime() < DateTime.UtcNow);
            machines.Start();
            await trace.Terminated.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
            Assert.True(handle.IsTerminated);
            Assert.Empty(trace.Events);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task StateAndSchedulingJournalsReplayOnlyToTheirMachineAndSurviveSnapshots()
    {
        var root = new ExecutionRoot();
        var source = new StateTransitionBigMachine(root);
        var replica = new StateTransitionBigMachine(root);
        var restored = new StateTransitionBigMachine(root);
        var journal = new TransitionJournal();
        try
        {
            source.StateJournalMachine.GetOrCreate(1);
            var handle = source.StateJournalMachine.GetOrCreate(2);
            TinyhandSerializer.DeserializeObject(TinyhandSerializer.Serialize(source), ref replica!);
            ((IStructuralObject)source).SetupStructure(journal);

            var delay = TimeSpan.FromTicks(12345);
            var lifespan = TimeSpan.FromHours(2);
            var nextRun = new DateTime(2040, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var termination = nextRun.AddDays(1);
            handle.SetTimeUntilRun(delay);
            handle.SetLifespan(lifespan);
            handle.SetNextRunTime(nextRun);
            handle.SetTerminationTime(termination);
            Assert.Equal(ChangeStateResult.Success, handle.ChangeState(StateJournalMachine.State.Ready));
            await handle.RunAsync();
            var lastRun = handle.GetLastRunTime();
            Assert.NotEqual(default, lastRun);
            Assert.Equal(6, journal.Records.Count);

            // Reapplying a schedule or state must not append duplicate journal records.
            handle.SetTimeUntilRun(delay);
            handle.SetLifespan(lifespan);
            handle.SetNextRunTime(nextRun);
            handle.SetTerminationTime(termination);
            Assert.Equal(ChangeStateResult.Success, handle.ChangeState(StateJournalMachine.State.Ready));
            Assert.Equal(6, journal.Records.Count);

            foreach (var record in journal.Records)
            {
                Replay(replica, record);
            }

            TinyhandSerializer.DeserializeObject(TinyhandSerializer.Serialize(replica), ref restored!);
            foreach (var target in new[] { replica, restored })
            {
                Assert.True(target.StateJournalMachine.TryGet(2, out var changed));
                Assert.True(changed.TryGetState(out var state));
                Assert.Equal(StateJournalMachine.State.Ready, state);
                Assert.Equal(delay, changed.GetTimeUntilRun());
                Assert.Equal(lifespan, changed.GetLifespan());
                Assert.Equal(nextRun, changed.GetNextRunTime());
                Assert.Equal(termination, changed.GetTerminationTime());
                Assert.Equal(lastRun, changed.GetLastRunTime());

                Assert.True(target.StateJournalMachine.TryGet(1, out var untouched));
                Assert.True(untouched.TryGetState(out var initial));
                Assert.Equal(StateJournalMachine.State.Initial, initial);
                Assert.Equal(TimeSpan.MaxValue, untouched.GetTimeUntilRun());
                Assert.Equal(TimeSpan.MaxValue, untouched.GetLifespan());
                Assert.Equal(DateTime.MaxValue, untouched.GetTerminationTime());
                Assert.Equal(default, untouched.GetNextRunTime());
                Assert.Equal(default, untouched.GetLastRunTime());
            }
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

    private static async Task Stop(ExecutionRoot root)
    {
        root.RequestTermination();
        await root.WaitForTerminationAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class TransitionJournal : IStructuralObject, IStructuralRoot
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

[BigMachineObject]
[AddMachine<GuardedTransitionMachine>]
[AddMachine<StateJournalMachine>]
public partial class StateTransitionBigMachine;

public sealed class TransitionTrace
{
    public bool AllowExit;
    public bool AllowEnter;
    public bool Advance;
    public bool Rerun;
    public bool RepeatCurrentState;
    public bool TerminateAfterChange;
    public int InitialRuns;
    public ChangeStateResult TransitionResult;
    public List<string> Events { get; } = new();
    public TaskCompletionSource Terminated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

[MachineObject]
public partial class GuardedTransitionMachine : Machine<int>
{
    private TransitionTrace trace = default!;

    protected override void OnCreate(object? createParameter) => this.trace = (TransitionTrace)createParameter!;

    protected override void OnTerminate() => this.trace.Terminated.TrySetResult();

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        if (++this.trace.InitialRuns > 1)
        {
            // Bound a failed same-state rerun regression instead of hanging the test runner.
            return StateResult.Terminate;
        }

        this.trace.Events.Add("Initial:before");
        if (this.trace.RepeatCurrentState)
        {
            this.trace.TransitionResult = this.ChangeState(State.Initial, this.trace.Rerun);
        }
        else if (this.trace.Advance)
        {
            this.trace.TransitionResult = this.ChangeState(State.Ready, this.trace.Rerun);
        }

        this.trace.Events.Add("Initial:after");
        return this.trace.TerminateAfterChange ? StateResult.Terminate : StateResult.Continue;
    }

    protected bool InitialCanExit() => this.trace.AllowExit;

    protected bool ReadyCanEnter() => this.trace.AllowEnter;

    [StateMethod(1)]
    protected StateResult Ready(StateParameter parameter)
    {
        this.trace.Events.Add("Ready");
        return StateResult.Continue;
    }
}

[TinyhandObject(Structural = true)]
[MachineObject]
public partial class StateJournalMachine : Machine<int>
{
    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter) => StateResult.Continue;

    [StateMethod(1)]
    protected StateResult Ready(StateParameter parameter) => StateResult.Continue;
}
