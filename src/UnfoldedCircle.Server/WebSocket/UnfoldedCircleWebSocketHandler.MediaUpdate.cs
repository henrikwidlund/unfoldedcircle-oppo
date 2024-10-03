using System.Collections.Concurrent;

using OppoTelnet;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Server.Oppo;
using UnfoldedCircle.Server.Response;

namespace UnfoldedCircle.Server.WebSocket;

internal partial class UnfoldedCircleWebSocketHandler
{
    private static readonly ConcurrentDictionary<string, bool> BroadcastingMediaEvents = new(StringComparer.Ordinal);
    
    private async Task HandleMediaUpdates(
        System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        CancellationTokenWrapper cancellationTokenWrapper)
    {
        if (SetupInProgressMap.TryGetValue(wsId, out var setupInProgress) && setupInProgress)
        {
            _logger.LogDebug("{WSId} Setup in progress, skipping media updates", wsId);
            return;
        }
        
        if (!await oppoClientHolder.Client.IsConnectedAsync() || oppoClientHolder is { ClientKey.UseMediaEvents: false })
        {
            _logger.LogDebug("{WSId} Client not connected or configured to not use media events. {@ClientKey}", wsId, oppoClientHolder.ClientKey);
            return;
        }
        
        var cancellationTokenSource = cancellationTokenWrapper.GetCurrentBroadcastCancellationTokenSource();
        if (cancellationTokenSource is null || cancellationTokenSource.IsCancellationRequested)
        {
            _logger.LogDebug("{WSId} Broadcast token is cancelled {IsCancellationRequested}", wsId, cancellationTokenSource?.IsCancellationRequested);
            return;
        }
        
        if (BroadcastingMediaEvents.TryGetValue(wsId, out var broadcastingMediaEvents) && broadcastingMediaEvents)
        {
            _logger.LogDebug("{WSId} Media updates already running for {DeviceId}", wsId, oppoClientHolder.Client.GetHost());
            return;
        }
        
        BroadcastingMediaEvents.AddOrUpdate(wsId, true, static (_, _) => true);
        
        _logger.LogDebug("{WSId} Starting media updates for {DeviceId}", wsId, oppoClientHolder.Client.GetHost());
        using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token))
            {
                var powerStatusResponse = await oppoClientHolder.Client.QueryPowerStatusAsync(cancellationTokenWrapper.ApplicationStopping);
                var state = powerStatusResponse switch
                {
                    { Result: PowerState.On } => State.Playing,
                    { Result: PowerState.Off } => State.Off,
                    _ => State.Unknown
                };
                
                OppoResult<DiscType>? discTypeResponse = null;
                OppoResult<uint>? elapsedResponse = null;
                OppoResult<uint>? remainingResponse = null;
                OppoResult<string>? trackResponse = null;
                string? album = null;
                string? performer = null;
                Uri? coverUri = null;
                bool? shuffle = null;
                Models.Shared.RepeatMode? repeatMode = null;

                if (powerStatusResponse is { Result: PowerState.On })
                {
                    var playbackStatusResponse = await oppoClientHolder.Client.QueryPlaybackStatusAsync(cancellationTokenWrapper.ApplicationStopping);
                    state = playbackStatusResponse switch
                    {
                        { Result: PlaybackStatus.Unknown } => State.Unknown,
                        { Result: PlaybackStatus.Play } => State.Playing,
                        { Result: PlaybackStatus.Pause } => State.Paused,
                        { Result: PlaybackStatus.FastForward or PlaybackStatus.FastRewind or PlaybackStatus.SlowForward or PlaybackStatus.SlowRewind} => State.Buffering,
                        _ => State.Unknown
                    };

                    if (playbackStatusResponse is { Result: PlaybackStatus.Play or PlaybackStatus.Pause })
                    {
                        discTypeResponse = await oppoClientHolder.Client.QueryDiscTypeAsync(cancellationTokenWrapper.ApplicationStopping);

                        if (discTypeResponse.Value && discTypeResponse.Value.Result is not DiscType.UnknownDisc and not DiscType.DataDisc)
                        {
                            (repeatMode, shuffle) = GetRepeatMode(await oppoClientHolder.Client.QueryRepeatModeAsync(cancellationTokenWrapper.ApplicationStopping));
                            
                            if (discTypeResponse.Value.Result is DiscType.BlueRayMovie or DiscType.DVDVideo or DiscType.UltraHDBluRay)
                            {
                                elapsedResponse = oppoClientHolder.ClientKey.UseChapterLengthForMovies
                                    ? await oppoClientHolder.Client.QueryChapterElapsedTimeAsync(cancellationTokenWrapper.ApplicationStopping)
                                    : await oppoClientHolder.Client.QueryTotalElapsedTimeAsync(cancellationTokenWrapper.ApplicationStopping);
                                if (elapsedResponse.Value)
                                    remainingResponse = oppoClientHolder.ClientKey.UseChapterLengthForMovies
                                        ? await oppoClientHolder.Client.QueryChapterRemainingTimeAsync(cancellationTokenWrapper.ApplicationStopping)
                                        : await oppoClientHolder.Client.QueryTotalRemainingTimeAsync(cancellationTokenWrapper.ApplicationStopping);
                            }
                            else
                            {
                                elapsedResponse = await oppoClientHolder.Client.QueryTrackOrTitleElapsedTimeAsync(cancellationTokenWrapper.ApplicationStopping);
                                if (elapsedResponse.Value)
                                {
                                    remainingResponse = await oppoClientHolder.Client.QueryTrackOrTitleRemainingTimeAsync(cancellationTokenWrapper.ApplicationStopping);

                                    if (oppoClientHolder.ClientKey.Model is OppoModel.UDP20X)
                                    {
                                        trackResponse = await oppoClientHolder.Client.QueryTrackNameAsync(cancellationTokenWrapper.ApplicationStopping);
                                        album = (await oppoClientHolder.Client.QueryTrackAlbumAsync(cancellationTokenWrapper.ApplicationStopping)).Result;
                                        performer = (await oppoClientHolder.Client.QueryTrackPerformerAsync(cancellationTokenWrapper.ApplicationStopping)).Result;                                        
                                    }
                                }
                            }
                            
                            if (!string.IsNullOrWhiteSpace(performer) && (!string.IsNullOrWhiteSpace(album) || !string.IsNullOrWhiteSpace(trackResponse?.Result)))
                            {
                                if (album?.StartsWith(performer, StringComparison.OrdinalIgnoreCase) is true && album.AsSpan()[performer.Length..].StartsWith("   ", StringComparison.Ordinal))
                                    album = album.AsSpan()[(performer.Length + 3)..].ToString();

                                coverUri = await _albumCoverService.GetAlbumCoverAsync(performer, album, null,
                                    cancellationTokenWrapper.ApplicationStopping);
                            }
                            else
                                coverUri = null;   
                        }
                    }
                }

                await SendAsync(socket,
                    JsonSerializer.SerializeToUtf8Bytes(new StateChangedEvent
                    {
                        Kind = "event",
                        Msg = "entity_change",
                        Cat = "ENTITY",
                        TimeStamp = DateTime.UtcNow,
                        MsgData = new StateChangedEventMessageData
                        {
                            EntityId = OppoConstants.EntityId,
                            EntityType = EntityType.MediaPlayer,
                            Attributes = new StateChangedEventMessageDataAttributes
                            {
                                State = state,
                                MediaType = discTypeResponse?.Result switch
                                {
                                    DiscType.BlueRayMovie => MediaType.Movie,
                                    DiscType.DVDVideo => MediaType.Movie,
                                    DiscType.DVDAudio => MediaType.Music,
                                    DiscType.SACD => MediaType.Music,
                                    DiscType.CDDiscAudio => MediaType.Music,
                                    DiscType.UltraHDBluRay => MediaType.Movie,
                                    _ => null
                                },
                                MediaPosition = elapsedResponse?.Result,
                                MediaDuration = elapsedResponse?.Result + remainingResponse?.Result,
                                MediaTitle = ReplaceStarWithEllipsis(trackResponse?.Result),
                                MediaAlbum = ReplaceStarWithEllipsis(album),
                                MediaArtist = ReplaceStarWithEllipsis(performer),
                                MediaImageUrl = coverUri,
                                Repeat = repeatMode,
                                Shuffle = shuffle
                            }
                        }
                    }, _unfoldedCircleJsonSerializerContext.StateChangedEvent),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
            }
        }
        finally
        {
            await SendAsync(socket,
                ResponsePayloadHelpers.CreateStateChangedResponsePayload(_unfoldedCircleJsonSerializerContext),
                wsId,
                cancellationTokenWrapper.ApplicationStopping);
            
            BroadcastingMediaEvents.TryRemove(wsId, out _);
        }
        
        _logger.LogDebug("{WSId} Stopping media updates for {DeviceId}", wsId, oppoClientHolder.Client.GetHost());
        return;

        static string? ReplaceStarWithEllipsis(string? input) =>
            string.IsNullOrWhiteSpace(input) ? input : input.Replace('*', '…');
    }
    
    private static (Models.Shared.RepeatMode? RepeatMode, bool? shuffle) GetRepeatMode(OppoResult<CurrentRepeatMode> repeatModeResponse) =>
        !repeatModeResponse
            ? (null, null)
            : repeatModeResponse.Result switch
            {
                CurrentRepeatMode.Off => (Models.Shared.RepeatMode.Off, false),
                CurrentRepeatMode.RepeatOne => (Models.Shared.RepeatMode.One, false),
                CurrentRepeatMode.RepeatChapter => (Models.Shared.RepeatMode.One, false),
                CurrentRepeatMode.RepeatAll => (Models.Shared.RepeatMode.All, false),
                CurrentRepeatMode.RepeatTitle => (Models.Shared.RepeatMode.One, false),
                CurrentRepeatMode.Shuffle => (Models.Shared.RepeatMode.Off, true),
                CurrentRepeatMode.Random => (Models.Shared.RepeatMode.Off, true),
                CurrentRepeatMode.Unknown => (Models.Shared.RepeatMode.Off, false),
                _ => (Models.Shared.RepeatMode.Off, false)
            };
}