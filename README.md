## BigMachines is State Machine library for .NET
![Nuget](https://img.shields.io/nuget/v/BigMachines) ![Build and Test](https://github.com/archi-Doc/BigMachines/workflows/Build%20and%20Test/badge.svg)

- Very versatile and easy to use.

- Running machines and sending commands to each machine is designed to be **lock-free**.

- Full serialization features integrated with [Tinyhand](https://github.com/archi-Doc/Tinyhand), [ValueLink](https://github.com/archi-Doc/ValueLink), [CrystalData](https://github.com/archi-Doc/CrystalData).

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

**C# 12** or later for generated codes.

**.NET 8** or later target framework.



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
[BigMachineObject]
[AddMachine<FirstMachine>]
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
[MachineObject]
public partial class PassiveMachine : Machine<int>
{
    public static async Task Test(BigMachine bigMachine)
    {
        var machine = bigMachine.PassiveMachine.GetOrCreate(0);

        await machine.Command.ReceiveString("message 1"); // Send a command.

        await machine.RunAsync(); // Manually run the machine.

        var result = machine.ChangeState(State.First); // Change the state from State.Initial to State.First
        Console.WriteLine(result.ToString());
        await machine.RunAsync(); // Manually run the machine.

        result = machine.ChangeState(State.Second); // Change the state from State.First to State.Second (denied)
        Console.WriteLine(result.ToString());
        await machine.RunAsync(); // Manually run the machine.

        result = machine.ChangeState(State.Second); // Change the state from State.First to State.Second (approved)
        Console.WriteLine(result.ToString());
        await machine.RunAsync(); // Manually run the machine.
    }

    public PassiveMachine()
    {
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"PassiveMachine: Initial - {this.Count++}");
        return StateResult.Continue;
    }

    [StateMethod]
    protected StateResult First(StateParameter parameter)
    {
        Console.WriteLine($"PassiveMachine: First - {this.Count++}");
        return StateResult.Continue;
    }

    protected bool FirstCanExit()
    {// State Name + "CanExit": Determines if it is possible to change from the state.
        return true;
    }

    [StateMethod]
    protected StateResult Second(StateParameter parameter)
    {
        Console.WriteLine($"PassiveMachine: Second - {this.Count++}");
        return StateResult.Continue;
    }

    protected bool SecondCanEnter()
    {// State Name + "CanEnter": Determines if it is possible to change to the state.
        var result = this.Count > 2;
        var message = result ? "Approved" : "Denied";
        Console.WriteLine($"PassiveMachine: {this.GetState().ToString()} -> {State.Second.ToString()}: {message}");
        return result;
    }

    [CommandMethod]
    protected CommandResult ReceiveString(string message)
    {
        Console.WriteLine($"PassiveMachine command: {message}");
        return CommandResult.Success;
    }
}
```



### Intermittent Machine

Intermittent machine is a machine that runs at regular intervals.

```csharp
[MachineObject]
public partial class IntermittentMachine : Machine<int>
{
    public static void Test(BigMachine bigMachine)
    {
        // The machine will run at regular intervals (1 second).
        var machine = bigMachine.IntermittentMachine.GetOrCreate(0);
    }

    public IntermittentMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
        this.Lifespan = TimeSpan.FromSeconds(5); // The time until the machine automatically terminates.
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
        this.TimeUntilRun = TimeSpan.FromSeconds(0.5); // Change the timeout of the machine.
        return StateResult.Continue;
    }
}
```



### Continuous Machine

Continuous machine is different from passive and intermittent machine (passive and intermittent machine are virtually the same).

It's designed for heavy and time-consuming tasks.

```csharp
[TinyhandObject]
[MachineObject(Control = MachineControlKind.Sequential, NumberOfTasks = 1)]
public partial class SequentialMachine : Machine<int>
{// SequentialMachine executes one at a time, in the order of their creation.
    public static void Test(BigMachine bigMachine)
    {
        bigMachine.SequentialMachine.TryCreate(1);
        bigMachine.SequentialMachine.TryCreate(2);
        bigMachine.SequentialMachine.TryCreate(3);
    }

    public SequentialMachine()
    {
        this.Lifespan = TimeSpan.FromSeconds(5);
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
    }

    [Key(10)]
    public int Count { get; set; }

    [StateMethod(0)]
    protected async Task<StateResult> Initial(StateParameter parameter)
    {
        Console.WriteLine($"SequentialMachine machine[{this.Identifier}]: {this.Count++}");

        await Task.Delay(500).ConfigureAwait(false); // Some heavy task

        if (this.Count >= 3)
        {
            return StateResult.Terminate;
        }

        return StateResult.Continue;
    }
}
```

To improve response and share resource, heavy task should not be done at once, but divided into several smaller tasks.



## Serialization

Thanks to [Tinyhand](https://github.com/archi-Doc/CrystalData) and [CrystalData](https://github.com/archi-Doc/CrystalData), serialization and persistence of **BigMachine** is very easy.

Add the `TinyhandObject` attribute to the **Machine** class, and use the code below to serialize and deserialize.

```csharp
[TinyhandObject]
[MachineObject]
public partial class TestMachine : Machine<int> {}
```

```csharp
var bin = TinyhandSerializer.Serialize(bigMachine);
var bigMachine2 = TinyhandSerializer.Deserialize<BigMachine>(bin);
```



If you want to save to a file, register it with **CrystalData** as shown in the following code.

```csharp
var builder = new CrystalControl.Builder()
    .ConfigureCrystal(context =>
    {
        context.AddCrystal<BigMachine>(new()
        {
            FileConfiguration = new LocalFileConfiguration("Data/BigMachine.tinyhand"),
            SavePolicy = SavePolicy.Manual,
            SaveFormat = SaveFormat.Utf8,
            NumberOfFileHistories = 3,
        });
    });
```



## Identifier

`Identifier` is a key concept of `BigMachines`.



## Other

### Virtual methods

These are virtual functions of `Machine` class.

You can override and use them when necessary.

```csharp
/// <summary>
/// Called when the machine is newly created.<br/>
/// Note that it is not called after deserialization.<br/>
/// <see cref="OnCreate(object?)"/> -> <see cref="OnStart()"/> -> <see cref="OnTerminate"/>.
/// </summary>
/// <param name="createParam">The parameters used when creating a machine.</param>
protected virtual void OnCreate(object? createParam)
{
}

/// <summary>
/// Called when the machine is ready to start<br/>
/// Note that it is called before the actual state method.<br/>
/// <see cref="OnCreate(object?)"/> -> <see cref="OnStart()"/> -> <see cref="OnTerminate"/>.
/// </summary>
protected virtual void OnStart()
{
}

/// <summary>
/// Called when the machine is terminating.<br/>
///  This code is inside a semaphore lock.<br/>
///  <see cref="OnCreate(object?)"/> -> <see cref="OnStart()"/> -> <see cref="OnTerminate"/>.
/// </summary>
protected virtual void OnTerminate()
{
}
```



### Service provider

Since the machine is independent, you cannot pass parameters directly when creating an instance (and mainly for the deserialization process).

```csharp
public class SomeService
{
    public void Print(string? text) => Console.WriteLine($"Some service : {text}");
}

// Machine depends on SomeService.
[MachineObject(UseServiceProvider = true)]
public partial class ServiceProviderMachine : Machine<int>
{
    public static void Test(BigMachine bigMachine)
    {
        bigMachine.ServiceProviderMachine.GetOrCreate(0, "A"); // Create a machine and set a parameter.
    }

    public ServiceProviderMachine(SomeService service)
        : base()
    {
        this.Service = service;
        this.DefaultTimeout = TimeSpan.FromSeconds(1);
        this.Lifespan = TimeSpan.FromSeconds(3);
    }

    protected override void OnCreation(object? createParam)
    {// Receives the parameter at the time of creation. Note that it is not called during deserialization.
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



### Recursive calls checker

Relationships between machines can become complicated, and may lead to circular command issuing.

```csharp

```



### Exception handling

Each machine is designed to run independently.

So exceptions thrown in machines are handled by **BigMachines**' main thread (`BigMachine.Core`), not by the caller.

In detail, exceptions are registered to **BigMachines** using `BigMachine.ReportException()`, and handled by the following method in **BigMachines**' main thread.

```cahrp
private static void DefaultExceptionHandler(BigMachineException exception)
{
    throw exception.Exception;
}
```

You can set a custom exception handler using `BigMachine.SetExceptionHandler()`.

