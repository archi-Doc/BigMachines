using System;
using System.Threading;
using System.Threading.Tasks;
using BigMachines;
using Tinyhand;

namespace BigMachines.Tests;

[BigMachineObject]
[AddMachine<DisposableSingleMachine>]
[AddMachine<DisposableUnorderedMachine>]
[AddMachine<DisposableSequentialMachine>]
[AddMachine<JournalMachine>]
[AddMachine<JournalSequentialMachine>]
[AddMachine<JournalSingleMachine>]
[AddMachine<OneShotMachine>]
[AddMachine<TwoWorkerMachine>]
[AddMachine<SnapshotPairMachine>]
public partial class ReviewBigMachine;

[BigMachineObject]
[AddMachine<SerializableSingleMachine>(NonPersistent = true)]
public partial class VolatileBigMachine;

[BigMachineObject]
[AddMachine<CreationJournalSingleMachine>]
[AddMachine<CreationJournalUnorderedMachine>]
[AddMachine<CreationJournalSequentialMachine>]
[AddMachine<ParameterNameMachine>]
public partial class CreationBigMachine;

[TinyhandObject(Structural = true)]
[MachineObject]
public partial class CreationJournalSingleMachine : Machine
{
    public static int Starts;

    protected override void OnCreate(object? createParameter)
        => this.TimeUntilRun = TimeSpan.FromTicks(777);

    protected override void OnStart()
        => Interlocked.Increment(ref Starts);
}

[TinyhandObject(Structural = true)]
[MachineObject]
public partial class CreationJournalUnorderedMachine : Machine<int>
{
    public static int Starts;

    protected override void OnCreate(object? createParameter)
        => this.TimeUntilRun = TimeSpan.FromTicks(777);

    protected override void OnStart()
        => Interlocked.Increment(ref Starts);
}

[TinyhandObject(Structural = true)]
[MachineObject(Control = MachineControlKind.Sequential)]
public partial class CreationJournalSequentialMachine : Machine<int>
{
    public static int Starts;

    protected override void OnCreate(object? createParameter)
        => this.TimeUntilRun = TimeSpan.FromTicks(777);

    protected override void OnStart()
        => Interlocked.Increment(ref Starts);
}

[MachineObject]
public partial class ParameterNameMachine : Machine<int>
{// Parameter names that match generated locals or C# keywords.
    [CommandMethod(GenerateAllCommand = true)]
    protected CommandResult<int> Encode(int e, int control, int machines, int results, int i, int @event)
        => new(e + (control * 10) + (machines * 100) + (results * 1_000) + (i * 10_000) + (@event * 100_000));

    [CommandMethod(WithLock = false, GenerateAllCommand = true)]
    protected Task<CommandStatus> Check(string @class, int value)
        => Task.FromResult(@class == "class" && value == 1 ? CommandStatus.Success : CommandStatus.Failure);
}

public class DerivedServiceMachine : ReusedServiceMachine;

[MachineObject]
public partial class DisposableSingleMachine : Machine, IDisposable
{
    public static int Disposals;

    public void Dispose() => Interlocked.Increment(ref Disposals);
}

[MachineObject]
public partial class DisposableUnorderedMachine : Machine<int>, IDisposable
{
    public static int Disposals;

    public void Dispose() => Interlocked.Increment(ref Disposals);
}

[MachineObject(Control = MachineControlKind.Sequential)]
public partial class DisposableSequentialMachine : Machine<int>, IDisposable
{
    public static int Disposals;

    public void Dispose() => Interlocked.Increment(ref Disposals);
}

[MachineObject(ExcludeFromIncludeAllMachines = true)]
public partial class ThrowingTerminationMachine : Machine, IDisposable
{
    public static int Terminations;

    public void Dispose() => throw new InvalidOperationException("Disposal failed.");

    protected override void OnTerminate()
    {
        Interlocked.Increment(ref Terminations);
        throw new InvalidOperationException("Termination callback failed.");
    }
}

[TinyhandObject(Structural = true)]
[MachineObject]
public partial class JournalMachine : Machine<int>
{
    [Key(10)]
    public int Value { get; set; }

    [CommandMethod]
    protected CommandResult<int> GetValue() => new(this.Value);

    [CommandMethod]
    protected CommandStatus CorruptIdentifier(int identifier)
    {
        this.__identifier__ = identifier;
        return CommandStatus.Success;
    }

    [CommandMethod]
    protected CommandStatus SetDelay(long ticks)
    {
        this.TimeUntilRun = TimeSpan.FromTicks(ticks);
        return CommandStatus.Success;
    }
}

[TinyhandObject(Structural = true)]
[MachineObject(Control = MachineControlKind.Sequential, WorkerCount = 1)]
public partial class JournalSequentialMachine : Machine<int>
{
    public static int Runs;

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Interlocked.Increment(ref Runs);
        return StateResult.Terminate;
    }
}

[TinyhandObject(Structural = true)]
[MachineObject]
public partial class JournalSingleMachine : Machine
{
    [Key(10)]
    public int Value { get; set; }

    protected override void OnCreate(object? createParameter)
        => this.Value = createParameter is int value ? value : 0;

    [CommandMethod]
    protected CommandResult<int> GetValue() => new(this.Value);
}

[MachineObject]
public partial class OneShotMachine : Machine
{
    public static int Runs;

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Interlocked.Increment(ref Runs);
        return StateResult.Continue;
    }
}

[MachineObject(Control = MachineControlKind.Sequential, WorkerCount = 2)]
public partial class TwoWorkerMachine : Machine<int>
{
    public static int Starts;
    public static TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        Interlocked.Increment(ref Starts);
        await Release.Task.ConfigureAwait(false);
        return StateResult.Terminate;
    }
}

[MachineObject(ExcludeFromIncludeAllMachines = true, UseServiceProvider = true)]
public partial class ReusedServiceMachine : Machine;

[MachineObject(ExcludeFromIncludeAllMachines = true)]
public partial class ManualStateFailureMachine : Machine
{
    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
        => throw new InvalidOperationException("Manual state failed.");
}

[TinyhandObject(LockMemberName = nameof(Semaphore))]
[MachineObject]
public partial class SnapshotPairMachine : Machine
{
    [Key(10)]
    public int Left { get; set; }

    [Key(11)]
    public int Right { get; set; }

    [CommandMethod]
    protected async Task<CommandStatus> Update(TaskCompletionSource entered, Task release)
    {
        this.Left = 42;
        entered.TrySetResult();
        await release.ConfigureAwait(false);
        this.Right = 42;
        return CommandStatus.Success;
    }

    [CommandMethod]
    protected CommandResult<(int Left, int Right)> GetPair() => new((this.Left, this.Right));
}
