namespace UnfoldedCircle.Models.Sync;

[JsonDerivedType(typeof(SettingTypeLabel))]
[JsonDerivedType(typeof(SettingTypeDropdown))]
[JsonDerivedType(typeof(SettingTypeCheckbox))]
[JsonDerivedType(typeof(SettingTypePassword))]
[JsonDerivedType(typeof(SettingTypeTextArea))]
[JsonDerivedType(typeof(SettingTypeNumber))]
[JsonDerivedType(typeof(SettingTypeText))]
public abstract record SettingTypeField;

public record SettingTypeLabel : SettingTypeField
{
    [JsonPropertyName("label")]
    public required SettingTypeLabelItem Label { get; init; }
}

public record SettingTypeLabelItem
{
    [JsonPropertyName("value")]
    public required Dictionary<string, string> Value { get; init; }
}

public record SettingTypeDropdown : SettingTypeField
{
    [JsonPropertyName("dropdown")]
    public required SettingTypeDropdownInner Dropdown { get; init; }
}

public record SettingTypeDropdownInner
{
    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("items")]
    public required SettingTypeDropdownItem[] Items { get; init; }
}

public record SettingTypeDropdownItem
{
    /// <summary>
    /// Selection identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Value { get; init; }

    [JsonPropertyName("label")]
    public required Dictionary<string, string> Label { get; init; }
}

public record SettingTypeCheckbox : SettingTypeField
{
    [JsonPropertyName("checkbox")]
    public required SettingTypeCheckboxInner Checkbox { get; init; }
}

public record SettingTypeCheckboxInner
{
    /// <summary>
    /// Initial setting.
    /// </summary>
    [JsonPropertyName("value")]
    public required bool Value { get; init; }
}

public record SettingTypePassword : SettingTypeField
{
    [JsonPropertyName("password")]
    public ValueRegex? Password { get; init; }
}

public record SettingTypeTextArea : SettingTypeField
{
    [JsonPropertyName("textarea")]
    public required SettingTypeTextAreaInner TextArea { get; init; }
}

public record SettingTypeTextAreaInner
{
    /// <summary>
    /// Optional default value.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

public record SettingTypeNumber : SettingTypeField
{
    [JsonPropertyName("number")]
    public required SettingTypeNumberInner Number { get; init; }
}

public record SettingTypeNumberInner
{
    [JsonPropertyName("value")]
    public required int Value { get; init; }

    [JsonPropertyName("min")]
    public int? Min { get; init; }

    [JsonPropertyName("max")]
    public int? Max { get; init; }

    [JsonPropertyName("steps")]
    public int? Steps { get; init; }

    [JsonPropertyName("decimals")]
    public int? Decimals { get; init; }

    [JsonPropertyName("unit")]
    public Dictionary<string, string>? Unit { get; init; }
}

public record SettingTypeText : SettingTypeField
{
    [JsonPropertyName("text")]
    public required ValueRegex Text { get; init; }
}