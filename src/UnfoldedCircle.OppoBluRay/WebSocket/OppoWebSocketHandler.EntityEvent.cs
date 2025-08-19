using System.Collections.Concurrent;

using Oppo;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.OppoBluRay.OppoEntity;
using UnfoldedCircle.Server.Response;
using UnfoldedCircle.Server.WebSocket;

namespace UnfoldedCircle.OppoBluRay.WebSocket;

public partial class OppoWebSocketHandler
{
    private readonly SemaphoreSlim _broadcastSemaphoreSlim = new(1, 1);
    private static readonly ConcurrentDictionary<int, int> PreviousMediaStatesMap = new();
    private static readonly ConcurrentDictionary<int, State> PreviousRemoteStatesMap = new();

    protected override async Task HandleEventUpdatesAsync(System.Net.WebSockets.WebSocket socket, string entityId, string wsId, CancellationTokenWrapper cancellationTokenWrapper)
    {
        if (!IsSocketSubscribedToEvents(wsId))
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

        if (await _broadcastSemaphoreSlim.WaitAsync(TimeSpan.FromSeconds(1), cancellationTokenSource.Token))
        {
            try
            {
                if (IsBroadcastingEvents(entityId) || !TryAddEntityIdToBroadcastingEvents(entityId))
                {
                    _logger.LogDebug("{WSId} Events already running for {EntityId}", wsId, entityId);
                    return;
                }
            }
            finally
            {
                _broadcastSemaphoreSlim.Release();
            }
        }
        else
        {
            _logger.LogError("{WSId} Could not acquire semaphore for broadcasting events for {EntityId}. Will not start broadcasting.",
                wsId, entityId);
            return;
        }

        using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        var oppoClientHolder = await TryGetOppoClientHolderAsync(wsId, entityId, IdentifierType.EntityId, cancellationTokenWrapper.RequestAborted);
        if (oppoClientHolder is null)
        {
            _logger.LogWarning("[{WSId}] WS: Could not find Oppo client for entity ID '{EntityId}'", wsId, entityId);
            return;
        }

        _logger.LogDebug("{WSId} Trying to get OppoClientHolder.", wsId);
        while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token))
        {
            if (!IsBroadcastingEvents(entityId))
            {
                _logger.LogDebug("{WSId} No longer subscribed to events for {EntityId}. Stopping event updates.", wsId, entityId);
                return;
            }

            if (await oppoClientHolder.Client.IsConnectedAsync())
                break;
        }

        _logger.LogDebug("{WSId} Starting events for {DeviceId}", wsId, oppoClientHolder.Client.GetHost());
        try
        {
            while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token))
            {
                if (!IsBroadcastingEvents(entityId))
                {
                    _logger.LogDebug("{WSId} No longer subscribed to events for {EntityId}. Stopping event updates.", wsId, entityId);
                    return;
                }

                var connected = await oppoClientHolder.Client.IsConnectedAsync();
                if (!connected)
                    _logger.LogDebug("{WSId} Client not connected. {@ClientKey}", wsId, oppoClientHolder.ClientKey);

                OppoResult<PowerState>? powerStatusResponse;
                if (connected)
                    powerStatusResponse = await oppoClientHolder.Client.QueryPowerStatusAsync(cancellationTokenSource.Token);
                else
                    powerStatusResponse = null;

                var state = powerStatusResponse switch
                {
                    { Result: PowerState.On } => State.On,
                    { Result: PowerState.Off } => State.Off,
                    _ => State.Unknown
                };

                MediaPlayerStateChangedEventMessageDataAttributes newMediaPlayerState;
                // Only send power state if not using media events
                if (oppoClientHolder is { ClientKey.UseMediaEvents: false })
                {
                    newMediaPlayerState = new MediaPlayerStateChangedEventMessageDataAttributes { State = state };
                    if (!await SendMediaPlayerEventAsync(socket, wsId, oppoClientHolder, newMediaPlayerState, cancellationTokenSource.Token))
                        continue;

                    await SendRemotePowerEventAsync(socket, wsId, oppoClientHolder, state, cancellationTokenSource.Token);

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
                    volumeResponse = await oppoClientHolder.Client.QueryVolumeAsync(cancellationTokenSource.Token);
                    inputSourceResponse = await oppoClientHolder.Client.QueryInputSourceAsync(cancellationTokenSource.Token);

                    var playbackStatusResponse = await oppoClientHolder.Client.QueryPlaybackStatusAsync(cancellationTokenSource.Token);
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
                        discTypeResponse = await oppoClientHolder.Client.QueryDiscTypeAsync(cancellationTokenSource.Token);

                        if (discTypeResponse.Value && discTypeResponse.Value.Result is not DiscType.UnknownDisc and not DiscType.DataDisc)
                        {
                            (repeatMode, shuffle) = GetRepeatMode(await oppoClientHolder.Client.QueryRepeatModeAsync(cancellationTokenSource.Token));

                            if (discTypeResponse.Value.Result is DiscType.BlueRayMovie or DiscType.DVDVideo or DiscType.UltraHDBluRay)
                            {
                                elapsedResponse = oppoClientHolder.ClientKey.UseChapterLengthForMovies
                                    ? await oppoClientHolder.Client.QueryChapterElapsedTimeAsync(cancellationTokenSource.Token)
                                    : await oppoClientHolder.Client.QueryTotalElapsedTimeAsync(cancellationTokenSource.Token);
                                if (elapsedResponse.Value)
                                    remainingResponse = oppoClientHolder.ClientKey.UseChapterLengthForMovies
                                        ? await oppoClientHolder.Client.QueryChapterRemainingTimeAsync(cancellationTokenSource.Token)
                                        : await oppoClientHolder.Client.QueryTotalRemainingTimeAsync(cancellationTokenSource.Token);
                            }
                            else
                            {
                                elapsedResponse = await oppoClientHolder.Client.QueryTrackOrTitleElapsedTimeAsync(cancellationTokenSource.Token);
                                if (elapsedResponse.Value)
                                {
                                    remainingResponse = await oppoClientHolder.Client.QueryTrackOrTitleRemainingTimeAsync(cancellationTokenSource.Token);

                                    if (oppoClientHolder.ClientKey.Model is OppoModel.UDP203 or OppoModel.UDP205)
                                    {
                                        trackResponse = await oppoClientHolder.Client.QueryTrackNameAsync(cancellationTokenSource.Token);
                                        album = (await oppoClientHolder.Client.QueryTrackAlbumAsync(cancellationTokenSource.Token)).Result;
                                        performer = (await oppoClientHolder.Client.QueryTrackPerformerAsync(cancellationTokenSource.Token)).Result;
                                    }
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(performer) && (!string.IsNullOrWhiteSpace(album) || !string.IsNullOrWhiteSpace(trackResponse?.Result)))
                            {
                                if (album?.StartsWith(performer, StringComparison.OrdinalIgnoreCase) is true &&
                                    album.AsSpan()[performer.Length..].StartsWith("   ", StringComparison.Ordinal))
                                    album = album.AsSpan()[(performer.Length + 3)..].ToString();

                                coverUri = await _albumCoverService.GetAlbumCoverAsync(performer, album, null, cancellationTokenSource.Token);
                            }
                            else
                                coverUri = null;
                        }
                    }
                }

                newMediaPlayerState = new MediaPlayerStateChangedEventMessageDataAttributes
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
                };

                if (!await SendMediaPlayerEventAsync(socket, wsId, oppoClientHolder, newMediaPlayerState, cancellationTokenSource.Token))
                    continue;

                await SendRemotePowerEventAsync(socket, wsId, oppoClientHolder, state, cancellationTokenSource.Token);
            }
        }
        finally
        {
            bool acquiredLock = await _broadcastSemaphoreSlim.WaitAsync(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
            try
            {
                RemoveEntityIdToBroadcastingEvents(entityId);
            }
            finally
            {
                if (acquiredLock)
                    _broadcastSemaphoreSlim.Release();
            }
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

    private async Task<bool> SendMediaPlayerEventAsync(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        MediaPlayerStateChangedEventMessageDataAttributes mediaPlayerState,
        CancellationToken cancellationToken)
    {
        var stateHash = mediaPlayerState.GetHashCode();
        int clientHashCode = oppoClientHolder.ClientKey.GetHashCode();
        if (PreviousMediaStatesMap.TryGetValue(clientHashCode, out var previousStateHash) &&
            previousStateHash == stateHash)
            return false;

        PreviousMediaStatesMap[clientHashCode] = stateHash;
        await SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                mediaPlayerState,
                oppoClientHolder.ClientKey.EntityId,
                EntityType.MediaPlayer),
            wsId,
            cancellationToken);
        return true;
    }

    private async Task SendRemotePowerEventAsync(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        State state,
        CancellationToken cancellationToken)
    {
        int clientHashCode = oppoClientHolder.ClientKey.GetHashCode();
        if (PreviousRemoteStatesMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == state)
            return;

        PreviousRemoteStatesMap[clientHashCode] = state;
        await SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateStateChangedResponsePayload(
                new RemoteStateChangedEventMessageDataAttributes { State = state switch
                {
                    State.Buffering or State.Playing or State.Paused or State.On => RemoteState.On,
                    State.Off => RemoteState.Off,
                    _ => RemoteState.Unknown
                } },
                oppoClientHolder.ClientKey.EntityId,
                EntityType.Remote),
            wsId,
            cancellationToken);
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
                CurrentRepeatMode.Shuffle or CurrentRepeatMode.Random => (Models.Shared.RepeatMode.Off, true),
                _ => (Models.Shared.RepeatMode.Off, false)
            };

    private static List<uint> GetDigits(uint number)
    {
        var digits = new List<uint>();
        while (number > 0)
        {
            digits.Add(number % 10);
            number /= 10;
        }
        digits.Reverse();
        return digits;
    }
}