using System;
using System.Threading;
using System.Threading.Tasks;
using BigMachines;
using Tinyhand;

namespace BigMachines.Tests;

[BigMachineObject]
[AddMachine<ScheduledMachine>]
[AddMachine<PausedMachine>]
[AddMachine<LongRunningMachine>]
[AddMachine<TerminationMachine>]
[AddMachine<SerializableSingleMachine>]
[AddMachine<SequentialIdleMachine>]
[AddMachine<SequentialCoordinatedMachine>]
[AddMachine<UnorderedTestMachine>]
[AddMachine<ThrowingMachine>]
public partial class TestBigMachine;

[MachineObject]
public partial class ScheduledMachine : Machine
{
    public static int Runs;

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Interlocked.Increment(ref Runs);
        return StateResult.Continue;
    }
}

[MachineObject]
public partial class PausedMachine : Machine
{
    public static int Runs;

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Interlocked.Increment(ref Runs);
        return StateResult.Continue;
    }
}

[MachineObject]
public partial class LongRunningMachine : Machine
{
    private static TaskCompletionSource entered = NewCompletionSource();
    private static TaskCompletionSource release = NewCompletionSource();

    public static Task Entered => entered.Task;

    public static void Reset()
    {
        entered = NewCompletionSource();
        release = NewCompletionSource();
    }

    public static void Release() => release.TrySetResult();

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        entered.TrySetResult();
        await release.Task.ConfigureAwait(false);
        return StateResult.Continue;
    }

    private static TaskCompletionSource NewCompletionSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

[MachineObject]
public partial class TerminationMachine : Machine
{
    public static int Commands;

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
        => StateResult.Continue;

    [CommandMethod]
    protected CommandResult Ping()
    {
        Interlocked.Increment(ref Commands);
        return CommandResult.Success;
    }
}

[TinyhandObject]
[MachineObject]
public partial class SerializableSingleMachine : Machine
{
    public static int Starts;

    [Key(10)]
    public int Value { get; set; }

    protected override void OnStart()
        => Interlocked.Increment(ref Starts);

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
        => StateResult.Continue;

    [CommandMethod]
    protected CommandResult SetValue(int value)
    {
        this.Value = value;
        return CommandResult.Success;
    }

    [CommandMethod]
    protected CommandResult<int> GetValue()
        => new(this.Value);
}

[TinyhandObject]
[MachineObject(Control = MachineControlKind.Sequential, NumberOfTasks = 1)]
public partial class SequentialIdleMachine : Machine<int>
{
    public static int Runs;

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Interlocked.Increment(ref Runs);
        return StateResult.Terminate;
    }
}

[MachineObject(Control = MachineControlKind.Sequential, NumberOfTasks = 1)]
public partial class SequentialCoordinatedMachine : Machine<int>
{
    private static TaskCompletionSource firstEntered = NewCompletionSource();
    private static TaskCompletionSource releaseFirst = NewCompletionSource();
    private static int currentExecutions;

    public SequentialCoordinatedMachine()
    {
        this.DefaultTimeout = TimeSpan.FromMilliseconds(1);
    }

    public static int Starts;

    public static int MaxConcurrency;

    public static Task FirstEntered => firstEntered.Task;

    public static void Reset()
    {
        firstEntered = NewCompletionSource();
        releaseFirst = NewCompletionSource();
        currentExecutions = 0;
        Starts = 0;
        MaxConcurrency = 0;
    }

    public static void ReleaseFirst() => releaseFirst.TrySetResult();

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        var concurrency = Interlocked.Increment(ref currentExecutions);
        UpdateMaximum(concurrency);
        Interlocked.Increment(ref Starts);
        try
        {
            if (this.Identifier == 1)
            {
                firstEntered.TrySetResult();
                await releaseFirst.Task.ConfigureAwait(false);
            }

            return StateResult.Terminate;
        }
        finally
        {
            Interlocked.Decrement(ref currentExecutions);
        }
    }

    private static void UpdateMaximum(int value)
    {
        var current = Volatile.Read(ref MaxConcurrency);
        while (value > current)
        {
            current = Interlocked.CompareExchange(ref MaxConcurrency, value, current);
        }
    }

    private static TaskCompletionSource NewCompletionSource()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

[TinyhandObject]
[MachineObject]
public partial class UnorderedTestMachine : Machine<int>
{
    [Key(10)]
    public int Runs { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        this.Runs++;
        return StateResult.Continue;
    }

    [CommandMethod]
    protected CommandResult<int> GetRuns()
        => new(this.Runs);
}

[MachineObject(Private = true)]
public partial class ManualTestMachine : Machine
{
    public static object? CreateParameter;

    protected override void OnCreate(object? createParam)
        => CreateParameter = createParam;

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
        => StateResult.Continue;
}

[MachineObject]
public partial class ThrowingMachine : Machine
{
    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
        => StateResult.Continue;

    [CommandMethod]
    protected CommandResult Throw()
        => throw new InvalidOperationException("Expected test exception.");
}
