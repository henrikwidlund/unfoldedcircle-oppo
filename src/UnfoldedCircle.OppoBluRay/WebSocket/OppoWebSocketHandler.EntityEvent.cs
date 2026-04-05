using System.Collections.Concurrent;

using Oppo;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.OppoBluRay.Configuration;
using UnfoldedCircle.OppoBluRay.Logging;
using UnfoldedCircle.OppoBluRay.OppoEntity;
using UnfoldedCircle.Server.Response;
using UnfoldedCircle.Server.WebSocket;

namespace UnfoldedCircle.OppoBluRay.WebSocket;

public partial class OppoWebSocketHandler
{
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

    protected override async Task HandleEventUpdatesAsync(System.Net.WebSockets.WebSocket socket,
        string wsId,
        SubscribedEntitiesHolder subscribedEntitiesHolder,
        CancellationToken cancellationToken)
    {
        using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        ConcurrentDictionary<string, OppoClientHolder> pollingClientHolders = new(StringComparer.OrdinalIgnoreCase);
        ConcurrentDictionary<string, StreamingClientContext> streamingClientContexts = new(StringComparer.OrdinalIgnoreCase);
        var activeKeys = new HashSet<string>(
            Math.Max(4, subscribedEntitiesHolder.SubscribedEntities.Count),
            StringComparer.OrdinalIgnoreCase);
        try
        {
            do
            {
                await ProcessSubscribedEntitiesAsync(
                    socket,
                    wsId,
                    subscribedEntitiesHolder,
                    pollingClientHolders,
                    streamingClientContexts,
                    activeKeys,
                    cancellationToken);
            } while (!cancellationToken.IsCancellationRequested && await periodicTimer.WaitForNextTickAsync(cancellationToken));
        }
        finally
        {
            await DisposeAndCleanupClientStateAsync(wsId, streamingClientContexts, pollingClientHolders);
        }
    }

    private async Task ProcessSubscribedEntitiesAsync(
        System.Net.WebSockets.WebSocket socket,
        string wsId,
        SubscribedEntitiesHolder subscribedEntitiesHolder,
        ConcurrentDictionary<string, OppoClientHolder> pollingClientHolders,
        ConcurrentDictionary<string, StreamingClientContext> streamingClientContexts,
        HashSet<string> activeKeys,
        CancellationToken cancellationToken)
    {
        activeKeys.Clear();
        foreach (var subscribedEntity in subscribedEntitiesHolder.SubscribedEntities)
        {
            activeKeys.Add(subscribedEntity.Key);

            try
            {
                if (await TryProcessStreamingContextAsync(socket, wsId, streamingClientContexts, subscribedEntity, cancellationToken))
                    continue;

                await ProcessForSingleClient(socket, wsId, pollingClientHolders, subscribedEntity, cancellationToken);
            }
            catch (Exception e)
            {
                // This is expected from control flow, no need to spam logs
                if (e is not OperationCanceledException)
                    _logger.FailureDuringEvent(e, wsId, subscribedEntity.Key);
            }
        }

        await RemoveStaleStreamingContextsAsync(streamingClientContexts, activeKeys);
        RemoveStalePollingClients(pollingClientHolders, activeKeys);
    }

    private async Task<bool> TryProcessStreamingContextAsync(
        System.Net.WebSockets.WebSocket socket,
        string wsId,
        ConcurrentDictionary<string, StreamingClientContext> streamingClientContexts,
        KeyValuePair<string, HashSet<SubscribedEntity>> subscribedEntity,
        CancellationToken cancellationToken)
    {
        if (streamingClientContexts.TryGetValue(subscribedEntity.Key, out var existingContext))
        {
            existingContext.SetSubscribedEntities(subscribedEntity.Value);
            if (IsStreamingContextActive(existingContext))
            {
                if (ShouldPollHdr(existingContext))
                    await RefreshHdrIfNeededAsync(socket, wsId, existingContext, cancellationToken);

                return true;
            }

            await TryRemoveStreamingContextAsync(streamingClientContexts, subscribedEntity.Key);
        }

        var streamingContext = await EnsureStreamingClientContextAsync(
            socket,
            wsId,
            streamingClientContexts,
            subscribedEntity,
            cancellationToken);

        return streamingContext is not null;
    }

    private async ValueTask DisposeAndCleanupClientStateAsync(
        string wsId,
        ConcurrentDictionary<string, StreamingClientContext> streamingClientContexts,
        ConcurrentDictionary<string, OppoClientHolder> pollingClientHolders)
    {
        foreach (var context in streamingClientContexts.Values)
        {
            try
            {
                await context.DisposeAsync();
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.FailureDuringEvent(e, wsId, context.ClientHolder.ClientKey.EntityId);
            }
        }

        var seen = new HashSet<int>();
        foreach (var context in streamingClientContexts.Values)
        {
            var hash = context.ClientHolder.ClientKey.GetHashCode();
            if (seen.Add(hash))
                CleanupPreviousMaps(hash);
        }

        foreach (var holder in pollingClientHolders.Values)
        {
            var hash = holder.ClientKey.GetHashCode();
            if (seen.Add(hash))
                CleanupPreviousMaps(hash);
        }
    }

    private static bool IsStreamingContextActive(StreamingClientContext context) =>
        context.StreamingTask is not { IsCompleted: true };

    private static ValueTask TryRemoveStreamingContextAsync(ConcurrentDictionary<string, StreamingClientContext> streamingClientContexts,
        string key)
    {
        return !streamingClientContexts.TryRemove(key, out var context)
            ? ValueTask.CompletedTask
            : DisposeStreamingContextAsync(context);
    }

    private static ValueTask DisposeStreamingContextAsync(StreamingClientContext context)
    {
        CleanupPreviousMaps(context.ClientHolder.ClientKey.GetHashCode());
        return context.DisposeAsync();
    }

    private async Task ProcessForSingleClient(System.Net.WebSockets.WebSocket socket,
        string wsId,
        ConcurrentDictionary<string, OppoClientHolder> oppoClientHolders,
        KeyValuePair<string, HashSet<SubscribedEntity>> subscribedEntity,
        CancellationToken cancellationToken)
    {
        var oppoClientHolder = oppoClientHolders.GetValueOrDefault(subscribedEntity.Key);
        if (oppoClientHolder is null)
        {
            _logger.TryingToGetOppoClientHolder(wsId);
            oppoClientHolder = await TryGetOppoClientHolderAsync(wsId, subscribedEntity.Key, IdentifierType.EntityId, cancellationToken);
            if (oppoClientHolder is null)
            {
                _logger.CouldNotFindOppoClientForEntityId(wsId, subscribedEntity.Key);
                return;
            }

            oppoClientHolders[subscribedEntity.Key] = oppoClientHolder;
            _logger.StartingEventsForDevice(wsId, oppoClientHolder.Client.HostName);
        }

        var snapshot = await BuildSnapshotAsync(oppoClientHolder, cancellationToken);
        await PublishSnapshotAsync(socket, wsId, oppoClientHolder, subscribedEntity.Value, snapshot, cancellationToken);
    }

    private async Task<StreamingClientContext?> EnsureStreamingClientContextAsync(
        System.Net.WebSockets.WebSocket socket,
        string wsId,
        ConcurrentDictionary<string, StreamingClientContext> streamingClientContexts,
        KeyValuePair<string, HashSet<SubscribedEntity>> subscribedEntity,
        CancellationToken cancellationToken)
    {
        var existingContext = await TryReuseStreamingClientContextAsync(streamingClientContexts, subscribedEntity);
        if (existingContext is not null)
            return existingContext;

        var oppoClientHolder = await TryGetStreamingCapableClientHolderAsync(wsId, subscribedEntity.Key, cancellationToken);
        if (oppoClientHolder is null)
            return null;

        var context = new StreamingClientContext(oppoClientHolder);
        context.SetSubscribedEntities(subscribedEntity.Value);

        if (!streamingClientContexts.TryAdd(subscribedEntity.Key, context))
            return streamingClientContexts.GetValueOrDefault(subscribedEntity.Key);

        try
        {
            await InitializeStreamingContextAsync(
                socket,
                wsId,
                subscribedEntity,
                context,
                cancellationToken);

            return context;
        }
        catch
        {
            await TryRemoveStreamingContextAsync(streamingClientContexts, subscribedEntity.Key);
            throw;
        }
    }

    private static async ValueTask<StreamingClientContext?> TryReuseStreamingClientContextAsync(
        ConcurrentDictionary<string, StreamingClientContext> streamingClientContexts,
        KeyValuePair<string, HashSet<SubscribedEntity>> subscribedEntity)
    {
        if (!streamingClientContexts.TryGetValue(subscribedEntity.Key, out var existingContext))
            return null;

        if (IsStreamingContextActive(existingContext))
        {
            existingContext.SetSubscribedEntities(subscribedEntity.Value);
            return existingContext;
        }

        await TryRemoveStreamingContextAsync(streamingClientContexts, subscribedEntity.Key);

        return null;
    }

    private async Task<OppoClientHolder?> TryGetStreamingCapableClientHolderAsync(
        string wsId,
        string entityId,
        CancellationToken cancellationToken)
    {
        _logger.TryingToGetOppoClientHolder(wsId);

        var oppoClientHolder = await TryGetOppoClientHolderAsync(wsId, entityId, IdentifierType.EntityId, cancellationToken);
        if (oppoClientHolder is null)
        {
            _logger.CouldNotFindOppoClientForEntityId(wsId, entityId);
            return null;
        }

        if (!oppoClientHolder.Client.SupportsStreamingUpdates || oppoClientHolder.ClientKey.Model == OppoModel.Magnetar)
            return null;

        _logger.StartingEventsForDevice(wsId, oppoClientHolder.Client.HostName);
        return oppoClientHolder;
    }

    private async Task InitializeStreamingContextAsync(
        System.Net.WebSockets.WebSocket socket,
        string wsId,
        KeyValuePair<string, HashSet<SubscribedEntity>> subscribedEntity,
        StreamingClientContext context,
        CancellationToken cancellationToken)
    {
        await context.Gate.WaitAsync(cancellationToken);
        try
        {
            context.Snapshot = await BuildSnapshotAsync(context.ClientHolder, cancellationToken);
            await PublishSnapshotAsync(socket,
                wsId,
                context.ClientHolder,
                subscribedEntity.Value,
                context.Snapshot,
                cancellationToken);
            context.LastHdrRefreshUtc = context.Snapshot.HdrStatusResponse is null
                ? DateTimeOffset.MinValue
                : DateTimeOffset.UtcNow;
        }
        finally
        {
            context.Gate.Release();
        }

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        context.Attach(
            linkedCts,
            Task.Run(() => RunStreamingLoopAsync(socket, wsId, subscribedEntity.Key, context, linkedCts.Token), CancellationToken.None));
    }

    private async ValueTask<ClientSnapshot> BuildSnapshotAsync(OppoClientHolder oppoClientHolder, CancellationToken cancellationToken)
    {
        var snapshot = new ClientSnapshot();
        if (!await oppoClientHolder.Client.IsConnectedAsync())
            return snapshot;

        var powerStatusResponse = await oppoClientHolder.Client.QueryPowerStatusAsync(cancellationToken);
        snapshot.State = MapPowerState(powerStatusResponse.Result);

        if (oppoClientHolder.ClientKey.Model == OppoModel.Magnetar && oppoClientHolder is { ClientKey.UseMediaEvents: false })
            return snapshot;

        if (powerStatusResponse is { Result: PowerState.On })
            await PopulatePoweredOnSnapshotAsync(oppoClientHolder, snapshot, cancellationToken);

        return snapshot;
    }

    private async ValueTask PopulatePoweredOnSnapshotAsync(
        OppoClientHolder oppoClientHolder,
        ClientSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        snapshot.VolumeResponse = await oppoClientHolder.Client.QueryVolumeAsync(cancellationToken);
        snapshot.InputSourceResponse = await oppoClientHolder.Client.QueryInputSourceAsync(cancellationToken);
        snapshot.DiscTypeResponse = await oppoClientHolder.Client.QueryDiscTypeAsync(cancellationToken);
        snapshot.HdmiResolutionResponse = await oppoClientHolder.Client.QueryHDMIResolutionAsync(cancellationToken);

        var playbackStatusResponse = await oppoClientHolder.Client.QueryPlaybackStatusAsync(cancellationToken);
        snapshot.State = MapPlaybackState(playbackStatusResponse.Result);

        snapshot.IsMovie = snapshot.DiscTypeResponse is { Success: true, Result: DiscType.BlueRayMovie or DiscType.DVDVideo or DiscType.UltraHDBluRay };

        if (playbackStatusResponse is not { Result: PlaybackStatus.Play or PlaybackStatus.Pause }
            || snapshot.DiscTypeResponse is not { Success: true }
            || snapshot.DiscTypeResponse.Value.Result is DiscType.Unknown or DiscType.UnknownDisc or DiscType.DataDisc)
            return;

        await PopulateActivePlaybackSnapshotAsync(oppoClientHolder, snapshot, cancellationToken);
    }

    private async ValueTask PopulateActivePlaybackSnapshotAsync(
        OppoClientHolder oppoClientHolder,
        ClientSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        (snapshot.RepeatMode, snapshot.Shuffle) = GetRepeatMode(await oppoClientHolder.Client.QueryRepeatModeAsync(cancellationToken));

        await PopulatePlaybackTimingAndMetadataAsync(oppoClientHolder, snapshot, cancellationToken);
        await TryPopulateAlbumCoverAsync(snapshot, cancellationToken);

        // if we're at 0, then we're at a title screen, and querying details will produce errors and lock up the player
        if (snapshot.ElapsedResponse is { Result: 0 } || snapshot.RemainingResponse is { Result: 0 })
            return;

        await PopulatePlaybackSensorsAsync(oppoClientHolder, snapshot, cancellationToken);
    }

    private static async ValueTask PopulatePlaybackTimingAndMetadataAsync(
        OppoClientHolder oppoClientHolder,
        ClientSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (snapshot.IsMovie)
        {
            snapshot.ElapsedResponse = oppoClientHolder.ClientKey.UseChapterLengthForMovies
                ? await oppoClientHolder.Client.QueryChapterElapsedTimeAsync(cancellationToken)
                : await oppoClientHolder.Client.QueryTotalElapsedTimeAsync(cancellationToken);

            if (snapshot.ElapsedResponse.Value)
            {
                snapshot.RemainingResponse = oppoClientHolder.ClientKey.UseChapterLengthForMovies
                    ? await oppoClientHolder.Client.QueryChapterRemainingTimeAsync(cancellationToken)
                    : await oppoClientHolder.Client.QueryTotalRemainingTimeAsync(cancellationToken);
            }

            return;
        }

        snapshot.ElapsedResponse = await oppoClientHolder.Client.QueryTrackOrTitleElapsedTimeAsync(cancellationToken);
        if (!snapshot.ElapsedResponse.Value)
            return;

        snapshot.RemainingResponse = await oppoClientHolder.Client.QueryTrackOrTitleRemainingTimeAsync(cancellationToken);
        if (oppoClientHolder.ClientKey.Model is not (OppoModel.UDP203 or OppoModel.UDP205))
            return;

        snapshot.TrackResponse = await oppoClientHolder.Client.QueryTrackNameAsync(cancellationToken);
        snapshot.Album = (await oppoClientHolder.Client.QueryTrackAlbumAsync(cancellationToken)).Result;
        snapshot.Performer = (await oppoClientHolder.Client.QueryTrackPerformerAsync(cancellationToken)).Result;
    }

    private async ValueTask TryPopulateAlbumCoverAsync(ClientSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(snapshot.Performer)
            || (string.IsNullOrWhiteSpace(snapshot.Album) && string.IsNullOrWhiteSpace(snapshot.TrackResponse?.Result)))
            return;

        if (snapshot.Album?.StartsWith(snapshot.Performer, StringComparison.OrdinalIgnoreCase) is true
            && snapshot.Album.AsSpan()[snapshot.Performer.Length..].StartsWith("   ", StringComparison.Ordinal))
        {
            snapshot.Album = snapshot.Album.AsSpan()[(snapshot.Performer.Length + 3)..].ToString();
        }

        snapshot.CoverUri = await _albumCoverService.GetAlbumCoverAsync(snapshot.Performer, snapshot.Album, null, cancellationToken);
    }

    private static async ValueTask PopulatePlaybackSensorsAsync(
        OppoClientHolder oppoClientHolder,
        ClientSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        snapshot.AudioTypeResponse = await oppoClientHolder.Client.QueryAudioTypeAsync(cancellationToken);
        if (!snapshot.IsMovie)
            return;

        snapshot.SubtitleTypeResponse = await oppoClientHolder.Client.QuerySubtitleTypeAsync(cancellationToken);
        if (oppoClientHolder.ClientKey.Model is not (OppoModel.UDP203 or OppoModel.UDP205))
            return;

        snapshot.ThreeDStatusResponse = await oppoClientHolder.Client.QueryThreeDStatusAsync(cancellationToken);
        snapshot.HdrStatusResponse = await oppoClientHolder.Client.QueryHDRStatusAsync(cancellationToken);
        snapshot.AspectRatioResponse = await oppoClientHolder.Client.QueryAspectRatioAsync(cancellationToken);
    }

    private async Task PublishSnapshotAsync(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        IReadOnlyCollection<SubscribedEntity> subscribedEntities,
        ClientSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        (bool hasMediaPlayer, bool hasRemote, bool hasSensor) = GetSubscriptionFlags(subscribedEntities);

        if (oppoClientHolder.ClientKey.Model == OppoModel.Magnetar)
        {
            if (hasRemote)
                await SendRemotePowerEventAsync(socket, wsId, oppoClientHolder, snapshot.State, cancellationToken);
            return;
        }

        if (oppoClientHolder is { ClientKey.UseMediaEvents: false })
        {
            var mediaPlayerState = new MediaPlayerStateChangedEventMessageDataAttributes { State = snapshot.State };
            await Task.WhenAll(
                hasMediaPlayer
                    ? SendMediaPlayerEventAsync(socket, wsId, oppoClientHolder, mediaPlayerState, cancellationToken)
                    : Task.CompletedTask,
                hasRemote
                    ? SendRemotePowerEventAsync(socket, wsId, oppoClientHolder, snapshot.State, cancellationToken)
                    : Task.CompletedTask,
                hasSensor
                    ? SendSensorEventAsync(socket, wsId, oppoClientHolder, subscribedEntities,
                        null, null, null, null, null, null, null, null, cancellationToken)
                    : Task.CompletedTask);
            return;
        }

        var newMediaPlayerState = new MediaPlayerStateChangedEventMessageDataAttributes
        {
            State = snapshot.State,
            MediaType = snapshot.DiscTypeResponse?.Result switch
            {
                DiscType.BlueRayMovie or DiscType.DVDVideo or DiscType.UltraHDBluRay => MediaType.Movie,
                DiscType.DVDAudio or DiscType.SACD or DiscType.CDDiscAudio => MediaType.Music,
                _ => null
            },
            MediaPosition = snapshot.ElapsedResponse?.Result,
            // Only set duration if we have both elapsed and remaining values
            MediaDuration = (snapshot.ElapsedResponse?.Result, snapshot.RemainingResponse?.Result) switch
            {
                (not null, not null) => snapshot.ElapsedResponse.Value.Result + snapshot.RemainingResponse.Value.Result,
                _ => null
            },
            MediaTitle = ReplaceStarWithEllipsis(snapshot.TrackResponse?.Result),
            MediaAlbum = ReplaceStarWithEllipsis(snapshot.Album),
            MediaArtist = ReplaceStarWithEllipsis(snapshot.Performer),
            MediaImageUrl = snapshot.CoverUri,
            Repeat = snapshot.RepeatMode,
            Shuffle = snapshot.Shuffle,
            Source = GetInputSource(snapshot.InputSourceResponse),
            Volume = snapshot.VolumeResponse?.Result.Volume,
            Muted = snapshot.VolumeResponse?.Result.Muted
        };

        await Task.WhenAll(
            hasMediaPlayer
                ? SendMediaPlayerEventAsync(socket, wsId, oppoClientHolder, newMediaPlayerState, cancellationToken)
                : Task.CompletedTask,
            hasRemote
                ? SendRemotePowerEventAsync(socket, wsId, oppoClientHolder, snapshot.State, cancellationToken)
                : Task.CompletedTask,
            hasSensor
                ? SendSensorEventAsync(socket, wsId, oppoClientHolder, subscribedEntities,
                    snapshot.InputSourceResponse?.Result,
                    snapshot.DiscTypeResponse?.Result,
                    snapshot.HdmiResolutionResponse?.Result,
                    snapshot.AudioTypeResponse?.Result,
                    snapshot.SubtitleTypeResponse?.Result,
                    snapshot.ThreeDStatusResponse?.Result,
                    snapshot.HdrStatusResponse?.Result,
                    snapshot.AspectRatioResponse?.Result,
                    cancellationToken)
                : Task.CompletedTask);
    }

    private async Task RunStreamingLoopAsync(System.Net.WebSockets.WebSocket socket,
        string wsId,
        string entityId,
        StreamingClientContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var streamingEvent in context.ClientHolder.Client.SubscribeStreamingUpdates(cancellationToken))
            {
                await context.Gate.WaitAsync(cancellationToken);
                try
                {
                    await ApplyStreamingEventAsync(context, streamingEvent, cancellationToken);
                    await PublishSnapshotAsync(socket,
                        wsId,
                        context.ClientHolder,
                        context.GetSubscribedEntities(),
                        context.Snapshot,
                        cancellationToken);
                }
                finally
                {
                    context.Gate.Release();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the websocket subscription is disposed.
        }
        catch (Exception e)
        {
            _logger.FailureDuringEvent(e, wsId, entityId);
        }
    }

    private async ValueTask ApplyStreamingEventAsync(StreamingClientContext context, OppoStreamingEvent streamingEvent, CancellationToken cancellationToken)
    {
        if (!context.ClientHolder.ClientKey.UseMediaEvents)
        {
            ApplyPowerOnlyStreamingEvent(context, streamingEvent);
            return;
        }

        switch (streamingEvent)
        {
            case OppoPowerStateStreamingEvent { PowerState: PowerState.Off }:
                // Device turned off – reset state without querying anything
                context.Snapshot = new ClientSnapshot { State = State.Off };
                break;

            case OppoPowerStateStreamingEvent:
                // Device turned on – rebuild to determine current playback state
                await RebuildSnapshotAndRefreshHdrTimestampAsync(context, cancellationToken);
                break;

            case OppoPlaybackStatusStreamingEvent playbackStatusEvent:
                await HandlePlaybackStatusStreamingEventAsync(context, playbackStatusEvent, cancellationToken);
                break;

            case OppoDiscTypeStreamingEvent:
            case OppoInputSourceStreamingEvent:
                // Both disc change and source change invalidate track metadata, cover art, and progress domain.
                // The streaming API only emits these events when the value actually changed – full rebuild.
                await RebuildSnapshotAndRefreshHdrTimestampAsync(context, cancellationToken);
                break;

            case OppoVolumeStreamingEvent volumeEvent:
                ApplyVolumeStreamingEvent(context.Snapshot, volumeEvent);
                break;

            case OppoVideoResolutionStreamingEvent resolutionEvent:
                await HandleVideoResolutionStreamingEventAsync(context, resolutionEvent, cancellationToken);
                break;

            case OppoAudioTypeStreamingEvent audioTypeEvent:
                ApplyAudioTypeStreamingEvent(context.Snapshot, audioTypeEvent);
                break;

            case OppoSubtitleTypeStreamingEvent subtitleTypeEvent:
                ApplySubtitleTypeStreamingEvent(context.Snapshot, subtitleTypeEvent);
                break;

            case OppoThreeDStatusStreamingEvent threeDStatusEvent:
                ApplyThreeDStatusStreamingEvent(context.Snapshot, threeDStatusEvent);
                break;

            case OppoAspectRatioStreamingEvent aspectRatioEvent:
                ApplyAspectRatioStreamingEvent(context.Snapshot, aspectRatioEvent);
                break;

            case OppoPlaybackProgressStreamingEvent playbackProgressEvent:
                await HandlePlaybackProgressStreamingEventAsync(context, playbackProgressEvent, cancellationToken);
                break;
        }
    }

    private static void ApplyPowerOnlyStreamingEvent(StreamingClientContext context, OppoStreamingEvent streamingEvent)
    {
        if (streamingEvent is not OppoPowerStateStreamingEvent powerEvent)
            return;

        context.Snapshot.State = MapPowerState(powerEvent.PowerState);
    }

    private static void ApplyVolumeStreamingEvent(ClientSnapshot snapshot, OppoVolumeStreamingEvent volumeEvent)
    {
        // Volume is self-contained – update directly from event
        snapshot.VolumeResponse = new OppoResult<VolumeInfo>
        {
            Success = true,
            Result = volumeEvent.VolumeInfo
        };
    }

    private static void ApplyAudioTypeStreamingEvent(ClientSnapshot snapshot, OppoAudioTypeStreamingEvent audioTypeEvent)
    {
        // Audio type is self-contained – update directly from event
        snapshot.AudioTypeResponse = new OppoResult<string>
        {
            Success = true,
            Result = audioTypeEvent.AudioType
        };
    }

    private static void ApplySubtitleTypeStreamingEvent(ClientSnapshot snapshot, OppoSubtitleTypeStreamingEvent subtitleTypeEvent)
    {
        // Subtitle type is self-contained – update directly from event
        snapshot.SubtitleTypeResponse = new OppoResult<string>
        {
            Success = true,
            Result = subtitleTypeEvent.SubtitleType
        };
    }

    private static void ApplyThreeDStatusStreamingEvent(ClientSnapshot snapshot, OppoThreeDStatusStreamingEvent threeDStatusEvent)
    {
        // 3D status is self-contained – update directly from event
        snapshot.ThreeDStatusResponse = new OppoResult<bool>
        {
            Success = true,
            Result = threeDStatusEvent.Is3D
        };
    }

    private static void ApplyAspectRatioStreamingEvent(ClientSnapshot snapshot, OppoAspectRatioStreamingEvent aspectRatioEvent)
    {
        // Aspect ratio is self-contained – update directly from event
        snapshot.AspectRatioResponse = new OppoResult<AspectRatio>
        {
            Success = true,
            Result = aspectRatioEvent.AspectRatio
        };
    }

    private async ValueTask HandlePlaybackStatusStreamingEventAsync(
        StreamingClientContext context,
        OppoPlaybackStatusStreamingEvent playbackStatusEvent,
        CancellationToken cancellationToken)
    {
        var newState = playbackStatusEvent.PlaybackStatus switch
        {
            PlaybackStatus.Play => State.Playing,
            PlaybackStatus.Pause => State.Paused,
            PlaybackStatus.FastForward or PlaybackStatus.FastRewind
                or PlaybackStatus.SlowForward or PlaybackStatus.SlowRewind => State.Buffering,
            _ => State.On
        };

        var prevState = context.Snapshot.State;
        var isNowActive = IsActivePlaybackState(newState);
        var wasActive = IsActivePlaybackState(prevState);

        if (isNowActive && !wasActive)
        {
            // Transitioned into active playback – rebuild for fresh disc/track/progress data
            await RebuildSnapshotAndRefreshHdrTimestampAsync(context, cancellationToken);
            return;
        }

        context.Snapshot.State = newState;
        if (!isNowActive)
        {
            // Stopped or navigated away – clear stale progress data
            context.Snapshot.ElapsedResponse = null;
            context.Snapshot.RemainingResponse = null;
        }
    }

    private static async ValueTask HandleVideoResolutionStreamingEventAsync(
        StreamingClientContext context,
        OppoVideoResolutionStreamingEvent resolutionEvent,
        CancellationToken cancellationToken)
    {
        // Resolution is self-contained – update directly from event
        context.Snapshot.HdmiResolutionResponse = new OppoResult<HDMIResolution>
        {
            Success = true,
            Result = resolutionEvent.Resolution
        };

        if (ShouldQueryHdrStatus(context) && context.HasHdrSensorSubscription())
        {
            context.Snapshot.HdrStatusResponse = await context.ClientHolder.Client.QueryHDRStatusAsync(cancellationToken);
            context.LastHdrRefreshUtc = DateTimeOffset.UtcNow;
        }
        else
        {
            context.Snapshot.HdrStatusResponse = null;
        }
    }

    private async ValueTask HandlePlaybackProgressStreamingEventAsync(
        StreamingClientContext context,
        OppoPlaybackProgressStreamingEvent playbackProgressEvent,
        CancellationToken cancellationToken)
    {
        // Rebuild if title/chapter changed – this invalidates track metadata and progress domain
        if (context.Snapshot.LastProgressTitle != playbackProgressEvent.Title
            || context.Snapshot.LastProgressChapter != playbackProgressEvent.Chapter)
        {
            await RebuildSnapshotAndRefreshHdrTimestampAsync(context, cancellationToken);
            context.Snapshot.LastProgressTitle = playbackProgressEvent.Title;
            context.Snapshot.LastProgressChapter = playbackProgressEvent.Chapter;
            return;
        }

        // Same title/chapter – apply progress directly from event (no rebuild needed)
        UpdateProgress(context.Snapshot, playbackProgressEvent);
    }

    private static async ValueTask RemoveStaleStreamingContextsAsync(
        ConcurrentDictionary<string, StreamingClientContext> streamingClientContexts,
        HashSet<string> activeKeys)
    {
        List<string>? stale = null;
        foreach (var key in streamingClientContexts.Keys)
        {
            if (!activeKeys.Contains(key))
                (stale ??= []).Add(key);
        }

        if (stale is null)
            return;

        foreach (var staleKey in stale)
        {
            if (!streamingClientContexts.TryRemove(staleKey, out var context))
                continue;

            await DisposeStreamingContextAsync(context);
        }
    }

    private static void RemoveStalePollingClients(
        ConcurrentDictionary<string, OppoClientHolder> pollingClientHolders,
        HashSet<string> activeKeys)
    {
        List<string>? stale = null;
        foreach (var key in pollingClientHolders.Keys)
        {
            if (!activeKeys.Contains(key))
                (stale ??= []).Add(key);
        }

        if (stale is null)
            return;

        foreach (var staleKey in stale)
        {
            if (!pollingClientHolders.TryRemove(staleKey, out var clientHolder))
                continue;

            CleanupPreviousMaps(clientHolder.ClientKey.GetHashCode());
        }
    }

    private static bool ShouldPollHdr(StreamingClientContext context)
    {
        return context.ClientHolder.ClientKey.UseMediaEvents
               && context.HasHdrSensorSubscription()
               && DateTimeOffset.UtcNow - context.LastHdrRefreshUtc >= TimeSpan.FromSeconds(5)
               && ShouldQueryHdrStatus(context);
    }

    private async Task RefreshHdrIfNeededAsync(System.Net.WebSockets.WebSocket socket,
        string wsId,
        StreamingClientContext context,
        CancellationToken cancellationToken)
    {
        await context.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!ShouldPollHdr(context))
                return;

            context.Snapshot.HdrStatusResponse = await context.ClientHolder.Client.QueryHDRStatusAsync(cancellationToken);
            context.LastHdrRefreshUtc = DateTimeOffset.UtcNow;

            await PublishSnapshotAsync(socket,
                wsId,
                context.ClientHolder,
                context.GetSubscribedEntities(),
                context.Snapshot,
                cancellationToken);
        }
        finally
        {
            context.Gate.Release();
        }
    }

    private async ValueTask RebuildSnapshotAndRefreshHdrTimestampAsync(
        StreamingClientContext context,
        CancellationToken cancellationToken)
    {
        context.Snapshot = await BuildSnapshotAsync(context.ClientHolder, cancellationToken);
        if (context.Snapshot.HdrStatusResponse is not null)
            context.LastHdrRefreshUtc = DateTimeOffset.UtcNow;
    }

    private static bool ShouldQueryHdrStatus(StreamingClientContext context)
    {
        return context.ClientHolder.ClientKey.Model is OppoModel.UDP203 or OppoModel.UDP205
               && IsActivePlaybackState(context.Snapshot.State)
               && context.Snapshot.HdmiResolutionResponse?.Result is HDMIResolution.RUltraHDp24 or HDMIResolution.RUltraHDp50 or HDMIResolution.RUltraHDp60;
    }

    private static bool IsActivePlaybackState(in State state) =>
        state is State.Playing or State.Paused or State.Buffering;

    private static State MapPowerState(in PowerState powerState) =>
        powerState switch
        {
            PowerState.On => State.On,
            PowerState.Off => State.Off,
            _ => State.Unknown
        };

    private static State MapPlaybackState(in PlaybackStatus playbackStatus) =>
        playbackStatus switch
        {
            PlaybackStatus.Unknown => State.Unknown,
            PlaybackStatus.Play => State.Playing,
            PlaybackStatus.Pause => State.Paused,
            PlaybackStatus.FastForward or PlaybackStatus.FastRewind or PlaybackStatus.SlowForward or PlaybackStatus.SlowRewind => State.Buffering,
            _ => State.On
        };

    private static (bool HasMediaPlayer, bool HasRemote, bool HasSensor) GetSubscriptionFlags(
        IReadOnlyCollection<SubscribedEntity> subscribedEntities)
    {
        bool hasMediaPlayer = false, hasRemote = false, hasSensor = false;
        foreach (var entity in subscribedEntities)
        {
            switch (entity.EntityType)
            {
                case EntityType.MediaPlayer:
                    hasMediaPlayer = true;
                    break;
                case EntityType.Remote:
                    hasRemote = true;
                    break;
                case EntityType.Sensor:
                    hasSensor = true;
                    break;
            }

            if (hasMediaPlayer && hasRemote && hasSensor)
                break;
        }

        return (hasMediaPlayer, hasRemote, hasSensor);
    }

    private static void UpdateProgress(ClientSnapshot snapshot, OppoPlaybackProgressStreamingEvent playbackProgressEvent)
    {
        switch (playbackProgressEvent.TimeCodeType)
        {
            case OppoTimeCodeType.TotalElapsed:
            case OppoTimeCodeType.TitleElapsed:
            case OppoTimeCodeType.ChapterElapsed:
                snapshot.ElapsedResponse = new OppoResult<uint>
                {
                    Success = true,
                    Result = playbackProgressEvent.Seconds
                };
                break;

            case OppoTimeCodeType.TotalRemaining:
            case OppoTimeCodeType.TitleRemaining:
            case OppoTimeCodeType.ChapterRemaining:
                snapshot.RemainingResponse = new OppoResult<uint>
                {
                    Success = true,
                    Result = playbackProgressEvent.Seconds
                };
                break;
        }
    }

    private static void CleanupPreviousMaps(in int clientHashCode)
    {
        PreviousMediaStatesMap.TryRemove(clientHashCode, out _);
        PreviousRemoteStatesMap.TryRemove(clientHashCode, out _);
        PreviousSensorInputSourcesMap.TryRemove(clientHashCode, out _);
        PreviousSensorDiscTypesMap.TryRemove(clientHashCode, out _);
        PreviousSensorHDMIResolutionsMap.TryRemove(clientHashCode, out _);
        PreviousSensorAudioTypesMap.TryRemove(clientHashCode, out _);
        PreviousSensorSubtitleTypesMap.TryRemove(clientHashCode, out _);
        PreviousSensorThreeDsMap.TryRemove(clientHashCode, out _);
        PreviousSensorHDRStatusMap.TryRemove(clientHashCode, out _);
        PreviousSensorAspectRatiosMap.TryRemove(clientHashCode, out _);
    }

    private static string? ReplaceStarWithEllipsis(string? input) =>
        string.IsNullOrWhiteSpace(input) ? input : input.Replace('*', '…');

    private static string? GetInputSource(in OppoResult<InputSource>? inputSourceResponse) =>
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

    private Task SendMediaPlayerEventAsync(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        MediaPlayerStateChangedEventMessageDataAttributes mediaPlayerState,
        CancellationToken cancellationToken)
    {
        var stateHash = mediaPlayerState.GetHashCode();
        var clientHashCode = oppoClientHolder.ClientKey.GetHashCode();
        if (PreviousMediaStatesMap.TryGetValue(clientHashCode, out var previousStateHash) &&
            previousStateHash == stateHash)
            return Task.CompletedTask;

        PreviousMediaStatesMap[clientHashCode] = stateHash;
        return SendMessageAsync(socket,
            ResponsePayloadHelpers.CreateMediaPlayerStateChangedResponsePayload(
                mediaPlayerState,
                oppoClientHolder.ClientKey.EntityId),
            wsId,
            cancellationToken);
    }

    private Task SendRemotePowerEventAsync(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        State state,
        CancellationToken cancellationToken)
    {
        var clientHashCode = oppoClientHolder.ClientKey.GetHashCode();
        if (PreviousRemoteStatesMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == state)
            return Task.CompletedTask;

        PreviousRemoteStatesMap[clientHashCode] = state;
        return SendMessageAsync(socket,
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
        IReadOnlyCollection<SubscribedEntity> subscribedEntities,
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
        List<Task>? tasks = null;
        foreach (var subscribedEntity in subscribedEntities)
        {
            if (subscribedEntity.EntityType != EntityType.Sensor)
                continue;

            var task = subscribedEntity.EntityId switch
            {
                _ when subscribedEntity.EntityId.EndsWith(nameof(OppoSensorType.InputSource), StringComparison.OrdinalIgnoreCase) => SendInputSourceSensor(socket, wsId, oppoClientHolder, clientHashCode, inputSource, cancellationToken),
                _ when subscribedEntity.EntityId.EndsWith(nameof(OppoSensorType.DiscType), StringComparison.OrdinalIgnoreCase) => SendDiscTypeSensor(socket, wsId, oppoClientHolder, clientHashCode, discType, cancellationToken),
                _ when subscribedEntity.EntityId.EndsWith(nameof(OppoSensorType.HDMIResolution), StringComparison.OrdinalIgnoreCase) => SendHDMIResolutionSensor(socket, wsId, oppoClientHolder, clientHashCode, hdmiResolution, cancellationToken),
                _ when subscribedEntity.EntityId.EndsWith(nameof(OppoSensorType.AudioType), StringComparison.OrdinalIgnoreCase) => SendAudioTypeSensor(socket, wsId, oppoClientHolder, clientHashCode, audioType, cancellationToken),
                _ when subscribedEntity.EntityId.EndsWith(nameof(OppoSensorType.SubtitleType), StringComparison.OrdinalIgnoreCase) => SendSubtitleTypeSensor(socket, wsId, oppoClientHolder, clientHashCode, subtitleType, cancellationToken),
                _ when subscribedEntity.EntityId.EndsWith(nameof(OppoSensorType.ThreeDStatus), StringComparison.OrdinalIgnoreCase) => SendThreeDSensor(socket, wsId, oppoClientHolder, clientHashCode, threeDStatus, cancellationToken),
                _ when subscribedEntity.EntityId.EndsWith(nameof(OppoSensorType.HDRStatus), StringComparison.OrdinalIgnoreCase) => SendHDRStatusSensor(socket, wsId, oppoClientHolder, clientHashCode, hdrStatus, cancellationToken),
                _ when subscribedEntity.EntityId.EndsWith(nameof(OppoSensorType.AspectRatio), StringComparison.OrdinalIgnoreCase) => SendAspectRatioSensor(socket, wsId, oppoClientHolder, clientHashCode, aspectRatio, cancellationToken),
                _ => null
            };

            if (task is null)
                continue;

            (tasks ??= new List<Task>(subscribedEntities.Count)).Add(task);
        }

        return tasks is null ? Task.CompletedTask : Task.WhenAll(tasks);
    }

    private Task SendDiscTypeSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        DiscType? discType,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorDiscTypesMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == discType)
            return Task.CompletedTask;

        PreviousSensorDiscTypesMap[clientHashCode] = discType;
        return SendMessageAsync(socket,
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

    private Task SendInputSourceSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        InputSource? inputSource,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorInputSourcesMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == inputSource)
            return Task.CompletedTask;

        PreviousSensorInputSourcesMap[clientHashCode] = inputSource;
        return SendMessageAsync(socket,
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

    private Task SendHDMIResolutionSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        HDMIResolution? hdmiResolution,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorHDMIResolutionsMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == hdmiResolution)
            return Task.CompletedTask;

        PreviousSensorHDMIResolutionsMap[clientHashCode] = hdmiResolution;
        return SendMessageAsync(socket,
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

    private Task SendAudioTypeSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        string? audioType,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorAudioTypesMap.TryGetValue(clientHashCode, out var previousState) &&
            string.Equals(previousState, audioType, StringComparison.Ordinal))
            return Task.CompletedTask;

        PreviousSensorAudioTypesMap[clientHashCode] = audioType;
        return SendMessageAsync(socket,
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

    private Task SendSubtitleTypeSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        string? subtitleType,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorSubtitleTypesMap.TryGetValue(clientHashCode, out var previousState) &&
            string.Equals(previousState, subtitleType, StringComparison.Ordinal))
            return Task.CompletedTask;

        PreviousSensorSubtitleTypesMap[clientHashCode] = subtitleType;
        return SendMessageAsync(socket,
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

    private Task SendThreeDSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        bool? threeD,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorThreeDsMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == threeD)
            return Task.CompletedTask;

        PreviousSensorThreeDsMap[clientHashCode] = threeD;
        return SendMessageAsync(socket,
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

    private Task SendHDRStatusSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        HDRStatus? hdrStatus,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorHDRStatusMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == hdrStatus)
            return Task.CompletedTask;

        PreviousSensorHDRStatusMap[clientHashCode] = hdrStatus;
        return SendMessageAsync(socket,
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

    private Task SendAspectRatioSensor(System.Net.WebSockets.WebSocket socket,
        string wsId,
        OppoClientHolder oppoClientHolder,
        int clientHashCode,
        AspectRatio? aspectRatio,
        CancellationToken cancellationToken)
    {
        if (PreviousSensorAspectRatiosMap.TryGetValue(clientHashCode, out var previousState) &&
            previousState == aspectRatio)
            return Task.CompletedTask;

        PreviousSensorAspectRatiosMap[clientHashCode] = aspectRatio;
        return SendMessageAsync(socket,
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

    private sealed class StreamingClientContext(OppoClientHolder clientHolder) : IAsyncDisposable
    {
        private SubscribedEntity[] _subscribedEntities = [];
        private CancellationTokenSource? _cancellationTokenSource;

        public OppoClientHolder ClientHolder { get; } = clientHolder;
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public ClientSnapshot Snapshot { get; set; } = new();
        public Task? StreamingTask { get; private set; }
        public DateTimeOffset LastHdrRefreshUtc { get; set; } = DateTimeOffset.MinValue;

        public void SetSubscribedEntities(HashSet<SubscribedEntity> subscribedEntities) =>
            _subscribedEntities = subscribedEntities.ToArray();

        public IReadOnlyCollection<SubscribedEntity> GetSubscribedEntities() =>
            _subscribedEntities;

        public bool HasHdrSensorSubscription()
        {
            foreach (var e in _subscribedEntities)
            {
                if (e.EntityType == EntityType.Sensor
                    && e.EntityId.EndsWith(nameof(OppoSensorType.HDRStatus), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public void Attach(CancellationTokenSource cancellationTokenSource, Task streamingTask)
        {
            _cancellationTokenSource = cancellationTokenSource;
            StreamingTask = streamingTask;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_cancellationTokenSource is not null)
                    await _cancellationTokenSource.CancelAsync();
            }
            catch (ObjectDisposedException)
            {
                // ignored
            }

            if (StreamingTask is not null)
            {
                try
                {
                    await StreamingTask;
                }
                catch (OperationCanceledException)
                {
                    // expected
                }
            }

            _cancellationTokenSource?.Dispose();
            Gate.Dispose();
        }
    }

    private sealed class ClientSnapshot
    {
        public State State { get; set; } = State.Unknown;
        public bool IsMovie { get; set; }
        public ushort? LastProgressTitle { get; set; }
        public ushort? LastProgressChapter { get; set; }

        public OppoResult<VolumeInfo>? VolumeResponse { get; set; }
        public OppoResult<InputSource>? InputSourceResponse { get; set; }
        public OppoResult<DiscType>? DiscTypeResponse { get; set; }
        public OppoResult<uint>? ElapsedResponse { get; set; }
        public OppoResult<uint>? RemainingResponse { get; set; }
        public OppoResult<string>? TrackResponse { get; set; }
        public string? Album { get; set; }
        public string? Performer { get; set; }
        public Uri? CoverUri { get; set; }
        public bool? Shuffle { get; set; }
        public Models.Shared.RepeatMode? RepeatMode { get; set; }
        public OppoResult<HDMIResolution>? HdmiResolutionResponse { get; set; }
        public OppoResult<string>? AudioTypeResponse { get; set; }
        public OppoResult<string>? SubtitleTypeResponse { get; set; }
        public OppoResult<bool>? ThreeDStatusResponse { get; set; }
        public OppoResult<HDRStatus>? HdrStatusResponse { get; set; }
        public OppoResult<AspectRatio>? AspectRatioResponse { get; set; }
    }

    private static (Models.Shared.RepeatMode? RepeatMode, bool? shuffle) GetRepeatMode(in OppoResult<CurrentRepeatMode> repeatModeResponse) =>
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
