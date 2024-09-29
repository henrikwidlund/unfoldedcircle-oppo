namespace UnfoldedCircle.Models.Events;

public record IntegrationSetupError
{
    [JsonPropertyName("error")]
    public required IntegrationSetupErrorCode Error { get; init; }
}