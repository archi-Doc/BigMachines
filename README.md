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
- [Serialization and persistence](#serialization-and-persistence)
- [Dependency injection](#dependency-injection)
- [Exceptions](#exceptions)
- [Generic, external, and private machines](#generic-external-and-private-machines)

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

Machines marked with `MachineObject(Private = true)` are not added to a root automatically. They can be managed through `ManualControl` or added explicitly when appropriate.

## Execution and lifecycle

A machine can run manually, on a timer, or through a sequential control.

- `DefaultTimeout` sets the periodic interval. `TimeSpan.Zero` disables interval execution.
- `SetTimeUntilRun` changes the remaining delay.
- `SetNextRunTime` schedules an absolute UTC execution time.
- `Lifespan` terminates a machine after the remaining duration reaches zero.
- `TerminationTime` terminates a machine at an absolute time.

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

## Serialization and persistence

BigMachines integrates with [Tinyhand](https://github.com/archi-Doc/Tinyhand) and [ValueLink](https://github.com/archi-Doc/ValueLink). Apply `TinyhandObjectAttribute` to each concrete machine whose state must be serialized. Do not apply it only to the abstract `Machine` base classes.

```csharp
using Tinyhand;

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

var data = TinyhandSerializer.Serialize(machines);
var restored = TinyhandSerializer.Deserialize<AppMachines>(data);
```

The base machine uses reserved Tinyhand keys for runtime state. Use key `10` or greater for machine data, as shown in the repository examples. A machine without `TinyhandObjectAttribute` remains runtime-only. `AddMachine(Volatile = true)` excludes that control from root persistence.

BigMachines emits the closed formatter registrations required by Tinyhand 0.144 and NativeAOT. Closed generic machines should be listed explicitly with `AddMachine<GenericMachine<ConcreteType>>` so the generator can register their concrete types.

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

```csharp
var services = new ServiceCollection()
    .AddSingleton<Clock>()
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
