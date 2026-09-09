using System.Collections.Concurrent;
using System.Collections.Frozen;

using Oppo;

namespace UnfoldedCircle.OppoBluRay.AlbumCover;

internal static class DefaultArtwork
{
    private static readonly FrozenDictionary<DiscType, string> IconFileNames = new Dictionary<DiscType, string>
    {
        [DiscType.BlueRayMovie] = "bdmovie.png",
        [DiscType.DVDVideo] = "dvdvideo.png",
        [DiscType.UltraHDBluRay] = "uhd.png",
        [DiscType.DVDAudio] = "dvdaudio.png",
        [DiscType.SACD] = "sacd.png",
        [DiscType.CDDiscAudio] = "cdda.png",
        [DiscType.HDCD] = "hdcd.png",
        [DiscType.VCD2] = "vcd.png",
        [DiscType.SVCD] = "svcd.png"
    }.ToFrozenDictionary();

    private const string MagnetarBrandIconFileName = "magnetar.png";
    private const string OppoBrandIconFileName = "oppo.png";

    private static readonly ConcurrentDictionary<DiscType, Uri?> CachedUris = new();
    private static readonly ConcurrentDictionary<OppoModel, Uri> BrandCachedUris = new();

    public static Uri? GetIconUri(DiscType discType) =>
        CachedUris.GetOrAdd(discType, static dt => IconFileNames.TryGetValue(dt, out var fileName) ? LoadUri(fileName) : null);

    public static Uri GetBrandIconUri(OppoModel model) =>
        BrandCachedUris.GetOrAdd(model, static m => LoadUri(m == OppoModel.Magnetar ? MagnetarBrandIconFileName : OppoBrandIconFileName));

    private static Uri LoadUri(string fileName)
    {
        using var stream = typeof(DefaultArtwork).Assembly.GetManifestResourceStream($"Icons/{fileName}")
            ?? throw new InvalidOperationException($"Missing embedded icon resource 'Icons/{fileName}'.");
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);

        return new Uri($"data:image/png;base64,{Convert.ToBase64String(memoryStream.ToArray())}");
    }
}
