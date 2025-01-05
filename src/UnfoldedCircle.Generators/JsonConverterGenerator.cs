using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnfoldedCircle.Generators;

[Generator]
public class JsonConverterGenerator : IIncrementalGenerator
{
    private const string EnumJsonConverterAttribute = "UnfoldedCircle.Generators.EnumJsonConverterAttribute";
    private const string DisplayAttribute = "System.ComponentModel.DataAnnotations.DisplayAttribute";
    private const string DescriptionAttribute = "System.ComponentModel.DescriptionAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx => ctx.AddSource(
            "EnumJsonConverterAttribute.g.cs", SourceText.From(JsonConverterSourceBuilder.Attribute, Encoding.UTF8)));

        var jsonConvertersToGenerate = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                EnumJsonConverterAttribute,
                static (node, _) => node is ClassDeclarationSyntax,
                GetTypeToGenerate)
            .Where(static m => m is not null);

        context.RegisterSourceOutput(jsonConvertersToGenerate,
            static (spc, source) => Execute(source, spc));
    }

    private static void Execute(JsonConverterToGenerate? jsonConverterToGenerate, SourceProductionContext context)
    {
        if (jsonConverterToGenerate is not { } eg)
            return;

        StringBuilder sb = new();
        var result = JsonConverterSourceBuilder.GenerateJsonConverterClass(sb, eg);
        context.AddSource(eg.ConverterType + ".g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static JsonConverterToGenerate? GetTypeToGenerate(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            // nothing to do if this type isn't available
            return null;
        }

        ct.ThrowIfCancellationRequested();
        
        var converterNamespace = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToString();
        var attributes = classSymbol.GetAttributes();
        var enumJsonConverterAttribute = attributes.FirstOrDefault(static ad =>
            ad.AttributeClass?.Name.Equals("EnumJsonConverterAttribute", StringComparison.Ordinal) == true ||
            ad.AttributeClass?.ToDisplayString().Equals(EnumJsonConverterAttribute, StringComparison.Ordinal) == true);

        if (enumJsonConverterAttribute is not { ConstructorArguments.Length: > 0 })
            return null;

        if (enumJsonConverterAttribute.ConstructorArguments[0].Value is not INamedTypeSymbol enumTypeSymbol)
            return null;

        ProcessNamedArguments(enumJsonConverterAttribute,
            out var caseSensitive,
            out var camelCase,
            out var propertyName);
        
        var enumMembers = enumTypeSymbol.GetMembers();
        var members = new List<(string, EnumValueOption)>(enumMembers.Length);
        HashSet<string>? displayNames = null;

        foreach (var member in enumMembers)
        {
            if (member is not IFieldSymbol field
                || field.ConstantValue is null)
            {
                continue;
            }

            string? displayName = null;
            foreach (var attribute in member.GetAttributes())
            {
                if (attribute.AttributeClass?.Name.Equals("DisplayAttribute", StringComparison.Ordinal) == true &&
                    attribute.AttributeClass.ToDisplayString().Equals(DisplayAttribute, StringComparison.Ordinal))
                {
                    foreach (var namedArgument in attribute.NamedArguments)
                    {
                        if (!namedArgument.Key.Equals("Name", StringComparison.Ordinal) || namedArgument.Value.Value?.ToString() is not { } dn)
                            continue;

                        // found display attribute, all done
                        displayName = dn;
                        break;
                    }
                }

                if (attribute.AttributeClass?.Name.Equals(DescriptionAttribute, StringComparison.Ordinal) != true
                    || !attribute.AttributeClass.ToDisplayString().Equals("DescriptionAttribute", StringComparison.Ordinal)
                    || attribute.ConstructorArguments.Length != 1
                    || attribute.ConstructorArguments[0].Value?.ToString() is not { } dn1)
                    continue;

                // found display attribute, all done
                displayName = dn1;
                break;
            }
            
            if (displayName is not null)
            {
                // Handle cases where contains a quote or a backslash
                displayName = displayName
                    .Replace(@"\", @"\\")
                    .Replace("\"", "\\\"");
                displayNames ??= [];
            }

            members.Add((member.Name, new EnumValueOption(displayName)));
        }

        return new JsonConverterToGenerate
        {
            CamelCase = camelCase,
            CaseSensitive = caseSensitive,
            ConverterType = classSymbol.Name,
            ConverterNamespace = converterNamespace,
            FullyQualifiedEnumName = enumTypeSymbol.ToString(),
            PropertyName = propertyName,
            IsPublic = classSymbol.DeclaredAccessibility == Accessibility.Public,
            Members = members
        };
    }

    private static void ProcessNamedArguments(AttributeData attributeData,
        out bool caseSensitive,
        out bool camelCase,
        out string? propertyName)
    {
        caseSensitive = false;
        camelCase = false;
        propertyName = null;

        foreach (var namedArgument in attributeData.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case "CaseSensitive" when namedArgument.Value.Value?.ToString() is { } cs:
                    caseSensitive = bool.Parse(cs);
                    continue;
                case "CamelCase" when namedArgument.Value.Value?.ToString() is { } cc:
                    camelCase = bool.Parse(cc);
                    continue;
                case "PropertyName" when namedArgument.Value.Value?.ToString() is { } pn:
                    propertyName = pn;
                    continue;
            }
        }
    }
}