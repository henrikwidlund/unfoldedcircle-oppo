using System.Collections.Concurrent;

using Oppo;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Server.Json;
using UnfoldedCircle.Server.Oppo;
using UnfoldedCircle.Server.Response;

namespace UnfoldedCircle.Server.WebSocket;

internal sealed partial class UnfoldedCircleWebSocketHandler
{
    private static readonly ConcurrentDictionary<string, bool> BroadcastingEvents = new(StringComparer.Ordinal);
    
    private async Task HandleEventUpdates(
        System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        CancellationTokenWrapper cancellationTokenWrapper)
    {
        if (SubscribeEvents.TryGetValue(wsId, out var subscribeEvents) && !subscribeEvents)
        {
            _logger.LogDebug("{WSId} Subscribe events not called", wsId);
            return;
        }
        
        var cancellationTokenSource = cancellationTokenWrapper.GetCurrentBroadcastCancellationTokenSource();
        if (cancellationTokenSource is null || cancellationTokenSource.IsCancellationRequested)
        {
            _logger.LogDebug("{WSId} Broadcast token is cancelled {IsCancellationRequested}", wsId, cancellationTokenSource?.IsCancellationRequested);
            return;
        }
        
        if (BroadcastingEvents.TryGetValue(wsId, out var broadcastingEvents) && broadcastingEvents)
        {
            _logger.LogDebug("{WSId} Media updates already running for {DeviceId}", wsId, oppoClientHolder.Client.GetHost());
            return;
        }
        
        BroadcastingEvents.AddOrUpdate(wsId, true, static (_, _) => true);
        
        using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        
        _logger.LogDebug("{WSId} Trying to get OppoClientHolder.", wsId);
        while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token))
        {
            if (await oppoClientHolder.Client.IsConnectedAsync())
                break;
        }
        
        _logger.LogDebug("{WSId} Starting events for {DeviceId}", wsId, oppoClientHolder.Client.GetHost());
        try
        {
            while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token))
            {
                var connected = await oppoClientHolder.Client.IsConnectedAsync();
                if (!connected)
                    _logger.LogDebug("{WSId} Client not connected. {@ClientKey}", wsId, oppoClientHolder.ClientKey);
                
                OppoResult<PowerState>? powerStatusResponse;
                if (connected)
                    powerStatusResponse = await oppoClientHolder.Client.QueryPowerStatusAsync(cancellationTokenWrapper.ApplicationStopping);
                else
                    powerStatusResponse = null;
                
                var state = powerStatusResponse switch
                {
                    { Result: PowerState.On } => State.On,
                    { Result: PowerState.Off } => State.Off,
                    _ => State.Unknown
                };
                
                // Only send power state if not using media events
                if (oppoClientHolder is { ClientKey.UseMediaEvents: false })
                {
                    await SendAsync(socket,
                        ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                            new StateChangedEventMessageDataAttributes { State = state },
                            oppoClientHolder.ClientKey.EntityId),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping);
                    
                    continue;
                }

                OppoResult<VolumeInfo>? volumeResponse = null;
                OppoResult<InputSource>? inputSourceResponse = null;
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
                    volumeResponse = await oppoClientHolder.Client.QueryVolumeAsync(cancellationTokenWrapper.ApplicationStopping);
                    inputSourceResponse = await oppoClientHolder.Client.QueryInputSourceAsync(cancellationTokenWrapper.ApplicationStopping);
                    
                    var playbackStatusResponse = await oppoClientHolder.Client.QueryPlaybackStatusAsync(cancellationTokenWrapper.ApplicationStopping);
                    state = playbackStatusResponse switch
                    {
                        { Result: PlaybackStatus.Unknown } => State.Unknown,
                        { Result: PlaybackStatus.Play } => State.Playing,
                        { Result: PlaybackStatus.Pause } => State.Paused,
                        { Result: PlaybackStatus.FastForward or PlaybackStatus.FastRewind or PlaybackStatus.SlowForward or PlaybackStatus.SlowRewind} => State.Buffering,
                        _ => State.On
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

                                    if (oppoClientHolder.ClientKey.Model is OppoModel.UDP203 or OppoModel.UDP205)
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
                            EntityId = oppoClientHolder.ClientKey.EntityId,
                            EntityType = EntityType.MediaPlayer,
                            Attributes = new StateChangedEventMessageDataAttributes
                            {
                                State = state,
                                MediaType = discTypeResponse?.Result switch
                                {
                                    DiscType.BlueRayMovie or DiscType.DVDVideo or DiscType.UltraHDBluRay => MediaType.Movie,
                                    DiscType.DVDAudio or DiscType.SACD or DiscType.CDDiscAudio => MediaType.Music,
                                    _ => null
                                },
                                MediaPosition = elapsedResponse?.Result,
                                MediaDuration = elapsedResponse?.Result + remainingResponse?.Result,
                                MediaTitle = ReplaceStarWithEllipsis(trackResponse?.Result),
                                MediaAlbum = ReplaceStarWithEllipsis(album),
                                MediaArtist = ReplaceStarWithEllipsis(performer),
                                MediaImageUrl = coverUri,
                                Repeat = repeatMode,
                                Shuffle = shuffle,
                                Source = GetInputSource(inputSourceResponse),
                                Volume = volumeResponse?.Result.Volume,
                                Muted = volumeResponse?.Result.Muted
                            }
                        }
                    }, UnfoldedCircleJsonSerializerContext.Instance.StateChangedEvent),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
            }
        }
        finally
        {
            BroadcastingEvents.TryRemove(wsId, out _);
        }
        
        _logger.LogDebug("{WSId} Stopping media updates for {DeviceId}", wsId, oppoClientHolder.Client.GetHost());
        return;

        static string? ReplaceStarWithEllipsis(string? input) =>
            string.IsNullOrWhiteSpace(input) ? input : input.Replace('*', '…');

        static string? GetInputSource(in OppoResult<InputSource>? inputSourceResponse)
        {
            if (inputSourceResponse is not { Success: true })
                return null;

            return inputSourceResponse.Value.Result switch
            {
                InputSource.Unknown => null,
                InputSource.BluRayPlayer => OppoConstants.InputSource.BluRayPlayer,
                InputSource.HDMIIn => OppoConstants.InputSource.HDMIIn,
                InputSource.ARCHDMIOut => OppoConstants.InputSource.ARCHDMIOut,
                InputSource.Optical => OppoConstants.InputSource.Optical,
                InputSource.Coaxial => OppoConstants.InputSource.Coaxial,
                InputSource.USBAudio => OppoConstants.InputSource.USBAudio,
                InputSource.HDMIFront => OppoConstants.InputSource.HDMIFront,
                InputSource.HDMIBack => OppoConstants.InputSource.HDMIBack,
                InputSource.ARCHDMIOut1 => OppoConstants.InputSource.ARCHDMIOut1,
                InputSource.ARCHDMIOut2 => OppoConstants.InputSource.ARCHDMIOut2,
                _ => null
            };
        }
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