using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.Caching.Memory;

using UnfoldedCircle.Server.Json;

namespace UnfoldedCircle.Server.AlbumCover;

public interface IAlbumCoverService
{
    Task<Uri?> GetAlbumCoverAsync(string artist, string? album, string? track, CancellationToken cancellationToken = default);
}

internal sealed class AlbumCoverService(
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

                var artistTrackResponse = await SendAndDeserializeAsync(artist, album, track,
                    _unfoldedCircleJsonSerializerContext.ArtistTrackResponse, cancellationToken);

                if (artistTrackResponse is not { Recordings.Length: > 0 })
                    return null;

                foreach (var release in artistTrackResponse.Recordings
                             .Where(static x => x.Score > 90)
                             .SelectMany(static x => x.Releases))
                {
                    var coverUri = await SendAndLogAsync(release.Id, cancellationToken);
                    if (coverUri is not null)
                        return coverUri;
                }

                _logger.LogDebug("No album cover found for {Artist} - {Album}", artist, album);
                return null;
            });
        }
        
        return await _memoryCache.GetOrCreateAsync((artist, album), async entry =>
        {
            entry.SetSlidingExpiration(CacheDuration);
            
            var artistAlbumsResponse = await SendAndDeserializeAsync(artist, album, track,
                _unfoldedCircleJsonSerializerContext.ArtistAlbumsResponse, cancellationToken);

            if (artistAlbumsResponse is not { Releases.Length: > 0 })
                return null;
            
            foreach (var release in artistAlbumsResponse.Releases.Where(static x => x.Score > 90))
            {
                var coverUri = await SendAndLogAsync(release.Id, cancellationToken);
                if (coverUri is not null)
                    return coverUri;
            }

            _logger.LogDebug("No album cover found for {Artist} - {Album}", artist, album);
            return null;
        });
    }

    private async Task<T?> SendAndDeserializeAsync<T>(string artist, string? album, string? track, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
        where T : class
    {
        var url = string.IsNullOrEmpty(album)
            ? $"https://musicbrainz.org/ws/2/recording/?query=artist:{artist}%20AND%20track:{track}&fmt=json"
            : $"https://musicbrainz.org/ws/2/release/?query=artist:{artist}%20AND%20release:{album}&fmt=json";
        
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to fetch {Url}: {StatusCode} - {Content}", url, response.StatusCode, responseContent);
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to fetch {Url}", url);
            return null;
        }
    }
    
    private async Task<Uri?> SendAndLogAsync(string releaseId, CancellationToken cancellationToken)
    {
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://coverartarchive.org/release/{releaseId}/front-250");
            using var httpResponseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return httpResponseMessage.IsSuccessStatusCode ? requestMessage.RequestUri : null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch album cover for {ReleaseId}", releaseId);
            return null;
        }
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

