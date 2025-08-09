using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

namespace UnfoldedCircle.Models.Sync;

[JsonConverter(typeof(RemoteEntityAttributeJsonConverter))]
public enum RemoteEntityAttribute
{
    /// <summary>
    /// State of the controlled device, it's either on or off.
    /// </summary>
    [Display(Name = "state")]
    State = 1
}

[EnumJsonConverter(typeof(RemoteEntityAttribute), CaseSensitive = false, PropertyName = "attributes")]
public partial class RemoteEntityAttributeJsonConverter;