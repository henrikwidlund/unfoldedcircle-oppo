using System.Collections.Concurrent;

using Oppo;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.OppoBluRay.Configuration;
using UnfoldedCircle.OppoBluRay.Logging;
using UnfoldedCircle.OppoBluRay.OppoEntity;
using UnfoldedCircle.Server.Response;
using UnfoldedCircle.Server.WebSocket;

namespace UnfoldedCircle.OppoBluRay.WebSocket;

public partial class OppoWebSocketHandler
{
    private readonly SemaphoreSlim _broadcastSemaphoreSlim = new(1, 1);
    private static readonly ConcurrentDictionary<int, int> PreviousMediaStatesMap = new();
    private static readonly ConcurrentDictionary<int, State> PreviousRemoteStatesMap = new();
    private static readonly ConcurrentDictionary<int, InputSource?> PreviousSensorInputSourcesMap = new();
    private static readonly ConcurrentDictionary<int, DiscType?> PreviousSensorDiscTypesMap = new();
    private static readonly ConcurrentDictionary<int, HDMIResolution?> PreviousSensorHDMIResolutionsMap = new();
    private static readonly ConcurrentDictionary<int, string?> PreviousSensorAudioTypesMap = new();
    private static readonly ConcurrentDictionary<int, string?> PreviousSensorSubtitleTypesMap = new();
    private static readonly ConcurrentDictionary<int, bool?> PreviousSensorThreeDsMap = new();
    private static readonly ConcurrentDictionary<int, HDRStatus?> PreviousSensorHDRStatusMap = new();
    private static readonly ConcurrentDictionary<int, AspectRatio?> PreviousSensorAspectRatiosMap = new();

    protected override async Task HandleEventUpdatesAsync(System.Net.WebSockets.WebSocket socket, string entityId, string wsId, CancellationTokenWrapper cancellationTokenWrapper)
    {
        if (!IsSocketSubscribedToEvents(wsId))
        {
            _logger.SubscribeEventsNotCalled(wsId);
            return;
        }

        var cancellationTokenSource = cancellationTokenWrapper.GetCurrentBroadcastCancellationTokenSource();
        if (cancellationTokenSource is null || cancellationTokenSource.IsCancellationRequested)
        {
            _logger.BroadcastTokenCancelled(wsId, cancellationTokenSource?.IsCancellationRequested);
            return;
        }

        if (await _broadcastSemaphoreSlim.WaitAsync(TimeSpan.FromSeconds(1), cancellationTokenSource.Token))
        {
            try
            {
                var entityIdSpan = entityId.AsSpan();
                if (IsBroadcastingEvents(entityIdSpan) || !TryAddEntityIdToBroadcastingEvents(entityIdSpan, cancellationTokenWrapper))
                {
                    _logger.EventsAlreadyRunning(wsId, entityId);
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
            _logger.CouldNotAcquireSemaphore(wsId, entityId);
            return;
        }

        using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        var oppoClientHolder = await TryGetOppoClientHolderAsync(wsId, entityId, IdentifierType.EntityId, cancellationTokenWrapper.RequestAborted);
        if (oppoClientHolder is null)
        {
            _logger.CouldNotFindOppoClientForEntityId(wsId, entityId.AsMemory());
            return;
        }

        _logger.TryingToGetOppoClientHolder(wsId);
        while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token))
        {
            if (!IsBroadcastingEvents(entityId))
            {
                _logger.NoLongerSubscribedToEvents(wsId, entityId);
                return;
            }

            if (await oppoClientHolder.Client.IsConnectedAsync())
                break;
        }

        _logger.StartingEventsForDevice(wsId, oppoClientHolder.Client.HostName);
        try
        {
            while (await periodicTimer.WaitForNextTickAsync(cancellationTokenSource.Token))
            {
                if (!IsBroadcastingEvents(entityId))
                {
                    _logger.NoLongerSubscribedToEvents(wsId, entityId);
                    return;
                }

                var connected = await oppoClientHolder.Client.IsConnectedAsync();
                if (!connected)
                    _logger.ClientNotConnected(wsId, oppoClientHolder.ClientKey);

                OppoResult<PowerState>? powerStatusResponse = connected
                    ? await oppoClientHolder.Client.QueryPowerStatusAsync(cancellationTokenSource.Token)
                    : null;

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
                    await SendSensorEventAsync(socket, wsId, oppoClientHolder,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        cancellationTokenSource.Token);

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
                OppoResult<HDMIResolution>? hdmiResolutionResponse = null;
                OppoResult<string>? audioTypeResponse = null;
                OppoResult<string>? subtitleTypeResponse = null;
                OppoResult<bool>? threeDStatusResponse = null;
                OppoResult<HDRStatus>? hdrStatusResponse = null;
                OppoResult<AspectRatio>? aspectRatioResponse = null;

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

                            hdmiResolutionResponse = await oppoClientHolder.Client.QueryHDMIResolutionAsync(cancellationTokenSource.Token);
                            audioTypeResponse = await oppoClientHolder.Client.QueryAudioTypeAsync(cancellationTokenSource.Token);
                            subtitleTypeResponse = await oppoClientHolder.Client.QuerySubtitleTypeAsync(cancellationTokenSource.Token);
                            if (oppoClientHolder.ClientKey.Model is OppoModel.UDP203 or OppoModel.UDP205)
                            {
                                threeDStatusResponse = await oppoClientHolder.Client.QueryThreeDStatusAsync(cancellationTokenSource.Token);
                                hdrStatusResponse = await oppoClientHolder.Client.QueryHDRStatusAsync(cancellationTokenSource.Token);
                                aspectRatioResponse = await oppoClientHolder.Client.QueryAspectRatioAsync(cancellationTokenSource.Token);
                            }
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

                await Task.WhenAll(
                    SendRemotePowerEventAsync(socket, wsId, oppoClientHolder, state, cancellationTokenSource.Token),
                    SendSensorEventAsync(socket, wsId, oppoClientHolder,
                        inputSourceResponse?.Result,
                        discTypeResponse?.Result,
                        hdmiResolutionResponse?.Result,
                        audioTypeResponse?.Result,
                        subtitleTypeResponse?.Result,
                        threeDStatusResponse?.Result,
                        hdrStatusResponse?.Result,
                        aspectRatioResponse?.Result,
                        cancellationTokenSource.Token));
            }
        }
        finally
        {
            bool acquiredLock = await _broadcastSemaphoreSlim.WaitAsync(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
            try
            {
                RemoveEntityIdToBroadcastingEvents(entityId, cancellationTokenWrapper);
            }
            finally
            {
                if (acquiredLock)
                    _broadcastSemaphoreSlim.Release();
            }
        }

        _logger.StoppingMediaUpdates(wsId, oppoClientHolder.Client.HostName);
        return;

        static string? ReplaceStarWithEllipsis(string? input) =>
            string.IsNullOrWhiteSpace(input) ? input : input.Replace('*', '…');

        static string? GetInputSource(in OppoResult<InputSource>? inputSourceResponse) =>
            inputSourceResponse is not { Success: true }
                ? null
                : inputSourceResponse.Value.Result switch
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
            ResponsePayloadHelpers.CreateMediaPlayerStateChangedResponsePayload(
                mediaPlayerState,
                oppoClientHolder.ClientKey.EntityId),
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
            ResponsePayloadHelpers.CreateRemoteStateChangedResponsePayload(
                new RemoteStateChangedEventMessageDataAttributes { State = state switch
                {
                    State.Buffering or State.Playing or State.Paused or State.On => RemoteState.On,
                    State.Off => RemoteState.Off,
                    _ => RemoteState.Unknown
                } },
                oppoClientHolder.ClientKey.EntityId),
            wsId,
            cancellationToken);
    }

    private Task SendSensorEventAsync(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        InputSource? inputSource,
        DiscType? discType,
        HDMIResolution? hdmiResolution,
        string? audioType,
        string? subtitleType,
        bool? threeDStatus,
        HDRStatus? hdrStatus,
        AspectRatio? aspectRatio,
        CancellationToken cancellationToken)
    {
        var clientHashCode = oppoClientHolder.ClientKey.GetHashCode();
        return Task.WhenAll(
            SendInputSourceSensor(socket, wsId, oppoClientHolder, clientHashCode,
                inputSource, cancellationToken),
            SendDiscTypeSensor(socket, wsId, oppoClientHolder, clientHashCode,
                discType, cancellationToken),
            SendHDMIResolutionSensor(socket, wsId, oppoClientHolder, clientHashCode,
                hdmiResolution, cancellationToken),
            SendAudioTypeSensor(socket, wsId, oppoClientHolder, clientHashCode,
                audioType, cancellationToken),
            SendSubtitleTypeSensor(socket, wsId, oppoClientHolder, clientHashCode,
                subtitleType, cancellationToken),
            SendThreeDSensor(socket, wsId, oppoClientHolder, clientHashCode,
                threeDStatus, cancellationToken),
            SendHDRStatusSensor(socket, wsId, oppoClientHolder, clientHashCode,
                hdrStatus, cancellationToken),
            SendAspectRatioSensor(socket, wsId, oppoClientHolder, clientHashCode,
                aspectRatio, cancellationToken)
        );
    }

    private async Task SendDiscTypeSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        DiscType? discType,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorDiscTypesMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == discType)
            return;

        PreviousSensorDiscTypesMap[clientHashCode] = discType;
        await SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateSensorStateChangedResponsePayload(
                new SensorStateChangedEventMessageDataAttributes<string>
                {
                    State = SensorState.On,
                    Value = discType?.ToStringFast(true) ?? string.Empty
                },
                oppoClientHolder.ClientKey.EntityId,
                nameof(OppoSensorType.DiscType)),
            wsId,
            cancellationToken);
    }

    private async Task SendInputSourceSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        InputSource? inputSource,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorInputSourcesMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == inputSource)
            return;

        PreviousSensorInputSourcesMap[clientHashCode] = inputSource;
        await SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateSensorStateChangedResponsePayload(
                new SensorStateChangedEventMessageDataAttributes<string>
                {
                    State = SensorState.On,
                    Value = inputSource?.ToStringFast(true) ?? string.Empty
                },
                oppoClientHolder.ClientKey.EntityId,
                nameof(OppoSensorType.InputSource)),
            wsId,
            cancellationToken);
    }

    private async Task SendHDMIResolutionSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        HDMIResolution? hdmiResolution,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorHDMIResolutionsMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == hdmiResolution)
            return;

        PreviousSensorHDMIResolutionsMap[clientHashCode] = hdmiResolution;
        await SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateSensorStateChangedResponsePayload(
                new SensorStateChangedEventMessageDataAttributes<string>
                {
                    State = SensorState.On,
                    Value = hdmiResolution?.ToStringFast(true) ?? string.Empty
                },
                oppoClientHolder.ClientKey.EntityId,
                nameof(OppoSensorType.HDMIResolution)),
            wsId,
            cancellationToken);
    }

    private async Task SendAudioTypeSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        string? audioType,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorAudioTypesMap.TryGetValue(clientHashCode, out var previousState) &&
            string.Equals(previousState, audioType, StringComparison.Ordinal))
            return;

        PreviousSensorAudioTypesMap[clientHashCode] = audioType;
        await SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateSensorStateChangedResponsePayload(
                new SensorStateChangedEventMessageDataAttributes<string>
                {
                    State = SensorState.On,
                    Value = audioType ?? string.Empty
                },
                oppoClientHolder.ClientKey.EntityId,
                nameof(OppoSensorType.AudioType)),
            wsId,
            cancellationToken);
    }

    private async Task SendSubtitleTypeSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        string? subtitleType,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorSubtitleTypesMap.TryGetValue(clientHashCode, out var previousState) &&
            string.Equals(previousState, subtitleType, StringComparison.Ordinal))
            return;

        PreviousSensorSubtitleTypesMap[clientHashCode] = subtitleType;
        await SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateSensorStateChangedResponsePayload(
                new SensorStateChangedEventMessageDataAttributes<string>
                {
                    State = SensorState.On,
                    Value = subtitleType ?? string.Empty
                },
                oppoClientHolder.ClientKey.EntityId,
                nameof(OppoSensorType.SubtitleType)),
            wsId,
            cancellationToken);
    }

    private async Task SendThreeDSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        bool? threeD,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorThreeDsMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == threeD)
            return;

        PreviousSensorThreeDsMap[clientHashCode] = threeD;
        await SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateSensorStateChangedResponsePayload(
                new SensorStateChangedEventMessageDataAttributes<string>
                {
                    State = SensorState.On,
                    Value = threeD switch
                    {
                        null => string.Empty,
                        true => "3D",
                        _ => "2D"
                    }
                },
                oppoClientHolder.ClientKey.EntityId,
                nameof(OppoSensorType.ThreeDStatus)),
            wsId,
            cancellationToken);
    }

    private async Task SendHDRStatusSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        HDRStatus? hdrStatus,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorHDRStatusMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == hdrStatus)
            return;

        PreviousSensorHDRStatusMap[clientHashCode] = hdrStatus;
        await SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateSensorStateChangedResponsePayload(
                new SensorStateChangedEventMessageDataAttributes<string>
                {
                    State = SensorState.On,
                    Value = hdrStatus?.ToStringFast(true) ?? string.Empty
                },
                oppoClientHolder.ClientKey.EntityId,
                nameof(OppoSensorType.HDRStatus)),
            wsId,
            cancellationToken);
    }

    private async Task SendAspectRatioSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        AspectRatio? aspectRatio,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorAspectRatiosMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == aspectRatio)
            return;

        PreviousSensorAspectRatiosMap[clientHashCode] = aspectRatio;
        await SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateSensorStateChangedResponsePayload(
                new SensorStateChangedEventMessageDataAttributes<string>
                {
                    State = SensorState.On,
                    Value = aspectRatio?.ToStringFast(true) ?? string.Empty
                },
                oppoClientHolder.ClientKey.EntityId,
                nameof(OppoSensorType.AspectRatio)),
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