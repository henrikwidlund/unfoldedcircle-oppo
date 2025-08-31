using System.Collections.Frozen;

using Oppo;

using UnfoldedCircle.Models.Events;
using UnfoldedCircle.Models.Sync;

namespace UnfoldedCircle.OppoBluRay.OppoEntity;

public static class OppoEntitySettings
{
    public static readonly FrozenSet<MediaPlayerEntityFeature> MediaPlayerEntityFeatures = new[]
    {
        MediaPlayerEntityFeature.OnOff,
        MediaPlayerEntityFeature.Toggle,
        MediaPlayerEntityFeature.Volume,
        MediaPlayerEntityFeature.VolumeUpDown,
        MediaPlayerEntityFeature.MuteToggle,
        MediaPlayerEntityFeature.PlayPause,
        MediaPlayerEntityFeature.Stop,
        MediaPlayerEntityFeature.Next,
        MediaPlayerEntityFeature.Previous,
        MediaPlayerEntityFeature.FastForward,
        MediaPlayerEntityFeature.Rewind,
        MediaPlayerEntityFeature.Repeat,
        MediaPlayerEntityFeature.Shuffle,
        MediaPlayerEntityFeature.Seek,
        MediaPlayerEntityFeature.MediaDuration,
        MediaPlayerEntityFeature.MediaPosition,
        MediaPlayerEntityFeature.MediaTitle,
        MediaPlayerEntityFeature.MediaArtist,
        MediaPlayerEntityFeature.MediaAlbum,
        MediaPlayerEntityFeature.MediaImageUrl,
        MediaPlayerEntityFeature.MediaType,
        MediaPlayerEntityFeature.Dpad,
        MediaPlayerEntityFeature.Numpad,
        MediaPlayerEntityFeature.Home,
        MediaPlayerEntityFeature.Menu,
        MediaPlayerEntityFeature.ContextMenu,
        MediaPlayerEntityFeature.Info,
        MediaPlayerEntityFeature.ColorButtons,
        MediaPlayerEntityFeature.ChannelSwitcher,
        MediaPlayerEntityFeature.SelectSource,
        MediaPlayerEntityFeature.OpenClose,
        MediaPlayerEntityFeature.AudioTrack,
        MediaPlayerEntityFeature.Subtitle,
        MediaPlayerEntityFeature.Settings
    }.ToFrozenSet();

    public static readonly FrozenSet<string> MediaPlayerSimpleCommands =
    [
        EntitySettingsConstants.Dimmer,
        EntitySettingsConstants.PureAudioToggle,
        EntitySettingsConstants.Clear,
        EntitySettingsConstants.InfoToggle,
        EntitySettingsConstants.TopMenu,
        EntitySettingsConstants.PopUpMenu,
        EntitySettingsConstants.Pause,
        EntitySettingsConstants.Play,
        EntitySettingsConstants.Angle,
        EntitySettingsConstants.Zoom,
        EntitySettingsConstants.SecondaryAudioProgram,
        EntitySettingsConstants.AbReplay,
        EntitySettingsConstants.PictureInPicture,
        EntitySettingsConstants.Resolution,
        EntitySettingsConstants.SubtitleHold,
        EntitySettingsConstants.Option,
        EntitySettingsConstants.ThreeD,
        EntitySettingsConstants.PictureAdjustment,
        EntitySettingsConstants.Hdr,
        EntitySettingsConstants.InfoHold,
        EntitySettingsConstants.ResolutionHold,
        EntitySettingsConstants.AvSync,
        EntitySettingsConstants.GaplessPlay
    ];

    private static readonly FrozenSet<string> RemoteSimpleCommands = [
        MediaPlayerCommandIdConstants.PlayPause,
        MediaPlayerCommandIdConstants.Stop,
        RemoteButtonConstants.Previous,
        RemoteButtonConstants.Next,
        MediaPlayerCommandIdConstants.FastForward,
        MediaPlayerCommandIdConstants.Rewind,
        RemoteButtonConstants.VolumeUp,
        RemoteButtonConstants.VolumeDown,
        RemoteButtonConstants.Mute,
        MediaPlayerCommandIdConstants.Repeat,
        RemoteButtonConstants.ChannelUp,
        RemoteButtonConstants.ChannelDown,
        RemoteButtonConstants.DpadUp,
        RemoteButtonConstants.DpadDown,
        RemoteButtonConstants.DpadLeft,
        RemoteButtonConstants.DpadRight,
        RemoteButtonConstants.DpadMiddle,
        MediaPlayerCommandIdConstants.Digit0,
        MediaPlayerCommandIdConstants.Digit1,
        MediaPlayerCommandIdConstants.Digit2,
        MediaPlayerCommandIdConstants.Digit3,
        MediaPlayerCommandIdConstants.Digit4,
        MediaPlayerCommandIdConstants.Digit5,
        MediaPlayerCommandIdConstants.Digit6,
        MediaPlayerCommandIdConstants.Digit7,
        MediaPlayerCommandIdConstants.Digit8,
        MediaPlayerCommandIdConstants.Digit9,
        RemoteButtonConstants.Red,
        RemoteButtonConstants.Green,
        RemoteButtonConstants.Yellow,
        RemoteButtonConstants.Blue,
        RemoteButtonConstants.Home,
        MediaPlayerCommandIdConstants.ContextMenu,
        MediaPlayerCommandIdConstants.Info,
        RemoteButtonConstants.Back,
        MediaPlayerCommandIdConstants.OpenClose,
        MediaPlayerCommandIdConstants.Subtitle,
        MediaPlayerCommandIdConstants.Settings,
        RemoteButtonConstants.Power,
        MediaPlayerCommandIdConstants.AudioTrack,

        EntitySettingsConstants.Dimmer,
        EntitySettingsConstants.PureAudioToggle,
        EntitySettingsConstants.Clear,
        EntitySettingsConstants.TopMenu,
        EntitySettingsConstants.PopUpMenu,
        EntitySettingsConstants.Pause,
        EntitySettingsConstants.Play,
        EntitySettingsConstants.Angle,
        EntitySettingsConstants.Zoom,
        EntitySettingsConstants.SecondaryAudioProgram,
        EntitySettingsConstants.AbReplay,
        EntitySettingsConstants.PictureInPicture,
        EntitySettingsConstants.Resolution,
        EntitySettingsConstants.SubtitleHold,
        EntitySettingsConstants.Option,
        EntitySettingsConstants.ThreeD,
        EntitySettingsConstants.PictureAdjustment,
        EntitySettingsConstants.Hdr,
        EntitySettingsConstants.InfoHold,
        EntitySettingsConstants.ResolutionHold,
        EntitySettingsConstants.AvSync,
        EntitySettingsConstants.GaplessPlay
    ];

    public static readonly FrozenDictionary<OppoModel, string[]> SourceList = new Dictionary<OppoModel, string[]>
    {
        [OppoModel.BDP10X] =
        [
            OppoConstants.InputSource.BluRayPlayer,
            OppoConstants.InputSource.HDMIFront,
            OppoConstants.InputSource.HDMIBack,
            OppoConstants.InputSource.ARCHDMIOut1,
            OppoConstants.InputSource.ARCHDMIOut2,
            OppoConstants.InputSource.Optical,
            OppoConstants.InputSource.Coaxial,
            OppoConstants.InputSource.USBAudio
        ],
        [OppoModel.UDP203] =
        [
            OppoConstants.InputSource.BluRayPlayer,
            OppoConstants.InputSource.HDMIIn,
            OppoConstants.InputSource.ARCHDMIOut
        ],
        [OppoModel.UDP205] =
        [
            OppoConstants.InputSource.BluRayPlayer,
            OppoConstants.InputSource.HDMIIn,
            OppoConstants.InputSource.ARCHDMIOut,
            OppoConstants.InputSource.Optical,
            OppoConstants.InputSource.Coaxial,
            OppoConstants.InputSource.USBAudio
        ]
    }.ToFrozenDictionary();
    
    public static readonly FrozenDictionary<string, InputSource> SourceMap = new Dictionary<string, InputSource>(StringComparer.OrdinalIgnoreCase)
    {
        [OppoConstants.InputSource.BluRayPlayer] = InputSource.BluRayPlayer,
        [OppoConstants.InputSource.HDMIFront] = InputSource.HDMIIn,
        [OppoConstants.InputSource.HDMIBack] = InputSource.HDMIIn,
        [OppoConstants.InputSource.ARCHDMIOut1] = InputSource.ARCHDMIOut,
        [OppoConstants.InputSource.ARCHDMIOut2] = InputSource.ARCHDMIOut,
        [OppoConstants.InputSource.Optical] = InputSource.Optical,
        [OppoConstants.InputSource.Coaxial] = InputSource.Coaxial,
        [OppoConstants.InputSource.USBAudio] = InputSource.USBAudio,
        [OppoConstants.InputSource.HDMIIn] = InputSource.HDMIIn,
        [OppoConstants.InputSource.ARCHDMIOut] = InputSource.ARCHDMIOut
    }.ToFrozenDictionary();

    public static readonly FrozenSet<RemoteFeature> RemoteFeatures =
    [
        RemoteFeature.OnOff,
        RemoteFeature.SendCmd,
        RemoteFeature.Toggle
    ];

    public static readonly RemoteOptions RemoteOptions = new()
    {
        ButtonMapping =
        [
            new DeviceButtonMapping { Button = RemoteButton.Home, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.Home } },
            new DeviceButtonMapping { Button = RemoteButton.Back, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.Back } },
            new DeviceButtonMapping { Button = RemoteButton.DpadUp, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.DpadUp } },
            new DeviceButtonMapping { Button = RemoteButton.DpadDown, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.DpadDown } },
            new DeviceButtonMapping { Button = RemoteButton.DpadLeft, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.DpadLeft } },
            new DeviceButtonMapping { Button = RemoteButton.DpadRight, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.DpadRight } },
            new DeviceButtonMapping { Button = RemoteButton.DpadMiddle, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.DpadMiddle } },
            new DeviceButtonMapping { Button = RemoteButton.ChannelUp, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.ChannelUp } },
            new DeviceButtonMapping { Button = RemoteButton.ChannelDown, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.ChannelDown } },
            new DeviceButtonMapping { Button = RemoteButton.VolumeUp, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.VolumeUp } },
            new DeviceButtonMapping { Button = RemoteButton.VolumeDown, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.VolumeDown } },
            new DeviceButtonMapping { Button = RemoteButton.Power, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.Power } },
            new DeviceButtonMapping { Button = RemoteButton.Mute, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.Mute } },
            new DeviceButtonMapping { Button = RemoteButton.Green, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.Green } },
            new DeviceButtonMapping { Button = RemoteButton.Yellow, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.Yellow } },
            new DeviceButtonMapping { Button = RemoteButton.Red, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.Red } },
            new DeviceButtonMapping { Button = RemoteButton.Blue, ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.Blue } },
            new DeviceButtonMapping
            {
                Button = RemoteButton.Previous,
                ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.Previous },
                LongPress = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Rewind }
            },
            new DeviceButtonMapping
            {
                Button = RemoteButton.Play,
                ShortPress = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.PlayPause },
                LongPress = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Stop }
            },
            new DeviceButtonMapping
            {
                Button = RemoteButton.Next,
                ShortPress = new EntityCommand { CmdId = RemoteButtonConstants.Next },
                LongPress = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.FastForward }
            }
        ],
        SimpleCommands = RemoteSimpleCommands,
        UserInterface = new UserInterface
        {
            Pages =
            [
                new UserInterfacePage
                {
                    PageId = "uc_oppo_general",
                    Name = "General",
                    Grid = new Grid { Height = 6, Width = 4 },
                    Items =
                    [
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "Option",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.Option },
                            Location = new GridLocation { X = 0, Y = 0 },
                            Size = new GridItemSize { Height = 1, Width = 2 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Icon,
                            Icon = "uc:circle-info",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.InfoToggle },
                            Location = new GridLocation { X = 3, Y = 0 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "Subtitle Hold",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.SubtitleHold },
                            Location = new GridLocation { X = 0, Y = 1 },
                            Size = new GridItemSize { Height = 1, Width = 2 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "Info Hold",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.InfoHold },
                            Location = new GridLocation { X = 2, Y = 1 },
                            Size = new GridItemSize { Height = 1, Width = 2 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "Pup-Up Menu",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.PopUpMenu },
                            Location = new GridLocation { X = 0, Y = 3 },
                            Size = new GridItemSize { Height = 1, Width = 2 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "Top Menu",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.TopMenu },
                            Location = new GridLocation { X = 2, Y = 3 },
                            Size = new GridItemSize { Height = 1, Width = 2 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Icon,
                            Icon = "uc:subtitles",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Subtitle },
                            Location = new GridLocation { X = 0, Y = 4 },
                            Size = new GridItemSize { Height = 1, Width = 2 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Icon,
                            Icon = "uc:language",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.AudioTrack },
                            Location = new GridLocation { X = 2, Y = 4 },
                            Size = new GridItemSize { Height = 1, Width = 2 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Icon,
                            Icon = "uc:lightbulb-on",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.Dimmer },
                            Location = new GridLocation { X = 0, Y = 5 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Icon,
                            Icon = "uc:eject",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.OpenClose },
                            Location = new GridLocation { X = 1, Y = 5 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Icon,
                            Icon = "uc:repeat",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Repeat },
                            Location = new GridLocation { X = 2, Y = 5 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Icon,
                            Icon = "uc:gear",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Settings },
                            Location = new GridLocation { X = 3, Y = 5 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        }
                    ]
                },
                new UserInterfacePage
                {
                    PageId = "uc_oppo_picture_audio",
                    Name = "Picture and Audio",
                    Grid = new Grid { Height = 7, Width = 4 },
                    Items =
                    [
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "PICTURE",
                            Location = new GridLocation { X = 0, Y = 0 },
                            Size = new GridItemSize { Height = 1, Width = 4 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "3D",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.ThreeD },
                            Location = new GridLocation { X = 0, Y = 1 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "HDR",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.Hdr },
                            Location = new GridLocation { X = 1, Y = 1 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "Picture Adjustment",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.PictureAdjustment },
                            Location = new GridLocation { X = 2, Y = 1 },
                            Size = new GridItemSize { Height = 1, Width = 2 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Icon,
                            Icon = "uc:magnifying-glass-plus",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.Zoom },
                            Location = new GridLocation { X = 0, Y = 2 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Icon,
                            Icon = "uc:angle",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.Angle },
                            Location = new GridLocation { X = 1, Y = 2 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "Resolution",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.Resolution },
                            Location = new GridLocation { X = 2, Y = 2 },
                            Size = new GridItemSize { Height = 1, Width = 2 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "Picture-in-Picture",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.PictureInPicture },
                            Location = new GridLocation { X = 0, Y = 3 },
                            Size = new GridItemSize { Height = 1, Width = 2 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "Resolution Hold",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.ResolutionHold },
                            Location = new GridLocation { X = 2, Y = 3 },
                            Size = new GridItemSize { Height = 1, Width = 2 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "AUDIO",
                            Location = new GridLocation { X = 0, Y = 5 },
                            Size = new GridItemSize { Height = 1, Width = 4 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "Pure Audio",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.PureAudioToggle },
                            Location = new GridLocation { X = 0, Y = 6 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "A/V Sync",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.AvSync },
                            Location = new GridLocation { X = 1, Y = 6 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "Secondary Audio Program",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.SecondaryAudioProgram },
                            Location = new GridLocation { X = 2, Y = 6 },
                            Size = new GridItemSize { Height = 1, Width = 2 }
                        }
                    ]
                },
                new UserInterfacePage
                {
                    PageId = "uc_oppo_numpad",
                    Name = "Numpad",
                    Grid = new Grid { Height = 4, Width = 3 },
                    Items =
                    [
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "1",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Digit1 },
                            Location = new GridLocation { X = 0, Y = 0 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "2",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Digit2 },
                            Location = new GridLocation { X = 1, Y = 0 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "3",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Digit3 },
                            Location = new GridLocation { X = 2, Y = 0 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },

                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "4",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Digit4 },
                            Location = new GridLocation { X = 0, Y = 1 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "5",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Digit5 },
                            Location = new GridLocation { X = 1, Y = 1 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "6",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Digit6 },
                            Location = new GridLocation { X = 2, Y = 1 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },

                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "7",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Digit7 },
                            Location = new GridLocation { X = 0, Y = 2 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "8",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Digit8 },
                            Location = new GridLocation { X = 1, Y = 2 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "9",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Digit9 },
                            Location = new GridLocation { X = 2, Y = 2 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Text,
                            Text = "0",
                            Command = new EntityCommand { CmdId = MediaPlayerCommandIdConstants.Digit0 },
                            Location = new GridLocation { X = 1, Y = 3 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        },
                        new UserInterfaceItem
                        {
                            Type = UserInterfaceItemType.Icon,
                            Icon = "uc:delete-left",
                            Command = new EntityCommand { CmdId = EntitySettingsConstants.Clear },
                            Location = new GridLocation { X = 2, Y = 3 },
                            Size = new GridItemSize { Height = 1, Width = 1 }
                        }
                    ]
                }
            ]
        }
    };
}