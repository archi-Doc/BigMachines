# BigMachines

[![NuGet](https://img.shields.io/nuget/v/BigMachines)](https://www.nuget.org/packages/BigMachines)
[![Build and Test](https://github.com/archi-Doc/BigMachines/actions/workflows/test.yml/badge.svg)](https://github.com/archi-Doc/BigMachines/actions/workflows/test.yml)

BigMachines is a source-generated state-machine library for .NET. It provides typed machine controls, asynchronous commands, scheduled execution, lifecycle management, Tinyhand serialization, and optional CrystalData persistence.

## Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Core concepts](#core-concepts)
- [Machine controls](#machine-controls)
- [Execution and lifecycle](#execution-and-lifecycle)
- [States and commands](#states-and-commands)
- [Concurrency and snapshots](#concurrency-and-snapshots)
- [Serialization and persistence](#serialization-and-persistence)
- [Dependency injection](#dependency-injection)
- [Exceptions](#exceptions)
- [Generic, external, and excluded machines](#generic-external-and-excluded-machines)
- [Source generator options](#source-generator-options)
- [Building and testing](#building-and-testing)

## Requirements

- .NET 10 or later
- C# 14 or later
- Visual Studio 2026 or another build environment that supports the required .NET SDK and source generators

## Installation

Install the package with the .NET CLI:

```shell
dotnet add package BigMachines
```

The package includes the BigMachines source generator.

## Quick start

Define an empty partial root class, add the machines it owns, and mark each machine as partial.

```csharp
using System;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;

namespace QuickStart;

[BigMachineObject]
[AddMachine<CounterMachine>]
public partial class AppMachines;

[MachineObject]
public partial class CounterMachine : Machine<int>
{
    public CounterMachine()
    {
        this.DefaultInterval = TimeSpan.FromSeconds(1);
        this.Lifespan = TimeSpan.FromSeconds(5);
    }

    public int Count { get; private set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"Machine {this.Identifier}: Initial");
        this.ChangeState(State.Counting);
        return StateResult.Continue;
    }

    [StateMethod]
    protected StateResult Counting(StateParameter parameter)
    {
        Console.WriteLine($"Machine {this.Identifier}: {this.Count++}");
        return StateResult.Continue;
    }

    [CommandMethod]
    protected CommandStatus Print(string message)
    {
        Console.WriteLine(message);
        return CommandStatus.Success;
    }

    protected override void OnTerminate()
    {
        this.BigMachine.ExecutionGroup.RequestTermination();
    }
}

public static class Program
{
    public static async Task Main()
    {
        var root = new ExecutionRoot();
        var machines = new AppMachines(root);
        machines.Start();

        var counter = machines.CounterMachine.GetOrCreate(42);
        await counter.Command.Print("Hello from BigMachines");
        await counter.RunAsync();

        await root.WaitForTerminationAsync();
    }
}
```

The generator adds the root constructor, typed controls, machine handle, state enum, and command proxy.

## Core concepts

- A **big-machine root** derives from `BigMachineBase` through generated code and owns the machine controls declared with `AddMachine<TMachine>` or discovered with `BigMachineObject(IncludeAllMachines = true)`.
- A **machine** derives from `Machine` or `Machine<TIdentifier>` and contains state and command methods.
- A generated **machine handle** (`<MachineName>.Handle`, derived from `Machine.MachineHandle`) is the public object used to inspect, run, pause, resume, or terminate a machine.
- A **machine control** creates, finds, enumerates, and schedules machine instances.
- An `ExecutionRoot` owns the execution lifetime. Construct the generated root with it, call `Start()`, and request termination through the root or the generated root's `ExecutionGroup`.

## Machine controls

`MachineObjectAttribute.Control` selects how instances are managed.

| Control | Purpose |
| --- | --- |
| `Default` | Uses `Single` for `Machine` and `Unordered` for `Machine<TIdentifier>`. |
| `Single` | Manages at most one instance of a machine type. |
| `Unordered` | Manages multiple identified machines without ordering guarantees. |
| `Sequential` | Queues identified machines in creation order. `WorkerCount` sets the number of dedicated workers. |

For an unordered control, common operations include:

```csharp
var machine = machines.CounterMachine.GetOrCreate(42);

if (machines.CounterMachine.TryGet(42, out var existing))
{
    await existing.RunAsync();
}

foreach (var identifier in machines.CounterMachine.GetIdentifiers())
{
    Console.WriteLine(identifier);
}
```

Single and unordered controls provide `TryGet` and `CreateOrReplace`; replacement terminates the existing instance first. Sequential and manual controls provide `TryCreate`, which returns `null` if the machine already exists, and `Find`, which returns the existing handle or `null`. All controls provide `GetOrCreate`.

`MachineObject(CreateOnStart = true)` creates a single-control machine when the root starts. Other controls require explicit creation. `ManualControl.GetOrCreate<TMachine>()` manages one instance per machine type and returns an untyped handle.

All controls provide `GetHandles()`, and identified controls also provide `GetIdentifiers()`. Both return snapshots. Editing the returned array does not change the control; its machine handles still refer to live instances. The root's `GetControls()` likewise returns a separate array of shared controls.

`RunAllAsync()` on an identified control runs a snapshot of its machines, awaiting each in turn. Paused and terminated machines are skipped.

With the default `WorkerCount = 0`, a sequential control runs only its first queued machine through the root's timer; a paused first machine holds up later machines. Lifespan expiration still applies to the whole queue.

Dedicated sequential workers (`WorkerCount > 0`) reserve different eligible machines before dispatch and skip paused machines. With `WorkerCount = 1`, only one worker dispatch runs at a time; with more workers, completion order is not guaranteed. Dedicated workers process available machines immediately, independently of their timer delay. A continuing machine can run repeatedly, so asynchronous work should yield rather than busy-loop. Manual `RunAsync()` calls are outside this worker limit. Restored queues start processing when the root starts.

Machines marked with `MachineObject(ExcludeFromIncludeAllMachines = true)` are not added by `BigMachineObject(IncludeAllMachines = true)`. They can be managed through `ManualControl` or added explicitly when appropriate.

## Execution and lifecycle

A machine can run manually, on a timer, or through a sequential control.

- `DefaultInterval` sets the periodic interval in the constructor. `TimeSpan.Zero` disables interval execution.
- `SetTimeUntilRun` changes the remaining delay.
- `SetNextRunTime` schedules an absolute UTC execution time.
- `Lifespan` terminates a machine after the remaining duration reaches zero.
- `TerminationTime` terminates a machine at an absolute UTC time.

`TimeSpan.MaxValue` disables the relative timer or lifespan. A non-positive delay runs on the next timer pass; a non-positive lifespan terminates on the next pass. A one-shot timer with no periodic interval becomes disabled after firing. Pausing stops state dispatch, but commands and lifespan expiration remain enabled.

The root polls every 500 ms by default. Adjust `((IBigMachine)machines).Core.TimeIntervalInMilliseconds` before starting the root when finer timer resolution is needed. A positive `DefaultInterval` makes a new machine eligible on the first timer pass; it does not delay the first run by a full interval.

Use the generated handle for runtime control:

```csharp
await machine.RunAsync();
machine.Pause();
machine.Resume();
machine.SetNextRunTimeFromNow(TimeSpan.FromMinutes(1));
machine.Terminate();
```

The lifecycle callbacks are invoked in this order for a newly created machine:

```text
OnCreate(createParameter) -> OnStart() -> OnTerminate()
```

`OnCreate` is not called after deserialization. `OnStart` is called after both creation and deserialization. `OnTerminate` runs while the machine semaphore is held.

Creation without a parameter calls `OnCreate(null)`. Termination and disposal run at most once per instance. Exceptions from `OnTerminate` or `Dispose` are queued on the root, and the instance is still removed. An old handle cannot remove a replacement with the same identifier.

Stopping the execution root or group stops background processing; it does not call each machine's `OnTerminate` or `Dispose`. Terminate handles explicitly when that cleanup is required. Long-running handlers can observe `this.BigMachine.CancellationToken` for cooperative shutdown.

`((IBigMachine)machines).HasPendingWork()` checks running machines, scheduled or periodic execution, dedicated-worker queues, and queued exceptions. Pass `excludedMachineType` to ignore a registered machine type, including machines in `ManualControl`; this is useful for a monitor that checks for other work before stopping the application. Exception processing remains pending regardless of that exclusion.

## States and commands

Mark state handlers with `StateMethodAttribute`. A handler takes one `StateParameter` and returns `StateResult` or `Task<StateResult>`. If a machine defines state handlers, state ID `0` is required and is the initial state. When an ID is omitted, the generator derives it from the method name. Use explicit, stable IDs for persisted states that may be renamed.

```csharp
[StateMethod(0)]
protected StateResult Initial(StateParameter parameter)
{
    this.ChangeState(State.Ready, rerun: true);
    return StateResult.Continue;
}
```

A method named `<StateName>CanExit` can reject leaving a state, and `<StateName>CanEnter` can reject entering one. Both are parameterless methods returning `bool`. `ChangeState` returns `ChangeStateResult` so callers can detect a rejected transition. From a state handler, `rerun: true` dispatches the new state after the current handler returns `Continue`, while retaining the semaphore. Changing to the current state does not request another run.

Mark command handlers with `CommandMethodAttribute`. Handlers return `CommandStatus`, `CommandResult<T>`, or a `Task` of either result type. The generator exposes them as asynchronous methods on `machine.Command` and converts thrown exceptions into `CommandStatus.Failure`.

State and command handlers must be non-generic instance methods. Command parameters must be passed by value and cannot be ref-like or pointer types.

```csharp
[CommandMethod]
protected CommandResult<string> Echo(string value)
    => new(value);

var result = await machine.Command.Echo("message");
if (result.Status == CommandStatus.Success)
{
    Console.WriteLine(result.Response);
}
```

Commands acquire the machine semaphore by default. Set `CommandMethod(WithLock = false)` only when the handler is safe to run concurrently.

For unordered and sequential machines, `CommandMethod(GenerateAllCommand = true)` generates an `All<CommandName>` extension on the typed control, such as `await machines.WorkerMachine.AllEcho("message")`. It takes a snapshot of that control's handles, awaits each command in turn, and returns an array containing each identifier and result. The extensions are in the `BigMachines` namespace unless `CustomNamespace` is configured.

## Concurrency and snapshots

Control creation, lookup, removal, and enumeration synchronize access to membership. State handlers and commands with the default `WithLock = true` share each machine's semaphore. Public scheduling setters also acquire that semaphore. Within a handler, use the protected machine properties directly; calling a locking handle method on the same machine can deadlock. Avoid cyclic calls between machines holding their semaphores.

`BigMachineObject(EnableRecursiveDetection = true)` is currently reserved and does not enable recursive-call checks.

A root snapshot is not a transaction across machines. Collection locks protect membership, but do not automatically protect arbitrary fields modified by a handler. For per-machine consistency, opt in to Tinyhand's serialization lock:

```csharp
[TinyhandObject(LockMemberName = nameof(Semaphore))]
[MachineObject]
public partial class PersistentMachine : Machine<int>
{
    // Define keyed data and state handlers here.
}
```

Coordinate root-wide save and restore with application-level synchronization when several machines must represent one consistent state. Block new writes and wait for in-flight operations before taking such a snapshot. Do not deserialize or replay journals into controls while their workers, commands, or other writers are active. Prefer loading a new root, then call `Start()` after loading and replay finish. For identified controls, serialization locks the collection before a machine's optional serialization lock; handlers must not acquire that collection while holding the machine lock, or the reversed lock order can deadlock.

## Serialization and persistence

BigMachines integrates with [Tinyhand](https://github.com/archi-Doc/Tinyhand) and [ValueLink](https://github.com/archi-Doc/ValueLink). Apply `TinyhandObjectAttribute` to each concrete machine whose state must be serialized. Do not apply it only to the abstract `Machine` base classes.

```csharp
using Tinyhand;
using Arc.Threading;
using BigMachines;
using Microsoft.Extensions.DependencyInjection;

[BigMachineObject]
[AddMachine<PersistentMachine>]
public partial class PersistentMachines;

[TinyhandObject]
[MachineObject]
public partial class PersistentMachine : Machine<int>
{
    [Key(10)]
    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
        => StateResult.Continue;
}
```

Create and restore the root inside your application method:

```csharp
// Register the destination execution root before deserializing a generated root.
var source = new PersistentMachines(new ExecutionRoot());
source.PersistentMachine.GetOrCreate(42);
var data = TinyhandSerializer.Serialize(source);
var destinationRoot = new ExecutionRoot();
TinyhandSerializer.ServiceProvider = new ServiceCollection()
    .AddSingleton(destinationRoot)
    .BuildServiceProvider();
var restored = TinyhandSerializer.Deserialize<PersistentMachines>(data)!;
restored.Start();
```

The base machine uses reserved Tinyhand keys for runtime state. Use key `10` or greater for machine data, as shown in the repository examples. A machine without `TinyhandObjectAttribute` remains runtime-only. `AddMachine(NonPersistent = true)` excludes that control from root persistence.

`ManualControl` is runtime-only. Non-persistent controls are excluded from root snapshots and journal routing, including when loading older snapshots that contain their keys. Deserialization preserves control objects, replaces the contents of present entries, and leaves omitted entries unchanged. A `nil` control entry clears its contents. Invalid machine identifiers are rejected before replacing the affected collection; loading a whole root is not transactional if a later entry fails.

For incremental journals, use `TinyhandObject(Structural = true)` on each persistent machine and attach the generated root to a Tinyhand structural root (normally through CrystalData). BigMachines reconnects controls and machines after loading, and records collection additions, removals, and runtime-property changes. Ordinary assignments to user data require Tinyhand's journal-aware setters or explicit journaling; `[Key]` alone only includes a field in snapshots. Replay restores data, not an in-flight handler or an external side effect.

BigMachines emits closed formatter registrations for Tinyhand and NativeAOT. Closed generic machines should be listed explicitly with `AddMachine<GenericMachine<ConcreteType>>` so the generator can register their concrete types.

Enable NativeAOT in the application project, not in the analyzer project:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>
```

The repository's `NativeAotTest` project publishes with trimming warnings treated as errors and executes Tinyhand round trips for single, unordered, and sequential controls.

For file persistence, register a root containing serializable machines with [CrystalData](https://github.com/archi-Doc/CrystalData):

```shell
dotnet add package CrystalData
```

```csharp
using CrystalData;
using Microsoft.Extensions.DependencyInjection;
using Tinyhand;

var builder = new CrystalUnit.Builder()
    .ConfigureCrystal(context =>
    {
        context.SetJournal(
            new SimpleJournalConfiguration(
                new LocalDirectoryConfiguration("Data/Journal")));

        context.AddCrystal<PersistentMachines>(new()
        {
            FileConfiguration = new LocalFileConfiguration("Data/PersistentMachines.tinyhand"),
            SaveFormat = SaveFormat.Utf8,
            NumberOfHistoryFiles = 3,
        });
    });

var unit = builder.Build();
TinyhandSerializer.ServiceProvider = unit.Context.ServiceProvider;
var crystalControl = unit.Context.ServiceProvider.GetRequiredService<CrystalControl>();
await crystalControl.PrepareAndLoad(false);
var machines = unit.Context.ServiceProvider.GetRequiredService<PersistentMachines>();
machines.Start();

// Run application work here, then stop background processing and save.
machines.ExecutionGroup.RequestTermination();
await machines.ExecutionGroup.WaitForTerminationAsync();
await crystalControl.StoreAndRip();
```

The `Advanced` sample demonstrates the complete shutdown flow. It currently references CrystalData 0.50.0; the library references Tinyhand 0.148.1. Use compatible versions together.

## Dependency injection

Set `MachineObject(UseServiceProvider = true)` when a machine requires constructor injection. Register the machine and its dependencies, then assign the built provider to `TinyhandSerializer.ServiceProvider` before machines are created or deserialized.

Install `Microsoft.Extensions.DependencyInjection` when using `ServiceCollection` and `BuildServiceProvider`. Register `ExecutionRoot` as well when deserializing generated roots. Machine services must return a fresh instance for each creation; attaching one instance to multiple controls, or reusing a terminated instance, is rejected.

```csharp
var services = new ServiceCollection()
    .AddSingleton<Clock>()
    .AddSingleton(new ExecutionRoot())
    .AddTransient<ServiceMachine>()
    .BuildServiceProvider();

TinyhandSerializer.ServiceProvider = services;
```

```csharp
[MachineObject(UseServiceProvider = true)]
public partial class ServiceMachine : Machine<int>
{
    public ServiceMachine(Clock clock)
    {
        this.Clock = clock;
    }

    private Clock Clock { get; }
}
```

Pass per-instance data through `GetOrCreate(identifier, createParameter)` and receive it in `OnCreate`. Constructor dependencies and creation parameters serve different purposes.

## Exceptions

Exceptions thrown by generated state or command dispatch are wrapped in `MachineExceptionInfo` and queued on the root. The default handler writes them to the console. Install a custom handler through `IBigMachine` when the application needs logging or another policy:

```csharp
((IBigMachine)machines).SetExceptionHandler(exception =>
{
    Console.Error.WriteLine(exception);
});
```

Command callers receive `CommandStatus.Failure` when a command handler throws. A command sent to a terminated machine returns `CommandStatus.Terminated`.

A state-handler exception terminates that machine. Exceptions from `OnCreate` or `OnStart` propagate to the creation or deserialization caller. Keep custom exception handlers from throwing, since they run on the background processing task.

## Generic, external, and excluded machines

Constructed generic machines and machines from referenced assemblies can be added explicitly:

```csharp
[BigMachineObject]
[AddMachine<GenericMachine<string>>]
[AddMachine<ExternalLibrary.WorkerMachine>]
public partial class AppMachines;
```

`BigMachineObject(IncludeAllMachines = true)` includes machines discovered in the current assembly, except those marked with `MachineObject(ExcludeFromIncludeAllMachines = true)`. Explicit `AddMachine<TMachine>` declarations remain the clearest choice for constructed generic and external types.

Use `AddMachine<TMachine>(Name = "Workers")` to choose the generated control property name, including when multiple machine types share the same simple name.

## Source generator options

Apply `BigMachinesGeneratorOptionAttribute` to a separate class in the project to configure generated output:

```csharp
[BigMachinesGeneratorOption(CustomNamespace = "MyApp.Generated")]
internal class GeneratorOptions;
```

`CustomNamespace` changes the namespace of the registration module and command extensions; machine and root partial classes remain in their declared namespaces. `GenerateToFile = true` writes generated sources into an existing `Generated` folder beside the options file instead of adding them directly to the compilation. Include those files in compilation when using that mode.

`UseModuleInitializer` defaults to `true` and registers machine types and formatters automatically. If set to `false`, call the generated module's `Initialize()` method before using machines or serialization. With the custom namespace above, that call is `MyApp.Generated.BigMachinesModule.Initialize()`.

To inspect generated sources while keeping normal compiler integration, use the SDK's output switch:

```shell
dotnet build BigMachines.slnx --property:EmitCompilerGeneratedFiles=true --property:CompilerGeneratedFilesOutputPath=obj/Generated
```

## Building and testing

The repository selects Microsoft.Testing.Platform in `global.json`. Use the .NET 10 solution syntax:

```shell
dotnet build BigMachines.slnx --configuration Release
dotnet test --solution BigMachines.slnx --configuration Release --no-build
dotnet test --solution BigMachines.slnx --configuration Release --coverage --coverage-output-format cobertura
dotnet publish NativeAotTest/NativeAotTest.csproj --configuration Release --runtime win-x64
```

Run the published NativeAOT executable to verify serialization and registration at runtime. CI performs the corresponding `linux-x64` publish and execution. Regression tests cover snapshot and journal restoration, non-persistent controls, lifecycle cleanup, scheduling boundaries, concurrent creation, dedicated-worker limits, and generated helper name collisions.

`QuickStart` contains a minimal runnable example. `Advanced` covers persistence, dependency injection, generic and external machines, and scheduling. `BigMachines.Tests` exercises runtime behavior; `BigMachinesGenerator.Tests` checks the source generator directly. `Benchmark` contains BenchmarkDotNet experiments with allocation reporting; run selected benchmarks in Release mode before drawing performance conclusions.
