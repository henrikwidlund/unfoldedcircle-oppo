using Oppo;

using UnfoldedCircle.Server.AlbumCover;
using UnfoldedCircle.Server.Configuration;
using UnfoldedCircle.Server.Dns;
using UnfoldedCircle.Server.Json;

using UnfoldedCircle.Server.WebSocket;

var builder = WebApplication.CreateSlimBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(builder.Configuration.GetOrDefault("UC_INTEGRATION_HTTP_PORT", 9001));
});

builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
builder.Services.AddSingleton<IOppoClientFactory, OppoClientFactory>();
builder.Services.AddHttpClient<IAlbumCoverService, AlbumCoverService>(static client =>
{
    client.DefaultRequestHeaders.UserAgent.Clear();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("UnfoldedCircle/1.0");
    client.Timeout = TimeSpan.FromSeconds(7);
});
builder.Services.AddMemoryCache();
builder.Services.ConfigureHttpJsonOptions(static options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, UnfoldedCircleJsonSerializerContext.Instance);
});

builder.Services.AddHostedService<MDnsBackgroundService>();

builder.Services.AddSingleton<UnfoldedCircleMiddleware>();
builder.Services.AddSingleton<UnfoldedCircleWebSocketHandler>();

var app = builder.Build();

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};

app.UseWebSockets(webSocketOptions);
app.UseMiddleware<UnfoldedCircleMiddleware>();

await app.RunAsync();
