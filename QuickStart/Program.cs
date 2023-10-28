// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;

namespace QuickStart;

/*[MachineObject(0)] // Annotate MachineObject and set Machine type id (unique number).
public partial class TestMachine : Machine<int> // Inherit Machine<TIdentifier> class. The type of the identifier is int.
{
    public TestMachine(BigMachine<int> bigMachine)
        : base(bigMachine)
    {
        this.DefaultTimeout = TimeSpan.FromSeconds(1); // Default time interval for machine execution.
        this.SetLifespan(TimeSpan.FromSeconds(5)); // The time until the machine automatically terminates.
    }

    public int Count { get; set; }

    [StateMethod(0)] // Annotate StateMethod attribute and set state method id (0 for default state).
    protected StateResult Initial(StateParameter parameter) // The name of method becomes the state name.
    {// This code is inside 'lock (this.Machine)' statement.
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

    [CommandMethod(1)] // Annotate CommandMethod attribute to a method which receives and processes commands.
    protected void TestCommand(string message)
    {
        Console.WriteLine($"Command received: {message}");
    }

    protected override void OnTerminated()
    {
        Console.WriteLine($"TestMachine {this.Identifier}: Terminated");
        ThreadCore.Root.Terminate(); // Send a termination signal to the root.
    }
}*/

public class Program
{
    public static async Task Main(string[] args)
    {
        /*var bigMachine = new BigMachine<int>(); // Create a BigMachine instance.
        bigMachine.Start(); // Start a thread to invoke a machine.

        var testMachine = bigMachine.CreateOrGet<TestMachine.Interface>(42); // Machine is created via an interface class and the identifier, not the machine class itself.
        testMachine.TryGetState(out var currentState); // Get the current state. You can operate machines using the interface class.

        testMachine = bigMachine.TryGet<TestMachine.Interface>(42); // Get the created machine.
        testMachine?.RunAsync().Wait(); // Run the machine manually.
        Console.WriteLine();

        var testGroup = bigMachine.GetGroup<TestMachine.Interface>(); // Group is a collection of machines.
        testMachine = testGroup.TryGet<TestMachine.Interface>(42); // Same as above

        Console.WriteLine("Enumerates identifiers.");
        foreach (var x in testGroup.GetIdentifiers())
        {
            Console.WriteLine($"Machine Id: {x}");
        }

        Console.WriteLine();

        testMachine?.CommandAsync(TestMachine.Command.TestCommand, "test message").Wait(); // Send a command to the machine.
        Console.WriteLine();*/

        await ThreadCore.Root.WaitForTerminationAsync(-1); // Wait for the termination infinitely.
    }
}
