namespace UnfoldedCircle.OppoBluRay.OppoEntity;

internal static class OppoConstants
{
    internal const string IpAddressKey = "ip_address";
    internal const string DeviceIdKey = "device_id";
    internal const string OppoModelKey = "oppo_model";
    internal const string EntityName = "entity_name";
    internal const string UseMediaEventsKey = "use_media_events";
    internal const string ChapterLengthValue = "chapter_length";
    internal const string MovieLengthValue = "movie_length";
    internal const string ChapterOrMovieLengthKey = "chapter_or_movie_length";
    internal const string MaxMessageHandlingWaitTimeInSecondsKey = "max_message_handling_wait_time_in_seconds";

    internal const string IpAddressRegex = @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)|(([0-9a-fA-F]{1,4}:)" +
                                           "{7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}" +
                                           "(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4})" +
                                           "{1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|" +
                                           @"fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|" +
                                           @"(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9])" +
                                           "{0,1}[0-9]))$";
    
    internal static class InputSource
    {
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
    }
}