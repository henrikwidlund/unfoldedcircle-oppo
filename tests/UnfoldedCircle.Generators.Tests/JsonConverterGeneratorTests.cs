using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UnfoldedCircle.Generators.Tests;

public class JsonConverterGeneratorTests
{
    [Fact]
    public Task Attribute_Usage_Generates_Proper_Converter()
    {
        const string input = """
                             using System.Text.Json.Serialization;
                             using UnfoldedCircle.Generators;

                             namespace MyTestNameSpace
                             {
                                 [EnumJsonConverterAttribute(typeof(MyEnum), CamelCase = false, CaseSensitive = false)]
                                 internal partial class MyEnumConverter;
                                 
                                 [JsonConverter(typeof(MyEnumConverter))]
                                 internal enum MyEnum
                                 {
                                     First = 0,
                                     Second = 1
                                 }
                             }
                             """;
        var (diagnostics, output) = TestHelpers.GetGeneratedOutput<JsonConverterGenerator>(input);

        Assert.Empty(diagnostics);
        return Verify(output).UseDirectory("Snapshots");
    }
}

internal static class TestHelpers
{
    public static (ImmutableArray<Diagnostic> Diagnostics, string Output) GetGeneratedOutput<T>(string source)
        where T : IIncrementalGenerator, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(static assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(static assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat([
                MetadataReference.CreateFromFile(typeof(T).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(EnumJsonConverterAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly.Location)
            ]);

        var compilation = CSharpCompilation.Create(
            "generator",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var originalTreeCount = compilation.SyntaxTrees.Length;
        var generator = new T();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var trees = outputCompilation.SyntaxTrees.ToList();

        return (diagnostics, trees.Count != originalTreeCount ? trees[^1].ToString() : string.Empty);
    }
}