using SurveillanceCameras.Domain.Common;

namespace SurveillanceCameras.Application.Common.Events;

/// <summary>Published when a camera's status changes (online/offline/error).</summary>
public sealed class CameraStatusChangedIntegrationEvent : IntegrationEvent
{
    public CameraStatusChangedIntegrationEvent(int cameraId, string cameraCode, string newStatus, string? areaCode)
    {
        CameraId = cameraId;
        CameraCode = cameraCode;
        NewStatus = newStatus;
        AreaCode = areaCode;
    }

    public int CameraId { get; init; }
    public string CameraCode { get; init; }
    public string NewStatus { get; init; }
    public string? AreaCode { get; init; }
}

