namespace SurveillanceCameras.Infrastructure.Repository.VideoStorage;

public class R2VideoStorageOptions
{
    public const string SectionName = "R2VideoStorage";

    /// <summary>Endpoint S3 của R2; <c>{0}</c> là Account ID của Cloudflare.</summary>
    public string EndpointTemplate { get; set; } = "https://{0}.r2.cloudflarestorage.com";

    /// <summary>Thời gian tối đa cho một lần tải file lên (giây).</summary>
    public int UploadTimeoutSeconds { get; set; } = 600;
}
