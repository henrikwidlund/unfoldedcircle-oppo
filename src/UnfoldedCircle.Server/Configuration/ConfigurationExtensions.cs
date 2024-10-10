using System.Globalization;

namespace UnfoldedCircle.Server.Configuration;

public static class ConfigurationExtensions
{
    public static T GetRequired<T>(this IConfiguration configuration, string key)
        where T : IParsable<T>
    {
        var s = configuration[key];
        if (T.TryParse(s, CultureInfo.InvariantCulture, out var value))
            return value;
        
        throw new InvalidOperationException($"Could not parse value '{s}' for key '{key}'");
    }
    
    public static T GetOrDefault<T>(this IConfiguration configuration, string key, T defaultValue)
        where T : IParsable<T> =>
        T.TryParse(configuration[key], CultureInfo.InvariantCulture, out var value) ? value : defaultValue;
}