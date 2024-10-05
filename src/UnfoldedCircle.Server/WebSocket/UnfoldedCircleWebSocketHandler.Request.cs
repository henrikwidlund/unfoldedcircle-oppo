using System.Collections.Concurrent;

using Oppo;

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
                            Name = OppoConstants.DriverName,
                            Version = new DriverVersionInner
                            {
                                Driver = OppoConstants.DriverVersion
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
                        await GetDeviceState(oppoClientHolder),
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
                bool isConnected;
                string? host;
                if (oppoClientHolder is not null)
                {
                    isConnected = await oppoClientHolder.Client.IsConnectedAsync();
                    host = oppoClientHolder.Client.GetHost();
                }
                else
                {
                    isConnected = false;
                    host = null;
                }
                
                await SendAsync(socket,
                    ResponsePayloadHelpers.CreateGetAvailableEntitiesMsg(payload,
                        new AvailableEntitiesMsgData<MediaPlayerEntityFeature>
                        {
                            Filter = payload.MsgData.Filter,
                            AvailableEntities = GetAvailableEntities(payload, isConnected, host)
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
                        oppoClientHolder is not null && await oppoClientHolder.Client.IsConnectedAsync(),
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
                    static (_, arg) => arg.MsgData.SetupData[OppoConstants.IpAddressKey],
                    static (_, _, arg) => arg.MsgData.SetupData[OppoConstants.IpAddressKey], payload);
                SetupInProgressMap.AddOrUpdate(wsId, static _ => true, static (_, _) => true);
                
                var entity = await UpdateConfiguration(payload.MsgData.SetupData, cancellationTokenWrapper.ApplicationStopping);
                var oppoClientHolder = await TryGetOppoClientHolder(wsId, entity.DeviceId, cancellationTokenWrapper.ApplicationStopping);
                
                var isConnected = oppoClientHolder is not null && await oppoClientHolder.Client.IsConnectedAsync();
                
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
                        ResponsePayloadHelpers.CreateConnectEventResponsePayload(await GetDeviceState(oppoClientHolder),
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
                    static (_, arg) => arg.MsgData.SetupData[OppoConstants.IpAddressKey],
                    static (_, _, arg) => arg.MsgData.SetupData[OppoConstants.IpAddressKey], payload);
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
    
    private static AvailableEntity<MediaPlayerEntityFeature>[] GetAvailableEntities(
        GetAvailableEntitiesMsg payload,
        in bool isConnected,
        string? host) => 
        isConnected ? [
            new AvailableEntity<MediaPlayerEntityFeature>
            {
                Name = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["en"] = OppoConstants.DeviceName
                },
                EntityId = OppoConstants.EntityId,
                EntityType = EntityType.MediaPlayer,
                Features = OppoEntitySettings.MediaPlayerEntityFeatures,
                Options = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["simple_commands"] = OppoEntitySettings.SimpleCommands
                },
                DeviceId = payload.MsgData.Filter?.DeviceId ?? host
            }
        ] : [];

    private static DriverMetadata? _driverMetadata;
    private static DriverMetadata CreateDriverMetadata() =>
        _driverMetadata ??= new DriverMetadata
        {
            Name = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = OppoConstants.DriverName
            },
            Description = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = OppoConstants.DriverDescription
            },
            Version = OppoConstants.DriverVersion,
            DriverId = OppoConstants.DriverId,
            Developer = new DriverDeveloper { Email = OppoConstants.DriverEmail, Name = OppoConstants.DriverDeveloper, Url = OppoConstants.DriverUrl },
            ReleaseDate = OppoConstants.DriverReleaseDate,
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
                        Id = OppoConstants.IpAddressKey,
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
                                            ["en"] = nameof(OppoModel.BDP83)
                                        },
                                        Value = nameof(OppoModel.BDP83)
                                    },
                                    new SettingTypeDropdownItem
                                    {
                                        Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                        {
                                            ["en"] = nameof(OppoModel.BDP10X)
                                        },
                                        Value = nameof(OppoModel.BDP10X)
                                    },
                                    new SettingTypeDropdownItem
                                    {
                                        Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                        {
                                            ["en"] = nameof(OppoModel.UDP20X)
                                        },
                                        Value = nameof(OppoModel.UDP20X)
                                    }
                                ]
                            }
                        },
                        Id = OppoConstants.OppoModelKey,
                        Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["en"] = "Select the model of your Oppo player"
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
                        Id = OppoConstants.UseMediaEventsKey,
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
                                        Value = OppoConstants.MovieLengthValue
                                    },
                                    new SettingTypeDropdownItem
                                    {
                                        Label = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                        {
                                            ["en"] = "Chapter Length"
                                        },
                                        Value = OppoConstants.ChapterLengthValue
                                    }
                                ],
                                Value = OppoConstants.MovieLengthValue
                            }
                        },
                        Id = OppoConstants.ChapterOrMovieLengthKey,
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