using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

namespace UnfoldedCircle.Models.Events;

[JsonConverter(typeof(RemoteFeaturesJsonConverter))]
public enum RemoteFeature
{
    [Display(Name = "send_cmd")]
    SendCmd,

    [Display(Name = "on_off")]
    OnOff,

    [Display(Name = "toggle")]
    Toggle
}

[EnumJsonConverter(typeof(RemoteFeature), CaseSensitive = false, PropertyName = "features")]
public partial class RemoteFeaturesJsonConverter;