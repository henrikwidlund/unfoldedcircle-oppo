using System.Collections.Concurrent;
using System.Globalization;

using UnfoldedCircle.Models.Shared;
using UnfoldedCircle.Models.Sync;
using UnfoldedCircle.Server.Event;
using UnfoldedCircle.Server.Oppo;
using UnfoldedCircle.Server.Response;

namespace UnfoldedCircle.Server.WebSocket;

internal partial class UnfoldedCircleWebSocketHandler
{
    private static readonly ConcurrentDictionary<string, string> SocketIdEntityIpMap = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, bool> SetupInProgressMap = new(StringComparer.Ordinal);
    
    private async Task HandleRequestMessage(
        System.Net.WebSockets.WebSocket socket,
        string wsId,
        MessageEvent messageEvent,
        JsonDocument jsonDocument,
        CancellationTokenWrapper cancellationTokenWrapper)
    {
        switch (messageEvent)
        {
            case MessageEvent.GetDriverVersion:
            {
                var payload = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.CommonReq)!;
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateDriverVersionResponsePayload(
                        payload,
                        new DriverVersion
                        {
                            Name = "Oppo UDP-20x Blu-ray Player",
                            Version = new DriverVersionInner
                            {
                                Driver = "0.0.1"
                            }
                        }, _unfoldedCircleJsonSerializerContext),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.GetDriverMetaData:
            {
                var payload = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.CommonReq)!;
                
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateDriverMetaDataResponsePayload(payload, CreateDriverMetadata(), _unfoldedCircleJsonSerializerContext),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.GetDeviceState:
            {
                var payload = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.GetDeviceStateMsg)!;
                var oppoClientHolder = await TryGetOppoClientHolder(wsId, payload.MsgData.DeviceId, cancellationTokenWrapper.ApplicationStopping);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateGetDeviceStateResponsePayload(
                        oppoClientHolder?.Client.GetDeviceState() ?? DeviceState.Error,
                        payload.MsgData.DeviceId ?? oppoClientHolder?.Client.GetHost(),
                        _unfoldedCircleJsonSerializerContext
                    ),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.GetAvailableEntities:
            {
                var payload = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.GetAvailableEntitiesMsg)!;
                var oppoClientHolder = await TryGetOppoClientHolder(wsId, payload.MsgData.Filter?.DeviceId, cancellationTokenWrapper.ApplicationStopping);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateGetAvailableEntitiesMsg(payload,
                        new AvailableEntitiesMsgData<MediaPlayerEntityFeature>
                        {
                            Filter = payload.MsgData.Filter,
                            AvailableEntities = GetAvailableEntities(oppoClientHolder, payload)
                        },
                        _unfoldedCircleJsonSerializerContext),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.SubscribeEvents:
            {
                var payload = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.CommonReq)!;
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateCommonResponsePayload(payload, _unfoldedCircleJsonSerializerContext),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);

                SetupInProgressMap.TryRemove(wsId, out _);
                
                return;
            }
            case MessageEvent.UnsubscribeEvents:
            {
                var payload = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.UnsubscribeEventsMsg)!;
                
                await RemoveConfiguration(new RemoveInstruction(payload.MsgData?.DeviceId, payload.MsgData?.EntityIds, null), cancellationTokenWrapper.ApplicationStopping);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateCommonResponsePayload(payload, _unfoldedCircleJsonSerializerContext),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.GetEntityStates:
            {
                var payload = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.GetEntityStatesMsg)!;
                var oppoClientHolder = await TryGetOppoClientHolder(wsId, payload.MsgData?.DeviceId, cancellationTokenWrapper.ApplicationStopping);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateGetEntityStatesResponsePayload(payload,
                        oppoClientHolder is { Client.IsConnected: true },
                        payload.MsgData?.DeviceId,
                        _unfoldedCircleJsonSerializerContext),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.SetupDriver:
            {
                var payload = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.SetupDriverMsg)!;
                SocketIdEntityIpMap.AddOrUpdate(wsId,
                    static (_, arg) => arg.MsgData.SetupData["ip_address"],
                    static (_, _, arg) => arg.MsgData.SetupData["ip_address"], payload);
                SetupInProgressMap.AddOrUpdate(wsId, static _ => true, static (_, _) => true);
                
                var entity = await UpdateConfiguration(payload.MsgData.SetupData, cancellationTokenWrapper.ApplicationStopping);
                var oppoClientHolder = await TryGetOppoClientHolder(wsId, entity.DeviceId, cancellationTokenWrapper.ApplicationStopping);
                
                var isConnected = oppoClientHolder is { Client.IsConnected: true };
                
                await Task.WhenAll(
                    SendAsync(socket,
                        ResponsePayloadHelpers.CreateCommonResponsePayload(payload, _unfoldedCircleJsonSerializerContext),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping),
                    SendAsync(socket,
                        ResponsePayloadHelpers.CreateDeviceSetupChangeResponsePayload(isConnected, _unfoldedCircleJsonSerializerContext),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping),
                    SendAsync(socket,
                        ResponsePayloadHelpers.CreateConnectEventResponsePayload(oppoClientHolder?.Client.GetDeviceState() ?? DeviceState.Error,
                            _unfoldedCircleJsonSerializerContext),
                        wsId,
                        cancellationTokenWrapper.ApplicationStopping)
                );
                
                return;
            }
            case MessageEvent.SetupDriverUserData:
            {
                var payload = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.SetDriverUserDataMsg)!;
                SocketIdEntityIpMap.AddOrUpdate(wsId,
                    static (_, arg) => arg.MsgData.SetupData["ip_address"],
                    static (_, _, arg) => arg.MsgData.SetupData["ip_address"], payload);
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateCommonResponsePayload(payload, _unfoldedCircleJsonSerializerContext),
                    wsId,
                    cancellationTokenWrapper.ApplicationStopping);
                
                return;
            }
            case MessageEvent.EntityCommand:
            {
                var payload = jsonDocument.Deserialize(_unfoldedCircleJsonSerializerContext.EntityCommandMsgOppoCommandId)!;
                await HandleEntityCommand(socket, payload, wsId, payload.MsgData.DeviceId, cancellationTokenWrapper);
                return;
            }
            case MessageEvent.SupportedEntityTypes:
            {
                // var payload = jsonDocument.Deserialize(unfoldedCircleJsonSerializerContext.CommonReq)!;
                return;
            }
            default:
                return;
        }
    }

    private static AvailableEntity<MediaPlayerEntityFeature>[]? _availableEntities;
    private static AvailableEntity<MediaPlayerEntityFeature>[] GetAvailableEntities(OppoClientHolder? oppoClientHolder, GetAvailableEntitiesMsg payload) =>
        _availableEntities ??= oppoClientHolder is { Client.IsConnected: true } ? [
            new AvailableEntity<MediaPlayerEntityFeature>
            {
                Name = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["en"] = "Oppo UDP-20x Blu-ray Player"
                },
                EntityId = "0393caf1-c9d2-422e-88b5-cb716756334a",
                EntityType = EntityType.MediaPlayer,
                Features = OppoEntitySettings.MediaPlayerEntityFeatures,
                Options = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["simple_commands"] = OppoEntitySettings.SimpleCommands
                },
                DeviceId = payload.MsgData.Filter?.DeviceId ?? oppoClientHolder.Client.GetHost()
            }
        ] : [];

    private static DriverMetadata? _driverMetadata;
    private static DriverMetadata CreateDriverMetadata() =>
        _driverMetadata ??= new DriverMetadata
        {
            Name = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = "Oppo UDP-20x Blu-ray Player"
            },
            Description = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = "Integration with the Oppo UDP-20x series of Blu-ray players"
            },
            Version = "0.0.1",
            DriverId = "oppo-unfolded-circle",
            Developer =
                new DriverDeveloper { Email = "none@of-your.business", Name = "Henrik Widlund", Url = new Uri("https://github.com/henrikwidlund/unfoldedcircle-integration") },
            ReleaseDate = DateOnly.Parse("2024-09-16", DateTimeFormatInfo.InvariantInfo),
            SetupDataSchema = new SettingsPage
            {
                Title = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["en"] = "Enter Details"
                },
                Settings =
                [
                    new Setting
                    {
                        Id = "ip_address",
                        Field = new SettingTypeText
                        {
                            Text = new ValueRegex
                            {
                                RegEx = Ipv4Or6,
                                Value = string.Empty
                            }
                        },
                        Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["en"] = "Enter the IP address of the Oppo player"
                        }
                    },
                    new Setting
                    {
                        Field = new SettingTypeCheckbox
                        {
                            Checkbox = new SettingTypeCheckboxInner
                            {
                                Value = false
                            }
                        },
                        Id = "use_media_events",
                        Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["en"] = "Use Media Events? This enables playback information at the expense of updates every second"
                        }
                    },
                    new Setting
                    {
                        Field = new SettingTypeDropdown
                        {
                            Dropdown = new SettingTypeDropdownInner
                            {
                                Items =
                                [
                                    new SettingTypeDropdownItem
                                    {
                                        Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                        {
                                            ["en"] = "Movie Length"
                                        },
                                        Value = "movie_length"
                                    },
                                    new SettingTypeDropdownItem
                                    {
                                        Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                        {
                                            ["en"] = "Chapter Length"
                                        },
                                        Value = "chapter_length"
                                    }
                                ],
                                Value = "movie_length"
                            }
                        },
                        Id = "chapter_or_movie_length",
                        Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["en"] = "Use chapter or movie length for progress bar (only applicable if Media Events is enabled)?"
                        }
                    }
                ]
            },
            DeviceDiscovery = false,
            Icon = "custom:oppo.png"
        };

    private const string? Ipv4Or6 = """^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))$""";
}