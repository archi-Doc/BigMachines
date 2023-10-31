// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;

namespace QuickStart;

[MachineObject] // Add a MachineObject attribute.
public partial class FirstMachine : Machine<int> // Inherit Machine class. The type of an identifier is int.
{
    public FirstMachine()
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
        this.Lifespan = TimeSpan.FromSeconds(5); // The time until the machine automatically terminates.
    }

    public int Count { get; set; }

    [StateMethod(0)] // Add a StateMethod attribute and set the state method id (0 for default state).
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
        ((IBigMachine)bigMachine).Core.ChangeParent(ThreadCore.Root);
        ((IBigMachine)bigMachine).Start(); // Start a thread to invoke a machine.

        var testMachine = bigMachine.FirstMachine.GetOrCreate(42); // Machine is created via an interface class and the identifier, not the machine class itself.
        testMachine.TryGetState(out var currentState); // Get the current state. You can operate machines using the interface class.

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
