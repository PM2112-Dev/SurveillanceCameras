using SurveillanceCameras.Domain.Common;

namespace SurveillanceCameras.Application.Common.Events;

/// <summary>Published when a security alert is triggered by a camera.</summary>
public sealed class AlertTriggeredIntegrationEvent : IntegrationEvent
{
    public AlertTriggeredIntegrationEvent(int cameraId, string cameraCode, string alertType, string severity, string? description)
    {
        CameraId = cameraId;
        CameraCode = cameraCode;
        AlertType = alertType;
        Severity = severity;
        Description = description;
    }

    public int CameraId { get; init; }
    public string CameraCode { get; init; }
    public string AlertType { get; init; }

    /// <summary>Low / Medium / High / Critical</summary>
    public string Severity { get; init; }

    public string? Description { get; init; }
}

