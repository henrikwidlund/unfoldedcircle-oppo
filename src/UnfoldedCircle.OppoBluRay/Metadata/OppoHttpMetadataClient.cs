using UnfoldedCircle.OppoBluRay.Json;
using UnfoldedCircle.OppoBluRay.Logging;

namespace UnfoldedCircle.OppoBluRay.Metadata;

/// <summary>
/// Client for the pre-20X players' proprietary HTTP JSON control API. Reverse-engineered from the
/// decompiled "OPPO BDP-10x MediaControl" Android app: BDP-83/93/95/103/105 all run a small HTTP JSON
/// API on TCP port 436, entirely separate from the RS-232-over-IP text protocol <see cref="Oppo.OppoClient"/>
/// speaks. <c>GET /getmusicplayinfo</c> returns an <c>id3info</c> object (title/album/artist) for
/// whatever is currently playing - fills the same hole UDP-20X already covers via its own telnet
/// QTN/QTA/QTP queries. Firmware support for this endpoint is only confirmed on BDP-103/105, so a
/// player that doesn't implement it must fail silently (timeout/connection-refused/malformed JSON all
/// just return null) rather than break the snapshot rebuild.
/// </summary>
public sealed class OppoHttpMetadataClient(HttpClient httpClient, ILogger<OppoHttpMetadataClient> logger)
{
    private const int Port = 436;
    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<OppoHttpMetadataClient> _logger = logger;

    public async ValueTask<OppoHttpMusicInfo?> GetMusicPlayInfoAsync(string host, CancellationToken cancellationToken = default)
    {
        var url = $"http://{host}:{Port}/getmusicplayinfo";
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var payload = await response.Content.ReadFromJsonAsync(OppoJsonSerializerContext.Instance.GetMusicPlayInfoResponse, cancellationToken);
            return payload?.Id3Info is null ? null : MapMusicInfo(payload);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception e)
        {
            _logger.FailedToFetchUrlException(url, e);
            return null;
        }
    }

    private static OppoHttpMusicInfo MapMusicInfo(GetMusicPlayInfoResponse payload)
    {
        var id3 = payload.Id3Info!;
        return new OppoHttpMusicInfo(
            Title: NullIfEmpty(id3.Title) ?? NullIfEmpty(payload.PlayInfo?.DlnaFilename) ?? FileNameFromPath(payload.PlayInfo?.FilePath),
            Album: NullIfEmpty(id3.Album),
            Artist: NullIfEmpty(id3.Artist));
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    // Untagged local files have no id3info.title - the official app falls back to the filename in that
    // case (getFileNameFromPath: everything after the last '/'), so this does the same rather than
    // showing nothing.
    private static string? FileNameFromPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        var span = path.AsSpan();
        var lastSlash = span.LastIndexOf('/');
        var fileName = lastSlash < 0 ? span : span[(lastSlash + 1)..];
        return fileName.IsEmpty ? null : fileName.ToString();
    }
}

public sealed record OppoHttpMusicInfo(string? Title, string? Album, string? Artist);

internal sealed record GetMusicPlayInfoResponse(
    [property: JsonPropertyName("id3info")] Id3Info? Id3Info,
    [property: JsonPropertyName("playinfo")] PlayInfo? PlayInfo);

internal sealed record Id3Info(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("album")] string? Album,
    [property: JsonPropertyName("artist")] string? Artist);

internal sealed record PlayInfo(
    [property: JsonPropertyName("dlna_filename")] string? DlnaFilename,
    [property: JsonPropertyName("file_path")] string? FilePath);
