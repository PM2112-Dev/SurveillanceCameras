namespace SurveillanceCameras.Application.Local.WebSources.IntegrationEvents;

public sealed class FetchStoryWebSourceIntegrationEvent : IntegrationEvent
{
    public int WebSourceId { get; init; }
    
    public string? LinkRaw { get; init; }
}
