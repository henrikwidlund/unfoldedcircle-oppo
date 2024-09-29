using Microsoft.Extensions.Caching.Memory;

using UnfoldedCircle.Server.Json;

namespace UnfoldedCircle.Server.AlbumCover;

public interface IAlbumCoverService
{
    Task<Uri?> GetAlbumCoverAsync(string artist, string? album, string? track, CancellationToken cancellationToken = default);
}

internal class AlbumCoverService(
    HttpClient httpClient,
    IMemoryCache memoryCache,
    UnfoldedCircleJsonSerializerContext unfoldedCircleJsonSerializerContext,
    ILogger<AlbumCoverService> logger) : IAlbumCoverService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly UnfoldedCircleJsonSerializerContext _unfoldedCircleJsonSerializerContext = unfoldedCircleJsonSerializerContext;
    private readonly ILogger<AlbumCoverService> _logger = logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(3);

    public async Task<Uri?> GetAlbumCoverAsync(string artist, string? album, string? track, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(album))
        {
            return await _memoryCache.GetOrCreateAsync((artist, track), async entry =>
            {
                entry.SetSlidingExpiration(CacheDuration);
                
                var artistTrackResponse = await _httpClient.GetFromJsonAsync($"https://musicbrainz.org/ws/2/recording/?query=artist:{artist}%20AND%20track:{track}&fmt=json",
                    _unfoldedCircleJsonSerializerContext.ArtistTrackResponse,
                    cancellationToken);

                if (artistTrackResponse is not { Recordings.Length: > 0 })
                    return null;

                foreach (var release in artistTrackResponse.Recordings
                             .Where(static x => x.Score > 90)
                             .SelectMany(static x => x.Releases))
                {
                    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://coverartarchive.org/release/{release.Id}/front-250");
                    var httpResponseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    if (httpResponseMessage.IsSuccessStatusCode)
                        return requestMessage.RequestUri;
                }

                _logger.LogDebug("No album cover found for {Artist} - {Album}", artist, album);
                return null;
            });
        }
        
        return await _memoryCache.GetOrCreateAsync((artist, album), async entry =>
        {
            entry.SetSlidingExpiration(CacheDuration);
            
            var artistAlbumsResponse = await _httpClient.GetFromJsonAsync($"https://musicbrainz.org/ws/2/release/?query=artist:{artist}%20AND%20release:{album}&fmt=json",
                _unfoldedCircleJsonSerializerContext.ArtistAlbumsResponse,
                cancellationToken);

            if (artistAlbumsResponse is not { Releases.Length: > 0 })
                return null;
            
            foreach (var release in artistAlbumsResponse.Releases.Where(static x => x.Score > 90))
            {
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://coverartarchive.org/release/{release.Id}/front-250");
                var httpResponseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (httpResponseMessage.IsSuccessStatusCode)
                    return requestMessage.RequestUri;
            }

            _logger.LogDebug("No album cover found for {Artist} - {Album}", artist, album);
            return null;
        });
    }
}

internal record ArtistAlbumsResponse(
    [property: JsonPropertyName("releases")]
    ArtistAlbumsReleases[] Releases
);

internal record ArtistAlbumsReleases(
    [property: JsonPropertyName("id")]
    string Id,
    
    [property: JsonPropertyName("score")]
    int Score
);

internal record ArtistTrackResponse(
    [property: JsonPropertyName("recordings")]
    Recordings[] Recordings
);

internal record Recordings(
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName("score")]
    int Score,
    [property: JsonPropertyName("releases")]
    RecordingsRelease[] Releases
);

internal record RecordingsRelease(
    [property: JsonPropertyName("id")]
    string Id
);

