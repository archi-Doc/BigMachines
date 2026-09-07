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
- [Generic, external, and private machines](#generic-external-and-private-machines)
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
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
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
    protected CommandResult Print(string message)
    {
        Console.WriteLine(message);
        return CommandResult.Success;
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

        await root.WaitForTermination();
    }
}
```

The generator adds the root constructor, typed controls, machine interface, state enum, and command proxy.

## Core concepts

- A **big-machine root** derives from `BigMachineBase` through generated code and owns the machine controls declared with `AddMachine<TMachine>` or discovered with `BigMachineObject(Inclusive = true)`.
- A **machine** derives from `Machine` or `Machine<TIdentifier>` and contains state and command methods.
- A generated **machine interface** is the public handle used to inspect, run, pause, unpause, or terminate a machine.
- A **machine control** creates, finds, enumerates, and schedules machine instances.
- An `ExecutionRoot` owns the execution lifetime. Construct the generated root with it, call `Start()`, and request termination through the root or the generated root's `ExecutionGroup`.

## Machine controls

`MachineObjectAttribute.Control` selects how instances are managed.

| Control | Purpose |
| --- | --- |
| `Default` | Uses `Single` for `Machine` and `Unordered` for `Machine<TIdentifier>`. |
| `Single` | Manages at most one instance of a machine type. |
| `Unordered` | Manages multiple identified machines without ordering guarantees. |
| `Sequential` | Queues identified machines in creation order. `NumberOfTasks` sets the number of dedicated workers. |

Common control operations include:

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

`CreateAlways` terminates an existing matching instance before creating its replacement. `TryCreate` is available on sequential and manual controls when creation must fail instead of returning an existing machine.

`GetArray()` and `GetIdentifiers()` return snapshots. Editing the returned array does not change the control; its machine handles still refer to live instances. The root's `GetArray()` likewise returns a separate array of shared controls.

Dedicated sequential workers reserve different eligible machines before dispatch. With `NumberOfTasks = 1`, only one worker dispatch runs at a time; with more workers, completion order is not guaranteed. Dedicated workers process available machines immediately, independently of their timer delay. Manual `RunAsync()` calls are outside this worker limit. Restored queues start processing when the root starts.

Machines marked with `MachineObject(Private = true)` are not added to a root automatically. They can be managed through `ManualControl` or added explicitly when appropriate.

## Execution and lifecycle

A machine can run manually, on a timer, or through a sequential control.

- `DefaultTimeout` sets the periodic interval. `TimeSpan.Zero` disables interval execution.
- `SetTimeUntilRun` changes the remaining delay.
- `SetNextRunTime` schedules an absolute UTC execution time.
- `Lifespan` terminates a machine after the remaining duration reaches zero.
- `TerminationTime` terminates a machine at an absolute time.

`TimeSpan.MaxValue` disables the relative timer or lifespan. A non-positive delay runs on the next timer pass; a non-positive lifespan terminates on the next pass. A one-shot timer with no periodic interval becomes disabled after firing. Pausing stops state dispatch, but commands and lifespan expiration remain enabled.

Use the generated interface for runtime control:

```csharp
await machine.RunAsync();
machine.PauseMachine();
machine.UnpauseMachine();
machine.SetNextRunTimeFromNow(TimeSpan.FromMinutes(1));
machine.TerminateMachine();
```

The lifecycle callbacks are invoked in this order for a newly created machine:

```text
OnCreate(createParam) -> OnStart() -> OnTerminate()
```

`OnCreate` is not called after deserialization. `OnStart` is called after both creation and deserialization. `OnTerminate` runs while the machine semaphore is held.

Creation without a parameter calls `OnCreate(null)`. Termination and disposal run at most once per instance. Exceptions from `OnTerminate` or `Dispose` are queued on the root, and the instance is still removed. An old handle cannot remove a replacement with the same identifier.

## States and commands

Mark state handlers with `StateMethodAttribute`. If a machine defines state handlers, state ID `0` is required and is the initial state. When an ID is omitted, the generator derives it from the method name.

```csharp
[StateMethod(0)]
protected StateResult Initial(StateParameter parameter)
{
    this.ChangeState(State.Ready, rerun: true);
    return StateResult.Continue;
}
```

A method named `<StateName>CanExit` can reject leaving a state, and `<StateName>CanEnter` can reject entering one. Both methods return `bool`.

Mark command handlers with `CommandMethodAttribute`. The generator exposes them as asynchronous methods on `machine.Command` and converts thrown exceptions into `CommandResult.Failure`.

```csharp
[CommandMethod]
protected CommandResult<string> Echo(string value)
    => new(value);

var result = await machine.Command.Echo("message");
if (result.Result == CommandResult.Success)
{
    Console.WriteLine(result.Response);
}
```

Commands acquire the machine semaphore by default. Set `CommandMethod(WithLock = false)` only when the handler is safe to run concurrently. `All = true` generates an extension that sends the command to every instance managed by the root.

## Concurrency and snapshots

Control creation, lookup, removal, and enumeration synchronize access to membership. State handlers and commands with the default `WithLock = true` share each machine's semaphore. Public scheduling setters also acquire that semaphore. Within a handler, use the protected machine properties directly; calling a locking interface method on the same machine can deadlock. Avoid cyclic calls between machines holding their semaphores.

A root snapshot is not a transaction across machines. Collection locks protect membership, but do not automatically protect arbitrary fields modified by a handler. For per-machine consistency, opt in to Tinyhand's serialization lock:

```csharp
[TinyhandObject(LockObject = nameof(Semaphore))]
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

The base machine uses reserved Tinyhand keys for runtime state. Use key `10` or greater for machine data, as shown in the repository examples. A machine without `TinyhandObjectAttribute` remains runtime-only. `AddMachine(Volatile = true)` excludes that control from root persistence.

`ManualControl` is runtime-only. Volatile controls are excluded from root snapshots and journal routing, including when loading older snapshots that contain their keys. Deserialization preserves control objects, replaces the contents of present entries, and leaves omitted entries unchanged. A `nil` control entry clears its contents. Invalid machine identifiers are rejected before replacing the affected collection; loading a whole root is not transactional if a later entry fails.

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

For file persistence, register the generated root with [CrystalData](https://github.com/archi-Doc/CrystalData):

```shell
dotnet add package CrystalData
```

```csharp
var builder = new CrystalUnit.Builder()
    .ConfigureCrystal(context =>
    {
        context.SetJournal(
            new SimpleJournalConfiguration(
                new LocalDirectoryConfiguration("Data/Journal")));

        context.AddCrystal<AppMachines>(new()
        {
            FileConfiguration = new LocalFileConfiguration("Data/AppMachines.tinyhand"),
            SaveFormat = SaveFormat.Utf8,
            NumberOfFileHistories = 3,
        });
    });
```

Use CrystalData 0.47.0 or later with Tinyhand 0.144 to avoid references to the removed dynamic formatter-registration API.

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

Pass per-instance data through `GetOrCreate(identifier, createParam)` and receive it in `OnCreate`. Constructor dependencies and creation parameters serve different purposes.

## Exceptions

Exceptions thrown by generated state or command dispatch are wrapped in `BigMachineException` and queued on the root. The default handler writes them to the console. Install a custom handler through `IBigMachine` when the application needs logging or another policy:

```csharp
((IBigMachine)machines).SetExceptionHandler(exception =>
{
    Console.Error.WriteLine(exception);
});
```

Command callers receive `CommandResult.Failure` when a command handler throws. A command sent to a terminated machine returns `CommandResult.Terminated`.

## Generic, external, and private machines

Constructed generic machines and machines from referenced assemblies can be added explicitly:

```csharp
[BigMachineObject]
[AddMachine<GenericMachine<string>>]
[AddMachine<ExternalLibrary.WorkerMachine>]
public partial class AppMachines;
```

`BigMachineObject(Inclusive = true)` includes eligible non-private machines discovered in the current assembly. Explicit `AddMachine<TMachine>` declarations remain the clearest choice for constructed generic and external types.

## Building and testing

The repository selects Microsoft.Testing.Platform in `global.json`. Use the .NET 10 solution syntax:

```shell
dotnet build BigMachines.slnx --configuration Release
dotnet test --solution BigMachines.slnx --configuration Release --no-build
dotnet test --solution BigMachines.slnx --configuration Release --coverage --coverage-output-format cobertura
dotnet publish NativeAotTest/NativeAotTest.csproj --configuration Release --runtime win-x64
```

Run the published NativeAOT executable to verify serialization and registration at runtime. CI performs the corresponding `linux-x64` publish and execution. Regression tests cover snapshot and journal restoration, volatile controls, lifecycle cleanup, scheduling boundaries, concurrent creation, dedicated-worker limits, and generated helper name collisions.
