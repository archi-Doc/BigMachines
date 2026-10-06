using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BigMachines.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace BigMachinesGenerator.Tests;

internal static class GeneratorTestHelper
{
    private static readonly MetadataReference[] References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Append(typeof(BigMachines.Machine).Assembly.Location)
        .Distinct()
        .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x))
        .ToArray();

    internal static CSharpCompilation CreateCompilation(params (string Path, string Source)[] sources)
        => CSharpCompilation.Create(
            "Test",
            sources.Select(x => CSharpSyntaxTree.ParseText(x.Source, path: x.Path.Length == 0 ? string.Empty : Path.Combine(AppContext.BaseDirectory, x.Path))),
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

    internal static (GeneratorDriver Driver, Compilation Compilation) Generate(string source)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BigMachinesGeneratorV2());
        driver = driver.RunGeneratorsAndUpdateCompilation(CreateCompilation(("Test.cs", source)), out var compilation, out _, TestContext.Current.CancellationToken);
        Assert.All(driver.GetRunResult().Results, x => Assert.Null(x.Exception));
        return (driver, compilation);
    }

    internal static void AssertCompiles(Compilation compilation)
        => Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(x => x.Severity == DiagnosticSeverity.Error));

    internal static string GetGeneratedText(GeneratorDriver driver)
        => string.Join("\n", driver.GetRunResult().GeneratedTrees.Select(x => x.ToString()));

    internal static bool Evaluate(Compilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        return (bool)assembly.GetType("Check")!.GetMethod("Run", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null)!;
    }
}
