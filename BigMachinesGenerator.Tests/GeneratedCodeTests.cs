using System;
using Microsoft.CodeAnalysis;
using Xunit;
using static BigMachinesGenerator.Tests.GeneratorTestHelper;

namespace BigMachinesGenerator.Tests;

public class GeneratedCodeTests
{
    [Fact]
    public void CommandGuardsAvoidBoxingAndRecognizeCombinedFlags()
    {
        var result = Generate("""
            using BigMachines;

            [MachineObject]
            public partial class FlagMachine : Machine
            {
                public int Calls;

                public void SetFlags(OperationalFlags flags) => this.__operationalState__ = flags;

                [CommandMethod(WithLock = false)]
                protected CommandStatus Status()
                {
                    this.Calls++;
                    return CommandStatus.Success;
                }

                [CommandMethod(WithLock = false)]
                protected CommandResult<int> Result()
                {
                    this.Calls++;
                    return new(123);
                }
            }

            public static class Check
            {
                public static bool Run()
                {
                    var machine = new FlagMachine();
                    var commands = ((FlagMachine.Handle)machine.HandleInstance).Command;
                    commands.Status().GetAwaiter().GetResult();
                    var before = System.GC.GetAllocatedBytesForCurrentThread();
                    for (var i = 0; i < 32; i++) commands.Status().GetAwaiter().GetResult();
                    var allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

                    machine.SetFlags(OperationalFlags.Paused | OperationalFlags.Terminated);
                    var status = commands.Status().GetAwaiter().GetResult();
                    var response = commands.Result().GetAwaiter().GetResult();
                    return allocated == 0 && machine.Calls == 33 && status == CommandStatus.Terminated &&
                        response.Status == CommandStatus.Terminated && response.Response == 0;
                }
            }
            """);

        AssertCompiles(result.Compilation);
        Assert.DoesNotContain("HasFlag", GetGeneratedText(result.Driver));
        Assert.True(Evaluate(result.Compilation));
    }

    [Fact]
    public void NullableCommandResponsesAndBroadcastsPreserveTheirAnnotations()
    {
        var result = Generate("""
            #nullable enable
            using BigMachines;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            [MachineObject]
            public partial class Item : Machine<int>
            {
                [CommandMethod(GenerateAllCommand = true)]
                protected CommandResult<string?> NullableResponse() => new((string?)null);

                [CommandMethod(GenerateAllCommand = true)]
                protected CommandResult<string> Response() => new("value");

                [CommandMethod(GenerateAllCommand = true)]
                protected Task<CommandResult<List<string?>?>> NestedNullableResponse()
                    => Task.FromResult(new CommandResult<List<string?>?>(null));
            }
            """);

        AssertCompiles(result.Compilation);
        Assert.DoesNotContain(result.Compilation.GetDiagnostics(TestContext.Current.CancellationToken), x => x.Id.StartsWith("CS86", StringComparison.Ordinal));
        var command = result.Compilation.GetTypeByMetadataName("Item+Handle+CommandList")!;
        var response = (INamedTypeSymbol)((INamedTypeSymbol)((IMethodSymbol)command.GetMembers("NullableResponse")[0]).ReturnType).TypeArguments[0];
        Assert.Equal(NullableAnnotation.Annotated, response.TypeArguments[0].NullableAnnotation);
    }

    [Fact]
    public void ReservedControlAccessorReportsAClearDiagnostic()
    {
        var result = Generate("using BigMachines; [BigMachineObject] public partial class Root { public void GetControlsCore() {} }");

        Assert.Contains(result.Driver.GetRunResult().Diagnostics, x => x.Id == "BMG004" && x.GetMessage().Contains("GetControlsCore", StringComparison.Ordinal));
        Assert.Empty(result.Driver.GetRunResult().GeneratedTrees);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void KeywordMethodsAndGeneratedLocalNamesCompile(bool withLock)
    {
        var result = Generate($$"""
            using BigMachines;
            using System.Threading.Tasks;

            [BigMachineObject]
            [AddMachine<Item>]
            public partial class Root;

            [MachineObject]
            public partial class Item : Machine<int>
            {
                [StateMethod(0)]
                protected StateResult @default(StateParameter parameter) => StateResult.Continue;
                private bool defaultCanEnter() => true;
                private bool defaultCanExit() => true;

                [StateMethod(1)]
                protected Task<StateResult> @return(StateParameter parameter) => Task.FromResult(StateResult.Terminate);

                [CommandMethod(GenerateAllCommand = true, WithLock = {{withLock.ToString().ToLowerInvariant()}})]
                protected CommandStatus @event(int __exception__, int __locked__, int __control__, int __handles__, int __results__, int __i__, int __gen_bm_command__001, int __gen_bm_command__002, int @class)
                    => CommandStatus.Success;

                [CommandMethod(GenerateAllCommand = true, WithLock = {{withLock.ToString().ToLowerInvariant()}})]
                protected Task<CommandResult<int>> @switch(int __gen_bm_command__001, int __gen_bm_command__003)
                    => Task.FromResult(new CommandResult<int>(1));
            }
            """);

        AssertCompiles(result.Compilation);
    }

    [Theory]
    [InlineData("[StateMethod(0)] protected static StateResult Initial(StateParameter parameter) => StateResult.Continue;", "BMG006")]
    [InlineData("[StateMethod(0)] protected StateResult Initial<T>(StateParameter parameter) => StateResult.Continue;", "BMG006")]
    [InlineData("[StateMethod(0)] protected StateResult Initial(ref StateParameter parameter) => StateResult.Continue;", "BMG006")]
    [InlineData("[CommandMethod] protected static CommandStatus Call() => CommandStatus.Success;", "BMG014")]
    [InlineData("[CommandMethod] protected CommandStatus Call<T>() => CommandStatus.Success;", "BMG014")]
    [InlineData("[CommandMethod] protected CommandStatus Call(ref int value) => CommandStatus.Success;", "BMG014")]
    [InlineData("[CommandMethod] protected CommandStatus Call(System.Span<int> value) => CommandStatus.Success;", "BMG014")]
    [InlineData("[CommandMethod] protected int Call() => 0;", "BMG014")]
    public void UnsupportedMethodsReportGeneratorDiagnostics(string method, string diagnosticId)
    {
        var result = Generate("using BigMachines; [MachineObject] public partial class Item : Machine { " + method + " }");

        Assert.Contains(result.Driver.GetRunResult().Diagnostics, x => x.Id == diagnosticId && x.Severity == DiagnosticSeverity.Error);
        Assert.Empty(result.Driver.GetRunResult().GeneratedTrees);
    }

    [Fact]
    public void SynchronousStatesUseSharedCompletedTasks()
    {
        var result = Generate("""
            using BigMachines;
            [MachineObject]
            public partial class Item : Machine
            {
                [StateMethod(0)]
                protected StateResult Initial(StateParameter parameter) => StateResult.Terminate;

                public System.Threading.Tasks.Task<StateResult> Invoke() => this.__InternalRun__(default);

                public void SetInvalidState() => this.__machineState__ = 123;
            }

            public static class Check
            {
                public static bool Run()
                {
                    var machine = new Item();
                    var result = machine.Invoke();
                    machine.SetInvalidState();
                    return result.Result == StateResult.Terminate && object.ReferenceEquals(result, machine.Invoke());
                }
            }
            """);

        AssertCompiles(result.Compilation);
        Assert.True(Evaluate(result.Compilation));
    }

    [Fact]
    public void RootKeepsPublicSnapshotAndProvidesAllocationFreeInternalAccess()
    {
        var result = Generate("""
            using BigMachines;
            [BigMachineObject]
            [BigMachinesGeneratorOption(UseModuleInitializer = false)]
            public partial class Root
            {
                public bool CheckControls()
                {
                    var snapshot = this.GetControls();
                    snapshot[0] = null!;
                    return this.GetControls()[0] == this.ManualControl &&
                        object.ReferenceEquals(this.GetControlsCore(), this.GetControlsCore());
                }
            }

            public static class Check
            {
                public static bool Run()
                {
                    using var executionRoot = new Arc.Threading.ExecutionRoot();
                    return new Root(executionRoot).CheckControls();
                }
            }
            """);

        AssertCompiles(result.Compilation);
        Assert.True(Evaluate(result.Compilation));
    }

    [Fact]
    public void EmptyBroadcastReusesTheEmptyResultArray()
    {
        var result = Generate("""
            using BigMachines;
            [BigMachineObject]
            [AddMachine<Item>]
            public partial class Root;

            [MachineObject]
            public partial class Item : Machine<int>
            {
                [CommandMethod(GenerateAllCommand = true)]
                protected CommandStatus Ping() => CommandStatus.Success;
            }

            public static class Check
            {
                public static bool Run()
                {
                    using var executionRoot = new Arc.Threading.ExecutionRoot();
                    var root = new Root(executionRoot);
                    var first = root.Item.AllPing().GetAwaiter().GetResult();
                    var second = root.Item.AllPing().GetAwaiter().GetResult();
                    return first.Length == 0 && object.ReferenceEquals(first, second);
                }
            }
            """);

        AssertCompiles(result.Compilation);
        Assert.True(Evaluate(result.Compilation));
    }
}
