using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

namespace UnfoldedCircle.Models.Shared;

[JsonConverter(typeof(EntityTypeJsonConverter))]
public enum EntityType
{
    [Display(Name = "cover")]
    Cover = 1,
    
    [Display(Name = "button")]
    Button,
    
    [Display(Name = "climate")]
    Climate,
    
    [Display(Name = "light")]
    Light,
    
    [Display(Name = "media_player")]
    MediaPlayer,

    [Display(Name = "remote")]
    Remote,
    
    [Display(Name = "sensor")]
    Sensor,
    
    [Display(Name = "switch")]
    Switch
}

[EnumJsonConverter(typeof(EntityType), CaseSensitive = false, PropertyName = "entity_type")]
public partial class EntityTypeJsonConverter;