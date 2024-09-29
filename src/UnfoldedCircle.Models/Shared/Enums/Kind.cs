using System.ComponentModel.DataAnnotations;
using UnfoldedCircle.Generators;

namespace UnfoldedCircle.Models.Shared;

[JsonConverter(typeof(KindJsonConverter))]
public enum Kind
{
    [Display(Name = "req")]
    Request = 1,
    
    [Display(Name = "resp")]
    Response,
    
    [Display(Name = "event")]
    Event
}

[EnumJsonConverter(typeof(Kind), CaseSensitive = false, PropertyName = "kind")]
public partial class KindJsonConverter;