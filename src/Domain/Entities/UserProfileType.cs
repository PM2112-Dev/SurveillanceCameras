namespace SurveillanceCameras.Domain.Entities;

public class UserProfileType : BaseAuditableEntity
{
    public ICollection<UserProfile>? UserProfiles { get; set; }
}
