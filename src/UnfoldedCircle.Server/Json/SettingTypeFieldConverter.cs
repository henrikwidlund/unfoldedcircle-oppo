using UnfoldedCircle.Models.Sync;

namespace UnfoldedCircle.Server.Json;

public class SettingTypeFieldConverter : JsonConverter<SettingTypeField>
{
    public override SettingTypeField Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty("label", out _))
        {
            return root.Deserialize<SettingTypeLabel>(UnfoldedCircleJsonSerializerContext.InstanceWithoutCustomConverters.SettingTypeLabel)!;
        }

        if (root.TryGetProperty("dropdown", out _))
        {
            return root.Deserialize<SettingTypeDropdown>(UnfoldedCircleJsonSerializerContext.InstanceWithoutCustomConverters.SettingTypeDropdown)!;
        }

        if (root.TryGetProperty("checkbox", out _))
        {
            return root.Deserialize<SettingTypeCheckbox>(UnfoldedCircleJsonSerializerContext.InstanceWithoutCustomConverters.SettingTypeCheckbox)!;
        }

        if (root.TryGetProperty("password", out _))
        {
            return root.Deserialize<SettingTypePassword>(UnfoldedCircleJsonSerializerContext.InstanceWithoutCustomConverters.SettingTypePassword)!;
        }

        if (root.TryGetProperty("textarea", out _))
        {
            return root.Deserialize<SettingTypeTextArea>(UnfoldedCircleJsonSerializerContext.InstanceWithoutCustomConverters.SettingTypeTextArea)!;
        }

        if (root.TryGetProperty("number", out _))
        {
            return root.Deserialize<SettingTypeNumber>(UnfoldedCircleJsonSerializerContext.InstanceWithoutCustomConverters.SettingTypeNumber)!;
        }

        if (root.TryGetProperty("text", out _))
        {
            return root.Deserialize<SettingTypeText>(UnfoldedCircleJsonSerializerContext.InstanceWithoutCustomConverters.SettingTypeText)!;
        }

        throw new JsonException("Unknown setting type field.");
    }

    public override void Write(Utf8JsonWriter writer, SettingTypeField value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), UnfoldedCircleJsonSerializerContext.InstanceWithoutCustomConverters);
    }
}