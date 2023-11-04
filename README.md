## BigMachines is State Machine library for .NET
![Nuget](https://img.shields.io/nuget/v/BigMachines) ![Build and Test](https://github.com/archi-Doc/BigMachines/workflows/Build%20and%20Test/badge.svg)

- Very versatile and easy to use.

- Running machines and sending commands to each machine is designed to be **lock-free**.

- Full serialization features integrated with [Tinyhand](https://github.com/archi-Doc/Tinyhand).

- Simplifies complex and long-running tasks.

  


## Table of Contents

- [Requirements](#requirements)
- [Quick Start](#quick-start)
- [Machine Types](#machine-types)
- [Identifier](#identifier)
- [Machine Class](#machine-class)
- [Other](#other)
  - [Service provider](#service-provider)
  - [Generic machine](#generic-machine)
  - [Loop checker](#loop-checker)
  - [Exception handling](#exception-handling)



## Requirements

**Visual Studio 2022** or later for Source Generator V2.

**C# 9.0** or later for generated codes.

**.NET 6** or later target framework.



## Quick Start

Install **BigMachines** using Package Manager Console.

```
Install-Package BigMachines
```

This is a small sample code to use **BigMachines**.

```csharp
using System;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;

namespace QuickStart;

// Create a BigMachine class that acts as the root for managing machines.
// In particular, define an empty partial class, add a BigMachineObject attribute, and then add AddMachine attributes for the Machine you want to include.
[BigMachineObject(Inclusive = true)]
public partial class BigMachine { }

[MachineObject] // Add a MachineObject attribute.
public partial class FirstMachine : Machine<int> // Inherit Machine class. The type of an identifier is int.
{
    public FirstMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1); // The default time interval for machine execution.
        this.Lifespan = TimeSpan.FromSeconds(5); // The time until the machine automatically terminates.
    }

    public int Count { get; set; }

    [StateMethod(0)] // Add a StateMethod attribute and set the state method id 0 (default state).
    protected StateResult Initial(StateParameter parameter)
    {// This code is inside the machine's exclusive lock.
        Console.WriteLine($"FirstMachine {this.Identifier}: Initial");
        this.ChangeState(FirstMachine.State.One); // Change to state One.
        return StateResult.Continue; // Continue (StateResult.Terminate to terminate machine).
    }

    [StateMethod] // If a state method id is not specified, the hash of the method name is used.
    protected StateResult One(StateParameter parameter)
    {
        Console.WriteLine($"FirstMachine {this.Identifier}: One - {this.Count++}");
        return StateResult.Continue;
    }

    [CommandMethod] // Add a CommandMethod attribute to a method which receives and processes commands.
    protected CommandResult TestCommand(string message)
    {
        Console.WriteLine($"Command received: {message}");
        return CommandResult.Success;
    }

    protected override void OnTermination()
    {
        Console.WriteLine($"FirstMachine {this.Identifier}: Terminated");
        ThreadCore.Root.Terminate(); // Send a termination signal to the root.
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var bigMachine = new BigMachine(); // Create a BigMachine instance.
        bigMachine.Start(ThreadCore.Root); // Launch BigMachine to run machines and change the parent of the BigMachine thread to the application thread.

        var testMachine = bigMachine.FirstMachine.GetOrCreate(42); // Machine is created via an interface class and the identifier, not the machine class itself.
        testMachine.TryGetState(out var state); // Get the current state. You can operate machines using the interface class.
        Console.WriteLine($"FirstMachine state: {state}");

        testMachine = bigMachine.FirstMachine.GetOrCreate(42); // Get the created machine.
        testMachine.RunAsync().Wait(); // Run the machine manually.
        Console.WriteLine();

        var testControl = bigMachine.FirstMachine; // Control is a collection of machines.

        Console.WriteLine("Enumerates identifiers.");
        foreach (var x in testControl.GetIdentifiers())
        {
            Console.WriteLine($"Machine Id: {x}");
        }

        Console.WriteLine();

        await testMachine.Command.TestCommand("Test message"); // Send a command to the machine.
        Console.WriteLine();

        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
    }
}
```



## Machine Types

**BigMachines** supports several types of machine.

### Passive Machine

Passive machine can be run and the state can be changed by an external operation.

```csharp
[MachineObject(0xffd829b4)]
public partial class PassiveMachine : Machine<int>
{
    public static async Task Test(BigMachine<int> bigMachine)
    {
        var m = bigMachine.CreateOrGet<PassiveMachine.Interface>(0);

        await m.CommandAsync(Command.ReceiveString, "message 1"); // Send a command.

        await m.RunAsync(); // Manually run the machine.

        await m.ChangeStateAsync(State.First); // Change the state from State.Initial to State.First
        await m.RunAsync(); // Manually run the machine.

        await m.ChangeStateAsync(State.Second); // Change the state from State.First to State.Second (denied)
        await m.RunAsync(); // Manually run the machine.

        await m.ChangeStateAsync(State.Second); // Change the state from State.First to State.Second (approved)
        await m.RunAsync(); // Manually run the machine.
    }

    public PassiveMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"PassiveMachine: Initial - {this.Count++}");
        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected StateResult First(StateParameter parameter)
    {
        Console.WriteLine($"PassiveMachine: First - {this.Count++}");
        return StateResult.Continue;
    }

    protected bool FirstCanExit()
    {// State Name + "CanExit": Determines if it is possible to change from the state.
        return true;
    }

    [StateMethod(2)]
    protected StateResult Second(StateParameter parameter)
    {
        Console.WriteLine($"PassiveMachine: Second - {this.Count++}");
        return StateResult.Continue;
    }

    protected bool SecondCanEnter()
    {// State Name + "CanEnter": Determines if it is possible to change to the state.
        var result = this.Count > 2;
        var message = result ? "Approved" : "Denied";
        Console.WriteLine($"PassiveMachine: {this.GetCurrentState().ToString()} -> {State.Second.ToString()}: {message}");
        return result;
    }

    [CommandMethod(0)]
    protected void ReceiveString(CommandPost<int>.Command command)
    {
        if (command.Message is string message)
        {
            Console.WriteLine($"PassiveMachine command: {message}");
        }
    }
}
```



### Intermittent Machine

Intermittent machine is a machine that runs at regular intervals.

```csharp
[MachineObject(0x1b431670)]
public partial class IntermittentMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        var m = bigMachine.CreateOrGet<IntermittentMachine.Interface>(0);

        // The machine will run at regular intervals (1 second).
    }

    public IntermittentMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
        this.SetLifespan(TimeSpan.FromSeconds(5)); // The time until the machine automatically terminates.
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"IntermittentMachine: Initial - {this.Count++}");
        if (this.Count > 2)
        {
            this.ChangeState(State.First);
        }

        return StateResult.Continue;
    }

    [StateMethod(1)]
    protected StateResult First(StateParameter parameter)
    {
        Console.WriteLine($"IntermittentMachine: First - {this.Count++}");
        this.SetTimeout(TimeSpan.FromSeconds(0.5)); // Change the timeout of the machine.
        return StateResult.Continue;
    }
}
```



### Continuous Machine

Continuous machine is different from passive and intermittent machine (passive and intermittent machine are virtually the same).

It's designed for heavy and time-consuming tasks.

Once a continuous machine is created, `BigMachine` will assign one thread for the machine and run the machine repeatedly until the machine returns `StateResult.Terminate`.

```csharp
[MachineObject(0xb579a7d8, Continuous = true)] // Set the Continuous property to true.
public partial class ContinuousMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        bigMachine.Continuous.SetMaxThreads(2); // Set the maximum number of threads used for continuous machines.
        var m = bigMachine.CreateOrGet<ContinuousMachine.Interface>(0);

        // The machine will run until the task is complete.
    }

    public ContinuousMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        Console.WriteLine($"ContinuousMachine: Initial - {this.Count++}");
        if (this.Count > 10)
        {
            Console.WriteLine($"ContinuousMachine: Done");
            return StateResult.Terminate;
        }

        await Task.Delay(100); // Some heavy task
        // await Task.Delay(100).WithoutLock(this); // You can also release the lock temporarily to improve the response (the machine state may change in the meantime).

        return StateResult.Continue;
    }
}
```

To improve response and share resource, heavy task should not be done at once, but divided into several smaller tasks.



## Identifier

`Identifier` is a key concept of `BigMachines`.

Each machine in `MachineGroup<TIdentifier>` has a unique identifier, and machines are identified and operated by identifiers.

`Identifier` has several constraints.

- Serializable with `Tinyhand ` (has `TinyhandObject` attribute).
- Must be immutable.
- Has proper `Equals()` implementation.
- Has proper `GetHashCode()` implementation.



This is a sample implementation of `Identifier`.

```csharp
[TinyhandObject]
public partial class IdentifierClass : IEquatable<IdentifierClass>
{
    public static IdentifierClass Default { get; } = new();

    public IdentifierClass()
    {
        this.Name = string.Empty;
    }

    public IdentifierClass(int id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    [Key(0)]
    public int Id { get; private set; }

    [Key(1)]
    public string Name { get; private set; }

    public bool Equals(IdentifierClass? other)
    {
        if (other == null)
        {
            return false;
        }

        return this.Id == other.Id && this.Name == other.Name;
    }

    public override int GetHashCode() => HashCode.Combine(this.Id, this.Name);

    public override string ToString() => $"Id: {this.Id} Name: {this.Name}";
}

// Alternative
[TinyhandObject(ImplicitKeyAsName = true)]
public partial record IdentifierClass2(int id, string name);
```



## Machine Class

### Reserved keywords

These keywords in `Machine` class are reserved for source generator.

- `Interface`: Nested class for operating a machine.
- `State`: enum type which represents the state of a machine.
- `Command`: enum type which represents the command of a machine.
- `CreateInterface()`: Creates an instance of machine interface.
- `ChangeState()`: Changes the state of a machine.
- `GetState()`: Gets the current state of a machine.
- `InternalChangeState()`: Internally used to change the state.
- `InternalRun()`:  Internally used to run machine and process each state.
- `InternalCommand()`:  Internally used to process commands.
- `RegisterBM()`: Registers the machine to `BigMachine`.

`Internal` methods are used within the library and should not be used by the user.



## Other

### Service provider

Since the machine is independent, you cannot pass parameters directly when creating an instance (and mainly for the deserialization process).

Consider using a DI container (service provider) or `Machine<TIdentifier>.SetParameter(object? createParam)` method.

```csharp
var container = new Container(); // DryIoc
container.RegisterDelegate<BigMachine<int>>(x => new BigMachine<int>(container), Reuse.Singleton);
container.Register<SomeService>(); // Register some service.
container.Register<ServiceProviderMachine>(Reuse.Transient); // Register machine.
```

```csharp
public class SomeService
{
    public void Print(string? text) => Console.WriteLine($"Some service : {text}");
}

// Machine depends on SomeService.
[MachineObject(0x4f8f7256)]
public partial class ServiceProviderMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        bigMachine.CreateOrGet<ServiceProviderMachine.Interface>(0, "A"); // Create a machine and set a parameter.
    }

    public ServiceProviderMachine(BigMachine<int> bigMachine, SomeService service)
        : base(bigMachine)
    {
        this.Service = service;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
        this.SetLifespan(TimeSpan.FromSeconds(3));
    }

    protected override void SetParameter(object? createParam)
    {// Receives a parameter. Note that this method is NOT called during deserialization.
        this.Text = (string?)createParam;
    }

    public SomeService Service { get; }

    public string? Text { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        this.Service.Print(this.Text);
        return StateResult.Continue;
    }
}
```



### Generic machine

`BigMachine` and `Machine` are strongly related with `Identifier`.

Normally, the type of the identifier is fixed.

However you can create generic-identifier machine and machines can be used with multiple types of `BigMachine`.

```csharp
[MachineObject(0x928b319e)]
public partial class GenericMachine<TIdentifier> : Machine<TIdentifier>
where TIdentifier : notnull
{
    public static void Test(BigMachine<TIdentifier> bigMachine)
    {
        bigMachine.CreateOrGet<GenericMachine<TIdentifier>.Interface>(default!);
    }

    public GenericMachine(BigMachine<TIdentifier> bigMachine)
        : base(bigMachine)
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
        this.SetLifespan(TimeSpan.FromSeconds(5));
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"Generic ({this.Identifier.ToString()}) - {this.Count++}");
        return StateResult.Continue;
    }
}
```



### Loop checker

Relationships between machines can become complicated, and may lead to circular command issuing.

```csharp
[MachineObject(0xb7196ebc)]
public partial class LoopMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        bigMachine.LoopCheckerMode = LoopCheckerMode.EnabledAndThrowException;
        var loopMachine = bigMachine.CreateOrGet<LoopMachine.Interface>(0);
        var loopMachine2 = bigMachine.CreateOrGet<LoopMachine.Interface>(2);

        // Case 1: LoopMachine -> LoopMachine
        loopMachine.CommandAsync(Command.RelayInt, 1);

        // Case 2: LoopMachine -> TestMachine -> LoopMachine
        // bigMachine.CreateOrGet<TestMachine.Interface>(3);
        // loopMachine.CommandAsync(Command.RelayString, "loop");

        // Case 3: LoopMachine -> LoopMachine2
        // loopMachine.CommandAsync(Command.RelayInt2, 2);
    }

    public LoopMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
    }

    [CommandMethod(0)]
    protected void RelayInt(CommandPost<int>.Command command)
    {
        if (command.Message is int n)
        {// LoopMachine
            Console.WriteLine($"RelayInt: {n}");
            this.BigMachine.TryGet<Interface>(this.Identifier)?.CommandAsync(Command.RelayInt, n);
        }
    }

    [CommandMethod(1)]
    protected void RelayString(CommandPost<int>.Command command)
    {
        if (command.Message is string st)
        {// LoopMachine -> TestMachine
            Console.WriteLine($"RelayString: {st}");
            this.BigMachine.TryGet<TestMachine.Interface>(3)?.CommandAsync(TestMachine.Command.RelayString, st);
        }
    }

    [CommandMethod(2)]
    protected void RelayInt2(CommandPost<int>.Command command)
    {
        if (command.Message is int n)
        {// LoopMachine -> LoopMachine n
            if (this.Identifier == 0)
            {
                Console.WriteLine($"RelayInt2: {n}");
                this.BigMachine.TryGet<Interface>(n)?.CommandAsync(Command.RelayInt2, n);
            }
            else
            {
                n = 0;
                Console.WriteLine($"RelayInt2: {n}");
                this.BigMachine.TryGet<Interface>(n)?.CommandAsync(Command.RelayInt2, n);
            }
        }
    }
}
```

This code will cause `InvalidOperationException`.

You can disable the loop checker if you want (not recommended).

```csharp
bigMachine.LoopCheckerMode = LoopCheckerMode.Disabled;
```



### Exception handling

Each machine is designed to run independently.

So exceptions thrown in machines are handled by BigMachine's main thread (`BigMachine.Core`), not by the caller.

In detail, exceptions are registered to BigMachine using `BigMachine.ReportException()`, and handled by the following method in BigMachine's main thread.

```cahrp
private static void DefaultExceptionHandler(BigMachineException exception)
{
    throw exception.Exception;
}
```

You can set a custom exception handler using `BigMachine.SetExceptionHandler()`.

