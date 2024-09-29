namespace UnfoldedCircle.Generators;

/// <summary>
/// Add to enums to indicate that a JsonConverter for the enum should be generated.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EnumJsonConverterAttribute : Attribute
{
    /// <summary>
    /// The enum type that the converter should be generated for.
    /// </summary>
    public Type EnumType { get; }

    /// <summary>
    /// Indicates if the string representation is case sensitive when deserializing it as an enum.
    /// </summary>
    public bool CaseSensitive { get; set; }

    /// <summary>
    /// Indicates if the value of <see cref="PropertyName"/> should be camel cased. Default is <see langword="true" />.
    /// </summary>
    public bool CamelCase { get; set; } = true;

    /// <summary>
    /// If set, this value will be used in messages when there are problems with validation and/or serialization/deserialization occurs.
    /// </summary>
    public string? PropertyName { get; set; }

    /// <summary>
    /// Creates a converter for tye given type in <paramref name="enumType"/>.
    /// </summary>
    /// <param name="enumType">The enum type the converter should be generated for.</param>
    public EnumJsonConverterAttribute(Type enumType) => EnumType = enumType;
}