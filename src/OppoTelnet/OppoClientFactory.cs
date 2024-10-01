using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace OppoTelnet;

public class OppoClientFactory(ILoggerFactory loggerFactory, ILogger<OppoClientFactory> logger) : IOppoClientFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly ILogger<OppoClientFactory> _logger = logger;
    private readonly ConcurrentDictionary<OppoClientKey, IOppoClient> _clients = new();

    public IOppoClient? TryGetOrCreateClient(in OppoClientKey oppoClientKey)
    {
        try
        {
            return _clients.GetOrAdd(oppoClientKey, static (oppoClientKey, localLoggerFactory)
                    => new OppoClient(oppoClientKey.HostName, oppoClientKey.Model, localLoggerFactory.CreateLogger<OppoClient>()),
                _loggerFactory);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create client {ClientKey}", oppoClientKey);
            return null;
        }
    }
    
    public void TryDisposeClient(in OppoClientKey oppoClientKey)
    {
        try
        {
            if (_clients.TryRemove(oppoClientKey, out var client))
                client.Dispose();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to dispose client {ClientKey}", oppoClientKey);
            throw;
        }
    }
    
    public void TryDisposeAllClients()
    {
        foreach (var client in _clients)
        {
            try
            {
                client.Value.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to dispose client {ClientKey}", client.Key);
            }
        }
        
        _clients.Clear();
    }
}