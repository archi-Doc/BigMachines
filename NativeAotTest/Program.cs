// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Threading.Tasks;
using Arc.Threading;
using BigMachines;
using Tinyhand;

namespace NativeAotTest;

[BigMachineObject]
[AddMachine<AotSingleMachine>]
[AddMachine<AotUnorderedMachine>]
[AddMachine<AotSequentialMachine>]
public partial class AotBigMachine;

[TinyhandObject]
[MachineObject]
public partial class AotSingleMachine : Machine
{
    [Key(10)]
    public int Value { get; set; }

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
[MachineObject]
public partial class AotUnorderedMachine : Machine<int>
{
    [Key(10)]
    public int Value { get; set; }

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
[MachineObject(Control = MachineControlKind.Sequential)]
public partial class AotSequentialMachine : Machine<int>
{
    [Key(10)]
    public int Value { get; set; }

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

public static class Program
{
    public static async Task Main()
    {
        var sourceRoot = new ExecutionRoot();
        var destinationRoot = new ExecutionRoot();
        try
        {
            var source = new AotBigMachine(sourceRoot);
            var single = source.AotSingleMachine.GetOrCreate();
            Ensure(await single.Command.SetValue(11) == CommandResult.Success, "Single-machine command failed.");

            var unordered = source.AotUnorderedMachine.GetOrCreate(2);
            Ensure(await unordered.Command.SetValue(22) == CommandResult.Success, "Unordered-machine command failed.");

            var sequential = source.AotSequentialMachine.TryCreate(3)
                ?? throw new InvalidOperationException("Sequential machine was not created.");
            Ensure(await sequential.Command.SetValue(33) == CommandResult.Success, "Sequential-machine command failed.");

            var data = TinyhandSerializer.Serialize(source);
            TinyhandSerializer.ServiceProvider = new RootServiceProvider(destinationRoot);
            var restored = TinyhandSerializer.Deserialize<AotBigMachine>(data)
                ?? throw new InvalidOperationException("Big-machine root was not deserialized.");

            if (!restored.AotSingleMachine.TryGet(out var restoredSingle))
            {
                throw new InvalidOperationException("Single machine was not restored.");
            }

            Ensure((await restoredSingle.Command.GetValue()).Response == 11, "Single-machine state did not round-trip.");

            if (!restored.AotUnorderedMachine.TryGet(2, out var restoredUnordered))
            {
                throw new InvalidOperationException("Unordered machine was not restored.");
            }

            Ensure((await restoredUnordered.Command.GetValue()).Response == 22, "Unordered-machine state did not round-trip.");

            var restoredSequential = restored.AotSequentialMachine.TryGet(3)
                ?? throw new InvalidOperationException("Sequential machine was not restored.");
            Ensure((await restoredSequential.Command.GetValue()).Response == 33, "Sequential-machine state did not round-trip.");

            Console.WriteLine("NativeAOT smoke test passed.");
        }
        finally
        {
            TinyhandSerializer.ServiceProvider = EmptyServiceProvider.Instance;
            destinationRoot.RequestTermination();
            sourceRoot.RequestTermination();
        }
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class RootServiceProvider(ExecutionRoot root) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(ExecutionRoot) ? root : null;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType) => null;
    }
}
