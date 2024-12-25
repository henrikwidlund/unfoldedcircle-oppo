using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

using UnfoldedCircle.Server.Configuration;

namespace UnfoldedCircle.Server.Logging;

file sealed class HttpLogger(string name, IOptionsMonitor<LoggerFilterOptions> logFilterOptions, IConfiguration configuration) : ILogger
{
    private readonly IOptionsMonitor<LoggerFilterOptions> _logFilterOptions = logFilterOptions;
    private readonly string _name = name;
    private readonly IConfiguration _configuration = configuration;
    
    // This should use factory, but the logger is used before the factory is available.
    private static readonly HttpClient HttpClient = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _logFilterOptions.CurrentValue.MinLevel;
    
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var logMessage = $"{DateTime.UtcNow:O} {eventId.Id,2}: {logLevel,-12} {_name} - {formatter(state, exception)}";
        Console.WriteLine(logMessage);
        
        try
        {
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _configuration.GetRequired<string>("HttpLogger:Endpoint"));
            httpRequestMessage.Content = new StringContent(logMessage, Encoding.UTF8);
            HttpClient.Send(httpRequestMessage);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

[ProviderAlias("HttpLogger")]
file sealed class HttpLoggerProvider(IOptionsMonitor<LoggerFilterOptions> logFilterOptions, IConfiguration configuration) : ILoggerProvider
{
    private readonly IOptionsMonitor<LoggerFilterOptions> _logFilterOptions = logFilterOptions;
    private readonly IConfiguration _configuration = configuration;
    private readonly ConcurrentDictionary<string, HttpLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName,
            static (name, factoryArgs) => new HttpLogger(name, factoryArgs._logFilterOptions, factoryArgs._configuration),
            (_logFilterOptions, _configuration));
    }
    
    public void Dispose() => _loggers.Clear();
}

public static class HttpLoggerExtensions
{
    public static ILoggingBuilder AddHttpLogger(this ILoggingBuilder builder)
    {
        builder.ClearProviders();
        builder.AddConfiguration();

        builder.SetMinimumLevel(LogLevel.Trace);
        
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, HttpLoggerProvider>());

        return builder;
    }
}