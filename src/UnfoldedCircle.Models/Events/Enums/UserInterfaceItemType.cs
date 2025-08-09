using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

namespace UnfoldedCircle.Models.Events;

[JsonConverter(typeof(UserInterfaceItemTypeJsonConverter))]
public enum UserInterfaceItemType
{
    [Display(Name = "icon")]
    Icon,

    [Display(Name = "text")]
    Text,

    [Display(Name = "numpad")]
    Numpad
}

[EnumJsonConverter(typeof(UserInterfaceItemType), CaseSensitive = false, PropertyName = "type")]
public partial class UserInterfaceItemTypeJsonConverter;