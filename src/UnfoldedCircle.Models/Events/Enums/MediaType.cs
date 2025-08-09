using System.ComponentModel.DataAnnotations;
using Theodicean.SourceGenerators;

namespace UnfoldedCircle.Models.Events;

[JsonConverter(typeof(MediaTypeJsonConverter))]
public enum MediaType
{
    [Display(Name = "MUSIC")]
    Music,
    
    [Display(Name = "RADIO")]
    Radio,
    
    [Display(Name = "TVSHOW")]
    TvShow,
    
    [Display(Name = "MOVIE")]
    Movie,
    
    [Display(Name = "VIDEO")]
    Video
}

[EnumJsonConverter(typeof(MediaType), CaseSensitive = false, PropertyName = "media_type")]
public partial class MediaTypeJsonConverter;