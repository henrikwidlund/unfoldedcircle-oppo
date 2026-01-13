using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

namespace Oppo;

public class OppoClientFactory(ILoggerFactory loggerFactory, ILogger<OppoClientFactory> logger) : IOppoClientFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly ILogger<OppoClientFactory> _logger = logger;
    private readonly ConcurrentDictionary<int, IOppoClient> _clients = new();
    private ILogger<OppoClient>? _oppoClientLogger;
    private ILogger<MagnetarClient>? _magnetarClientLogger;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public async ValueTask<IOppoClient?> TryGetOrCreateClient(OppoClientKey oppoClientKey, CancellationToken cancellationToken)
    {
        var clientKeyHash = oppoClientKey.GetHashCode();
        if (_clients.TryGetValue(clientKeyHash, out var client))
            return client;

        if (await _semaphoreSlim.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
        {
            try
            {
                if (_clients.TryGetValue(clientKeyHash, out client))
                    return client;

                if (oppoClientKey.Model == OppoModel.Magnetar)
                {
                    _magnetarClientLogger ??= _loggerFactory.CreateLogger<MagnetarClient>();
                    client = new MagnetarClient(oppoClientKey.HostName, _magnetarClientLogger);
                }
                else
                {
                    _oppoClientLogger ??= _loggerFactory.CreateLogger<OppoClient>();
                    client = new OppoClient(oppoClientKey.HostName, oppoClientKey.Model, _oppoClientLogger);
                }

                _clients[clientKeyHash] = client;
                return client;
            }
            catch (Exception e)
            {
                _logger.TryGetOrCreateClientException(e, oppoClientKey);
                return null;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        _logger.CreateClientSemaphoreFailure(oppoClientKey);
        return null;
    }
    
    public void TryDisposeClient(in OppoClientKey oppoClientKey)
    {
        try
        {
            if (_clients.TryRemove(oppoClientKey.GetHashCode(), out var client))
                client.Dispose();
        }
        catch (Exception e)
        {
            _logger.FailedToDisposeClient(e, oppoClientKey);
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
                _logger.FailedToDisposeClient(e, client.Key);
            }
        }
        
        _clients.Clear();
    }
}