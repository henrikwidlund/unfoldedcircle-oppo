namespace UnfoldedCircle.Server.Oppo;

internal static class OppoConstants
{
    internal const string DriverName = "Oppo Blu-ray Player";
    internal const string DriverDescription = "Integration for the Oppo Blu-ray players";
    internal const string DriverId = "oppo-unfolded-circle";
    internal const string DriverVersion = "0.0.3";
    internal const string DriverDeveloper = "Henrik Widlund";
    internal const string DriverEmail = "07online_rodeo@icloud.com";
    internal static readonly DateOnly DriverReleaseDate = new(2024, 10, 01);
    internal static readonly Uri DriverUrl = new("https://github.com/henrikwidlund/unfoldedcircle-oppo");
    internal const string DeviceName = DriverName;
    internal const string EntityId = "0393caf1-c9d2-422e-88b5-cb716756334a";
    
    internal const string IpAddressKey = "ip_address";
    internal const string DeviceIdKey = "device_id";
    internal const string OppoModelKey = "oppo_model";
    internal const string UseMediaEventsKey = "use_media_events";
    internal const string MovieLengthValue = "movie_length";
    internal const string ChapterLengthValue = "chapter_length";
    internal const string ChapterOrMovieLengthKey = "chapter_or_movie_length";
    
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