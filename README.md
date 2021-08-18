## BigMachines is State Machine library for .NET
![Nuget](https://img.shields.io/nuget/v/BigMachines) ![Build and Test](https://github.com/archi-Doc/BigMachines/workflows/Build%20and%20Test/badge.svg)

- Very versatile and easy to use.

- Running machines and sending commands to each machine is designed to be **lock-free**.

- Full serialization features integrated with [Tinyhand](https://github.com/archi-Doc/Tinyhand).

- Simplify complex and long-running tasks.

  


## Table of Contents

- [Requirements](#requirements)
- [Quick Start](#quick-start)
- [Machine Types](#machine-types)
- [Machine Class](#machine-class)



## Requirements

**C# 9.0** or later for generated codes.

**.NET 5** or later target framework.



## Quick Start

Install BigMachines using Package Manager Console.

```
Install-Package BigMachines
```

This is a small sample code to use BigMachines.

```csharp
using System;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;

namespace QuickStart
{
    [MachineObject(0)] // Annotate MachineObject and set Machine type id (unique number).
    public partial class TestMachine : Machine<int> // Inherit Machine<TIdentifier> class. The type of an identifier is int.
    {
        public TestMachine(BigMachine<int> bigMachine)
            : base(bigMachine)
        {
            this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
            this.SetLifespan(TimeSpan.FromSeconds(5)); // Time until the machine automatically terminates.
        }

        public int Count { get; set; }

        [StateMethod(0)] // Annotate StateMethod attribute and set state method id (0 for default state).
        protected StateResult Initial(StateParameter parameter) // The name of method becomes the state name.
        {// This code is inside 'lock (this.SyncMachine) {}'.
            Console.WriteLine($"TestMachine {this.Identifier}: Initial");
            this.ChangeState(TestMachine.State.One); // Change to state One.
            return StateResult.Continue; // Continue (StateResult.Terminate to terminate machine).
        }

        [StateMethod(0x6015f7a7)] // State id can be a random number.
        protected StateResult One(StateParameter parameter)
        {
            Console.WriteLine($"TestMachine {this.Identifier}: One - {this.Count++}");
            return StateResult.Continue;
        }

        protected override void OnTerminated()
        {
            Console.WriteLine($"TestMachine {this.Identifier}: Terminated");
            ThreadCore.Root.Terminate(); // Send a termination signal to the root.
        }
    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            var bigMachine = new BigMachine<int>(ThreadCore.Root); // Create BigMachine and set thread core (parent thread).

            var testMachine = bigMachine.TryCreate<TestMachine.Interface>(42); // Machine is created via the interface class and identifier, not the machine class itself.
            if (testMachine != null)
            {
                var currentState = testMachine.GetCurrentState(); // Get current state. You can operate machines using interface class.
            }

            testMachine = bigMachine.TryGet<TestMachine.Interface>(42); // Get the created machine.

            var testGroup = bigMachine.GetGroup<TestMachine.Interface>(); // Group is a collection of machines.
            testMachine = testGroup.TryGet<TestMachine.Interface>(42); // Same as above

            await ThreadCore.Root.WaitForTermination(-1); // Wait for the termination infinitely.
        }
    }
}
```



## Machine Types

`BigMachines` supports several types of machine.

### Passive Machine

Passive machine can be run and the state can be changed by an external operation.

```csharp
[MachineObject(0xffd829b4)]
public partial class PassiveMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        var m = bigMachine.TryCreate<PassiveMachine.Interface>(0);

        m.Command("message 1"); // Send command.

        m.Run(); // Manually run machine.

        m.ChangeState(State.First); // Change state from State.Initial to State.First
        m.Run(); // Manually run machine.

        m.ChangeState(State.Second); // Change state from State.First to State.Second (denied)
        m.Run(); // Manually run machine.

        m.ChangeState(State.Second); // Change state from State.First to State.Second (approved)
        m.Run(); // Manually run machine.
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

    protected override void ProcessCommand(CommandPost<int>.Command command)
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
        var m = bigMachine.TryCreate<IntermittentMachine.Interface>(0);

        // The machine will run at regular intervals (1 second).
    }

    public IntermittentMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
        this.SetLifespan(TimeSpan.FromSeconds(5)); // Time until the machine automatically terminates.
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
[MachineObject(0xb579a7d8, Continuous = true)] // Set Continuous property to true.
public partial class ContinuousMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        bigMachine.Continuous.SetMaxThreads(2); // Set the maximum number of threads used for continuous machines.
        var m = bigMachine.TryCreate<ContinuousMachine.Interface>(0);

        // The machine will run until the task is complete.
    }

    public ContinuousMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
    }

    public int Count { get; set; }

    [StateMethod(0)]
    protected StateResult Initial(StateParameter parameter)
    {
        Console.WriteLine($"ContinuousMachine: Initial - {this.Count++}");
        if (this.Count > 10)
        {
            Console.WriteLine($"ContinuousMachine: Done");
            return StateResult.Terminate;
        }

        Thread.Sleep(100); // Some heavy task

        return StateResult.Continue;
    }
}
```

To improve response and share resource, heavy task should not be done at once, but divided into several smaller tasks.



Identifier



## Machine Class

### Reserved keywords

These keywords in `Machine` class are reserved for source generator.

- `Interface`: Nested class for operating a machine.
- `State`: enum type which represents the state of a machine.
- `CreateInterface()`: Creates an instance of machine interface.
- `RunInternal()`: Runs machine and process each state.
- `ChangeState()`: Changes the state of a machine.
- `GetState()`: Gets the current state of a machine.
- `IntChangeState()`: Internally used to change the state.
- `RegisterBM()`: Registers the machine to `BigMachine`.



## Other

### Generic machine

`BigMachine` and `Machine` 



### Loop checker

Relationships between machines can become complicated, and may lead to circular command issue.

```csharp
[MachineObject(0xb7196ebc)]
public partial class LoopMachine : Machine<int>
{
    public static void Test(BigMachine<int> bigMachine)
    {
        var loopMachine = bigMachine.TryCreate<LoopMachine.Interface>(0);
        loopMachine.Command(1);
    }

    public LoopMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
    }

    protected override void ProcessCommand(CommandPost<int>.Command command)
    {
        if (command.Message is int n)
        {// LoopMachine
            this.BigMachine.TryGet<Interface>(this.Identifier)?.Command(0);
        }
    }
}
```

This code will cause `InvalidOperationException`.

You can disable loop checker if you want (not recommended).

```csharp
bigMachine.EnableLoopChecker = false;
```

