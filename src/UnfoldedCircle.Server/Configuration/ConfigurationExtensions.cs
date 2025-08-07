using System.Globalization;

using UnfoldedCircle.Models.Shared;

namespace UnfoldedCircle.Server.Configuration;

public static class ConfigurationExtensions
{
    public static T GetOrDefault<T>(this IConfiguration configuration, string key, T defaultValue)
        where T : IParsable<T> =>
        T.TryParse(configuration[key], CultureInfo.InvariantCulture, out var value) ? value : defaultValue;

    public static string GetValueOrNull<TKey>(this IReadOnlyDictionary<TKey, string> dictionary, TKey key, string defaultValue)
    {
        string value = dictionary.GetValueOrDefault(key, defaultValue);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    public static string GetIdentifier(this string identifier, in EntityType entityType)
    {
        var identifierSpan = identifier.AsSpan()[(identifier.IndexOf(':', StringComparison.Ordinal) + 1)..];
        return entityType switch
        {
            EntityType.Cover => identifier.StartsWith("COVER:", StringComparison.OrdinalIgnoreCase) ? identifier : $"COVER:{identifierSpan}",
            EntityType.Button => identifier.StartsWith("BUTTON:", StringComparison.OrdinalIgnoreCase) ? identifier : $"BUTTON:{identifierSpan}",
            EntityType.Climate => identifier.StartsWith("CLIMATE:", StringComparison.OrdinalIgnoreCase) ? identifier : $"CLIMATE:{identifierSpan}",
            EntityType.Light => identifier.StartsWith("LIGHT:", StringComparison.OrdinalIgnoreCase) ? identifier : $"LIGHT:{identifierSpan}",
            EntityType.MediaPlayer => identifier.Length != identifierSpan.Length ? identifierSpan.ToString() : identifier,
            EntityType.Remote => identifier.StartsWith("REMOTE:", StringComparison.OrdinalIgnoreCase) ? identifier : $"REMOTE:{identifierSpan}",
            EntityType.Sensor => identifier.StartsWith("SENSOR:", StringComparison.OrdinalIgnoreCase) ? identifier : $"SENSOR:{identifierSpan}",
            EntityType.Switch => identifier.StartsWith("SWITCH:", StringComparison.OrdinalIgnoreCase) ? identifier : $"SWITCH:{identifierSpan}",
            _ => throw new ArgumentOutOfRangeException(nameof(entityType), entityType, null)
        };
    }

    public static string? GetUnprefixedIdentifier(this string? identifier) => identifier.GetNullableIdentifier(EntityType.MediaPlayer);

    public static string? GetNullableIdentifier(this string? identifier, in EntityType entityType) => string.IsNullOrWhiteSpace(identifier) ? null : identifier.GetIdentifier(entityType);
}