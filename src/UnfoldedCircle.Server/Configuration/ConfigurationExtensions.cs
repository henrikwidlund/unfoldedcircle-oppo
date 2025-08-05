using System.Globalization;

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
}