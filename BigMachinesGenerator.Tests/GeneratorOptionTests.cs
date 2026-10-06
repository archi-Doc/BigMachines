using BigMachines.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using static BigMachinesGenerator.Tests.GeneratorTestHelper;

namespace BigMachinesGenerator.Tests;

public class GeneratorOptionTests
{
    private const string RootSource = """
        using BigMachines;

        [BigMachineObject]
        public partial class Root;
        """;

    private const string OptionSource = """
        using BigMachines;

        [BigMachinesGeneratorOption(CustomNamespace = "CustomModule")]
        public partial class Option;
        """;

    [Fact]
    public void OptionsDoNotLeakIntoLaterRuns()
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BigMachinesGeneratorV2());

        driver = driver.RunGenerators(CreateCompilation(("Option.cs", OptionSource), ("Root.cs", RootSource)), TestContext.Current.CancellationToken);
        Assert.Contains("namespace CustomModule", GetGeneratedText(driver));

        // The same generator instance runs again after the option is removed.
        driver = driver.RunGenerators(CreateCompilation(("Root.cs", RootSource)), TestContext.Current.CancellationToken);
        var generated = GetGeneratedText(driver);
        Assert.DoesNotContain("CustomModule", generated);
        Assert.Contains("public static class BigMachinesModule_Test", generated);
    }

    [Fact]
    public void OptionInTreeWithoutPathIsApplied()
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BigMachinesGeneratorV2());
        driver = driver.RunGenerators(CreateCompilation((string.Empty, OptionSource), ("Root.cs", RootSource)), TestContext.Current.CancellationToken);

        var result = driver.GetRunResult();
        Assert.All(result.Results, x => Assert.Null(x.Exception));
        Assert.Contains("namespace CustomModule", GetGeneratedText(driver));
    }

    [Theory]
    [InlineData("[BigMachineObject]\n[BigMachinesGeneratorOption(CustomNamespace = \"CustomModule\")] public partial class Root;")]
    [InlineData("[MachineObject]\n[BigMachinesGeneratorOption(CustomNamespace = \"CustomModule\")] public partial class Item : Machine;")]
    public void OptionsOnGeneratedTypesAreApplied(string declaration)
    {
        var result = Generate("using BigMachines;\n" + declaration);
        AssertCompiles(result.Compilation);
        Assert.Contains("namespace CustomModule", GetGeneratedText(result.Driver));
    }

    [Fact]
    public void AttributeAliasesAreRecognized()
    {
        var result = Generate("""
            using BigMachines;
            using RootMarker = BigMachines.BigMachineObjectAttribute;
            using MachineMarker = BigMachines.MachineObjectAttribute;
            using Options = BigMachines.BigMachinesGeneratorOptionAttribute;

            [RootMarker, Options(CustomNamespace = "Aliases")]
            [AddMachine<Item>]
            public partial class Root;

            [MachineMarker]
            public partial class Item : Machine;
            """);

        AssertCompiles(result.Compilation);
        var generated = GetGeneratedText(result.Driver);
        Assert.Contains("namespace Aliases", generated);
        Assert.Contains("public partial class Item", generated);
        Assert.Contains("public partial class Root : BigMachineBase", generated);
    }

    [Fact]
    public void ModuleInitializerCanBeDisabledWithoutRemovingManualInitialization()
    {
        var compilation = CreateCompilation(("Root.cs", RootSource), ("Option.cs", OptionSource.Replace("CustomNamespace = \"CustomModule\"", "CustomNamespace = \"CustomModule\", UseModuleInitializer = false")));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BigMachinesGeneratorV2());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _, TestContext.Current.CancellationToken);

        AssertCompiles(output);
        Assert.DoesNotContain("[ModuleInitializer]", GetGeneratedText(driver));
        Assert.Contains("public static void Initialize()", GetGeneratedText(driver));

        driver = driver.RunGenerators(CreateCompilation(("Root.cs", RootSource)), TestContext.Current.CancellationToken);
        Assert.Contains("[ModuleInitializer]", GetGeneratedText(driver));
    }

    [Fact]
    public void UnrelatedAttributesDoNotGenerateSources()
    {
        var result = Generate("[System.Serializable] public class Item;");
        AssertCompiles(result.Compilation);
        Assert.Empty(result.Driver.GetRunResult().GeneratedTrees);
    }
}
