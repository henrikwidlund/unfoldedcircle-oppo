namespace UnfoldedCircle.OppoBluRay.OppoEntity;

internal static class OppoConstants
{
    internal const string IpAddressKey = "ip_address";
    internal const string MacAddressKey = "mac_address";
    internal const string OppoModelKey = "oppo_model";
    internal const string EntityName = "entity_name";
    internal const string UseMediaEventsKey = "use_media_events";
    internal const string UseStreamingEventsKey = "use_streaming_events";
    internal const string MaxMessageHandlingWaitTimeInSecondsKey = "max_message_handling_wait_time_in_seconds";
    internal const string DiscoveredPlayerKey = "discovered_player";
    internal const string ManualEntryValue = "manual_entry";
    internal const string SerialBridgeEntryValue = "serial_bridge_entry";
    internal const string SerialBridgeInfoKey = "serial_bridge_info";
    internal const string SerialBridgeHostKey = "serial_bridge_host";
    internal const string SerialBridgePortKey = "serial_bridge_port";

    // Second serial port of a UC Dock 3. Deliberately not the first (4999): that UART carries the
    // dock's own boot messages, which the player would read as commands.
    internal const ushort DefaultSerialBridgePort = 5000;

    internal const string InputSourceSelectSuffix = "InputSource";

    internal const string IpAddressRegex = @"^(?:(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|(([0-9a-fA-F]{1,4}:)" +
                                           "{7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}" +
                                           "(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4})" +
                                           "{1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|" +
                                           @"fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|" +
                                           @"(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9])" +
                                           "{0,1}[0-9])))$";
    internal const string MacAddressRegex = "^([0-9a-fA-F]{2}[:-]){5}([0-9a-fA-F]{2})$|^$";

    // Looser than IpAddressRegex: a bridge is as often addressed by name (a dock's mDNS name, DHCP
    // hostname) as by literal address. Labels of digits make this accept IPv4 too. IPv6 literals are
    // not accepted: the entity id is host:port, which has no unambiguous reading for one. Address a
    // bridge on an IPv6-only network by name.
    internal const string HostNameRegex = @"^(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)(?:\.[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?)*\.?$";

    internal static class InputSource
    {
        // ReSharper disable InconsistentNaming
        internal const string BluRayPlayer = "Blu-Ray Player";
        internal const string HDMIFront = "HDMI/MHL IN-FRONT";
        internal const string HDMIBack = "HDMI IN-BACK";
        internal const string ARCHDMIOut1 = "ARC on HDMI OUT1";
        internal const string ARCHDMIOut2 = "ARC on HDMI OUT2";
        internal const string Optical = "OPTICAL IN";
        internal const string Coaxial = "COAXIAL IN";
        internal const string USBAudio = "USB AUDIO IN";
        internal const string HDMIIn = "HDMI IN";
        internal const string ARCHDMIOut = "ARC: HDMI OUT";
        // ReSharper restore InconsistentNaming
    }
}
