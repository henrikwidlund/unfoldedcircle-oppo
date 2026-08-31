using Oppo;

using UnfoldedCircle.OppoBluRay.AlbumCover;
using UnfoldedCircle.OppoBluRay.Configuration;
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
builder.Services.AddMemoryCache();

var app = builder.Build();
app.UseUnfoldedCircleServer<OppoWebSocketHandler, OppoCommandId, OppoGlobalConfiguration, OppoConfigurationItem>();

await app.RunAsync();
