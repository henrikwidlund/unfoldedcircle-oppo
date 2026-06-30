using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.Caching.Memory;

using UnfoldedCircle.OppoBluRay.Json;
using UnfoldedCircle.OppoBluRay.Logging;

namespace UnfoldedCircle.OppoBluRay.AlbumCover;

public interface IAlbumCoverService
{
    ValueTask<Uri?> GetAlbumCoverAsync(string artist, string? album, string? track, CancellationToken cancellationToken = default);
}

internal sealed class AlbumCoverService(
    HttpClient httpClient,
    IMemoryCache memoryCache,
    ILogger<AlbumCoverService> logger) : IAlbumCoverService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<AlbumCoverService> _logger = logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(3);

    public async ValueTask<Uri?> GetAlbumCoverAsync(string artist, string? album, string? track, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(album))
        {
            if (string.IsNullOrWhiteSpace(track))
                return null;

            return await _memoryCache.GetOrCreateAsync((artist, track), async entry =>
            {
                entry.SetSlidingExpiration(CacheDuration);

                var artistTrackResponse = await SendAndDeserializeAsync<ArtistTrackResponse>(artist, album, track,
                    OppoJsonSerializerContext.Instance.ArtistTrackResponse, cancellationToken);

                if (artistTrackResponse is not { Recordings.Length: > 0 })
                    return null;

                foreach (var release in artistTrackResponse.Recordings
                             .Where(static x => x.Score > 80)
                             .OrderByDescending(static x => x.Score)
                             .SelectMany(static x => x.Releases))
                {
                    var coverUri = await SendAndLogAsync(release.Id, cancellationToken);
                    if (coverUri is not null)
                        return coverUri;
                }

                _logger.NoAlbumCoverFound(artist, album);
                return null;
            });
        }

        return await _memoryCache.GetOrCreateAsync((artist, album), async entry =>
        {
            entry.SetSlidingExpiration(CacheDuration);

            var artistAlbumsResponse = await SendAndDeserializeAsync<ArtistAlbumsResponse>(artist, album, track,
                OppoJsonSerializerContext.Instance.ArtistAlbumsResponse, cancellationToken);

            if (artistAlbumsResponse is not { Releases.Length: > 0 })
                return null;

            foreach (var release in artistAlbumsResponse.Releases.Where(static x => x.Score > 80)
                         .OrderByDescending(static x => x.Score))
            {
                var coverUri = await SendAndLogAsync(release.Id, cancellationToken);
                if (coverUri is not null)
                    return coverUri;
            }

            _logger.NoAlbumCoverFound(artist, album);
            return null;
        });
    }

    private async Task<T?> SendAndDeserializeAsync<T>(string artist, string? album, string? track, JsonTypeInfo<T> jsonTypeInfo, CancellationToken cancellationToken)
        where T : class
    {
        var url = string.IsNullOrWhiteSpace(album)
            ? $"https://musicbrainz.org/ws/2/recording/?query={Uri.EscapeDataString($"artist:{ToLucenePhrase(artist)} AND recording:{ToLucenePhrase(track!)}")}&fmt=json"
            : $"https://musicbrainz.org/ws/2/release/?query={Uri.EscapeDataString($"artist:{ToLucenePhrase(artist)} AND release:{ToLucenePhrase(album)}")}&fmt=json";

        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync(jsonTypeInfo, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.FailedToFetchUrl(url, response.StatusCode, responseContent);
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            _logger.FailedToFetchUrlException(e, url);

            return null;
        }
    }

    // Wrap the term in a Lucene phrase ("...") so its contents are treated literally instead of as
    // query operators or field separators. Inside a phrase only \ and " are special, so escape those.
    private static string ToLucenePhrase(string value) =>
        $"\"{value.Replace("\\", @"\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

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
            _logger.FailedToFetchAlbumCover(ex, releaseId);

            return null;
        }
    }
}

internal sealed record ArtistAlbumsResponse(
    [property: JsonPropertyName("releases")]
    ArtistAlbumsReleases[] Releases
);

internal sealed record ArtistAlbumsReleases(
    [property: JsonPropertyName("id")]
    string Id,

    [property: JsonPropertyName("score")]
    int Score
);

internal sealed record ArtistTrackResponse(
    [property: JsonPropertyName("recordings")]
    Recordings[] Recordings
);

internal sealed record Recordings(
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName("score")]
    int Score,
    [property: JsonPropertyName("releases")]
    RecordingsRelease[] Releases
);

internal sealed record RecordingsRelease(
    [property: JsonPropertyName("id")]
    string Id
);
