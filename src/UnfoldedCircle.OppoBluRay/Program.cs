using Oppo;

using UnfoldedCircle.OppoBluRay.AlbumCover;
using UnfoldedCircle.OppoBluRay.Configuration;
using UnfoldedCircle.OppoBluRay.Discovery;
using UnfoldedCircle.OppoBluRay.Metadata;
using UnfoldedCircle.OppoBluRay.OppoEntity;
using UnfoldedCircle.OppoBluRay.WebSocket;

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddUnfoldedCircleServer<OppoWebSocketHandler, OppoCommandId, OppoConfigurationService, OppoGlobalConfiguration, OppoConfigurationItem>();
builder.Services.AddSingleton<IOppoClientFactory, OppoClientFactory>();
builder.Services.AddHttpClient<IAlbumCoverService, AlbumCoverService>(static client =>
{
    client.DefaultRequestHeaders.UserAgent.Clear();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("UnfoldedCircle/1.0");
    client.Timeout = TimeSpan.FromSeconds(7);
});
builder.Services.AddHttpClient<OppoHttpMetadataClient>(static client =>
    client.Timeout = TimeSpan.FromSeconds(3));
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IPlayerDiscovery, SsdpDiscovery>();
builder.Services.AddSingleton<IPlayerDiscovery, OppoUdpDiscovery>();
builder.Services.AddSingleton<IPlayerDiscovery, MagnetarDiscovery>();
builder.Services.AddSingleton<DiscoveryService>();

var app = builder.Build();
app.UseUnfoldedCircleServer<OppoWebSocketHandler, OppoCommandId, OppoGlobalConfiguration, OppoConfigurationItem>();

await app.RunAsync();
