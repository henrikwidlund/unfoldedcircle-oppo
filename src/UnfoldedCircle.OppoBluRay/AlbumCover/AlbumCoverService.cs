using System.Text;
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
            ? $"https://musicbrainz.org/ws/2/recording/?query={Uri.EscapeDataString($"artist:{ToLuceneTerm(artist)} AND recording:{ToLuceneTerm(track!)}")}&fmt=json"
            : $"https://musicbrainz.org/ws/2/release/?query={Uri.EscapeDataString($"artist:{ToLuceneTerm(artist)} AND release:{ToLuceneTerm(album)}")}&fmt=json";

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

    // Lucene special characters that must be escaped so they are treated literally rather than as
    // query operators. '*' is intentionally excluded so it can act as a prefix wildcard (see below).
    private const string LuceneSpecials = @"+-&|!(){}[]^""~?:\/";

    private static string EscapeLucene(ReadOnlySpan<char> value)
    {
        var sb = new StringBuilder(value.Length + 8);
        foreach (var c in value)
        {
            if (LuceneSpecials.Contains(c, StringComparison.Ordinal))
                sb.Append('\\');
            sb.Append(c);
        }
        return sb.ToString();
    }

    // Build a Lucene term. The Oppo player truncates long fields with a trailing '*'. Wildcards only
    // work on bare terms, not inside a phrase query, so a truncated value must not be fully quoted.
    // For multi-word truncated values we quote all but the last word so Lucene treats the leading
    // words as a contiguous required unit: +"NORTHERN" LIGHTS*
    private static string ToLuceneTerm(string value)
    {
        if (!value.EndsWith('*'))
            return $"\"{EscapeLucene(value)}\"";

        var core = value.AsSpan(0, value.Length - 1).TrimEnd();

        var lastWs = -1;
        for (var i = core.Length - 1; i >= 0; i--)
        {
            if (char.IsWhiteSpace(core[i]))
            {
                lastWs = i;
                break;
            }
        }

        if (lastWs < 0)
            return $"{EscapeLucene(core)}*";

        var prefix = EscapeLucene(core[..lastWs].TrimEnd());
        var tail = EscapeLucene(core[(lastWs + 1)..]);
        return $"+\"{prefix}\" {tail}*";
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
