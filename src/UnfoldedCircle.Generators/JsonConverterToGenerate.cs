namespace UnfoldedCircle.Generators;

public readonly record struct JsonConverterToGenerate(
    string ConverterType,
    string? ConverterNamespace,
    bool IsPublic,
    string FullyQualifiedEnumName,
    bool CaseSensitive,
    bool CamelCase,
    string? PropertyName,
    List<(string EnumMember, EnumValueOption EnumValueOption)> Members);