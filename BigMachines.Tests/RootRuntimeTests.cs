using System;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using BigMachines.Control;
using Tinyhand;
using Xunit;

namespace BigMachines.Tests;

public class RootRuntimeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task PendingWorkTracksScheduledMachinesAndExcludedTypes(int kind)
    {
        var root = new ExecutionRoot();
        var machines = new TestBigMachine(root);
        var api = (IBigMachine)machines;
        try
        {
            Assert.False(api.HasPendingWork());
            var (handle, type) = kind switch
            {
                0 => ((Machine.MachineHandle)machines.ScheduledMachine.GetOrCreate(), typeof(ScheduledMachine)),
                1 => (machines.UnorderedTestMachine.GetOrCreate(1), typeof(UnorderedTestMachine)),
                2 => (machines.SequentialIdleMachine.GetOrCreate(1), typeof(SequentialIdleMachine)),
                _ => (machines.ManualControl.GetOrCreate<ManualTestMachine>(), typeof(ManualTestMachine)),
            };

            handle.SetTimeUntilRun(TimeSpan.FromSeconds(1));
            Assert.True(api.HasPendingWork());
            Assert.False(api.HasPendingWork(type));
            Assert.True(api.HasPendingWork(typeof(RootRuntimeTests)));
            Assert.True(handle.Terminate());
            Assert.False(api.HasPendingWork());
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task ExcludedManualTypeDoesNotHideOtherManualMachines()
    {
        var root = new ExecutionRoot();
        var machines = new TestBigMachine(root);
        var api = (IBigMachine)machines;
        try
        {
            var first = machines.ManualControl.GetOrCreate<ManualTestMachine>();
            var second = machines.ManualControl.GetOrCreate<DisposableSingleMachine>();
            first.SetTimeUntilRun(TimeSpan.Zero);
            second.SetTimeUntilRun(TimeSpan.Zero);
            Assert.True(api.HasPendingWork(typeof(ManualTestMachine)));
            Assert.True(second.Terminate());
            Assert.False(api.HasPendingWork(typeof(ManualTestMachine)));
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task ManualExclusionUsesTheRegisteredTypeWhenProviderReturnsDerivedMachine()
    {
        var root = new ExecutionRoot();
        var machines = new TestBigMachine(root);
        var previous = TinyhandSerializer.ServiceProvider;
        try
        {
            TinyhandSerializer.ServiceProvider = new DerivedMachineProvider();
            machines.ManualControl.GetOrCreate<ReusedServiceMachine>().SetTimeUntilRun(TimeSpan.Zero);
            Assert.False(((IBigMachine)machines).HasPendingWork(typeof(ReusedServiceMachine)));
            Assert.True(((IBigMachine)machines).HasPendingWork(typeof(DerivedServiceMachine)));
        }
        finally
        {
            TinyhandSerializer.ServiceProvider = previous;
            await Stop(root);
        }
    }

    [Fact]
    public async Task ExceptionsRemainPendingUntilProcessedAndNullHandlerDoesNotReplaceHandler()
    {
        var root = new ExecutionRoot();
        var machines = new TestBigMachine(root);
        var api = (IBigMachine)machines;
        try
        {
            var observed = 0;
            api.SetExceptionHandler(_ => observed++);
            Assert.Throws<ArgumentNullException>(() => api.SetExceptionHandler(null!));
            var exception = new MachineExceptionInfo(MachineRegistry.CreateMachine<ScheduledMachine>(), new InvalidOperationException());
            api.ReportException(exception);
            Assert.True(api.HasPendingWork(typeof(ScheduledMachine)));
            api.ProcessExceptions();
            Assert.Equal(1, observed);
            Assert.False(api.HasPendingWork());
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task GeneratedPendingWorkChecksDoNotAllocateControlSnapshots()
    {
        var root = new ExecutionRoot();
        var api = (IBigMachine)new TestBigMachine(root);
        try
        {
            for (var i = 0; i < 100; i++)
            {
                _ = api.HasPendingWork();
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            var pending = false;
            for (var i = 0; i < 1000; i++)
            {
                pending |= api.HasPendingWork();
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.False(pending);
            Assert.Equal(0, allocated);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task HandwrittenRootsRetainTheGetControlsFallback()
    {
        var root = new ExecutionRoot();
        var machines = new HandwrittenRoot(root);
        try
        {
            Assert.False(((IBigMachine)machines).HasPendingWork());
            Assert.Equal(1, machines.Snapshots);
            machines.ManualControl.GetOrCreate<ManualTestMachine>().SetTimeUntilRun(TimeSpan.Zero);
            Assert.True(((IBigMachine)machines).HasPendingWork());
            Assert.Equal(2, machines.Snapshots);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public async Task SingleReplacementTerminatesOldInstanceAndPassesCreateParameter()
    {
        var root = new ExecutionRoot();
        var machines = new RootReviewBigMachine(root);
        try
        {
            var firstState = new ReplacementState();
            var first = machines.ReplacementMachine.GetOrCreate(firstState);
            var secondState = new ReplacementState();
            var second = machines.ReplacementMachine.CreateOrReplace(secondState);
            Assert.True(first.IsTerminated);
            Assert.Equal(1, firstState.Terminations);
            Assert.Equal(1, firstState.Disposals);
            Assert.Same(secondState, (await second.Command.GetCreationState()).Response);
            Assert.False(first.Terminate());
            Assert.Same(second, machines.ReplacementMachine.GetOrCreate());

            var third = machines.ReplacementMachine.CreateOrReplace();
            Assert.True(second.IsTerminated);
            Assert.Equal(1, secondState.Terminations);
            Assert.Equal(1, secondState.Disposals);
            Assert.Null((await third.Command.GetCreationState()).Response);
            Assert.Equal(1, machines.ReplacementMachine.Count);
        }
        finally
        {
            await Stop(root);
        }
    }

    [Fact]
    public void RegistryReportsUnregisteredTypesAndRetainsFirstRegistration()
    {
        Assert.False(MachineRegistry.TryGetInformation<RootRuntimeTests>(out var missing));
        Assert.Null(missing);
        Assert.Throws<InvalidOperationException>(() => MachineRegistry.GetInformation<RootRuntimeTests>());
        var information = MachineRegistry.GetInformation<ScheduledMachine>();
        MachineRegistry.Register(information with { WorkerCount = 99 });
        Assert.True(MachineRegistry.TryGetInformation<ScheduledMachine>(out var found));
        Assert.Same(information, found);
        Assert.NotSame(MachineRegistry.CreateMachine<ScheduledMachine>(), MachineRegistry.CreateMachine<ScheduledMachine>());
    }

    private static async Task Stop(ExecutionRoot root)
    {
        root.RequestTermination();
        await root.WaitForTerminationAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class DerivedMachineProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(ReusedServiceMachine) ? new DerivedServiceMachine() : null;
    }

    private sealed class HandwrittenRoot(ExecutionRoot root) : BigMachineBase(root)
    {
        public int Snapshots { get; private set; }

        public override MachineControl[] GetControls()
        {
            this.Snapshots++;
            return [this.ManualControl];
        }
    }
}

[BigMachineObject]
[AddMachine<ReplacementMachine>]
public partial class RootReviewBigMachine;

public sealed class ReplacementState
{
    public int Terminations;
    public int Disposals;
}

[MachineObject]
public partial class ReplacementMachine : Machine, IDisposable
{
    private ReplacementState? creationState;

    public void Dispose()
    {
        if (this.creationState is { } state)
        {
            state.Disposals++;
        }
    }

    protected override void OnCreate(object? createParameter) => this.creationState = createParameter as ReplacementState;

    protected override void OnTerminate()
    {
        if (this.creationState is { } state)
        {
            state.Terminations++;
        }
    }

    [CommandMethod]
    protected CommandResult<ReplacementState?> GetCreationState() => new(this.creationState);
}
