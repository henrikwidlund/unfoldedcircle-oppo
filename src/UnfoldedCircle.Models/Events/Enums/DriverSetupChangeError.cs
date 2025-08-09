using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

namespace UnfoldedCircle.Models.Events;

[JsonConverter(typeof(DriverSetupChangeErrorJsonConverter))]
public enum DriverSetupChangeError
{
    [Display(Name = "NONE")]
    None = 1,
    
    [Display(Name = "NOT_FOUND")]
    NotFound,
    
    [Display(Name = "CONNECTION_REFUSED")]
    ConnectionRefused,
    
    [Display(Name = "AUTHORIZATION_ERROR")]
    AuthorizationError,
    
    [Display(Name = "TIMEOUT")]
    Timeout,
    
    [Display(Name = "OTHER")]
    Other
}

[EnumJsonConverter(typeof(DriverSetupChangeError), CaseSensitive = false, PropertyName = "error")]
public partial class DriverSetupChangeErrorJsonConverter;