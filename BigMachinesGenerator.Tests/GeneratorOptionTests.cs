using System;
using System.IO;
using System.Linq;
using BigMachines.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

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

    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Append(typeof(BigMachines.Machine).Assembly.Location)
        .Distinct()
        .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x))
        .ToArray();

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

    private static CSharpCompilation CreateCompilation(params (string Path, string Source)[] sources)
        => CSharpCompilation.Create(
            "Test",
            sources.Select(x => CSharpSyntaxTree.ParseText(x.Source, path: x.Path.Length == 0 ? string.Empty : Path.Combine(AppContext.BaseDirectory, x.Path))),
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static string GetGeneratedText(GeneratorDriver driver)
        => string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(x => x.ToString()));
}
