using System.Globalization;

namespace UnfoldedCircle.Server.Configuration;

public static class ConfigurationExtensions
{
    public static T GetOrDefault<T>(this IConfiguration configuration, string key, T defaultValue)
        where T : IParsable<T> =>
        T.TryParse(configuration[key], CultureInfo.InvariantCulture, out var value) ? value : defaultValue;
}