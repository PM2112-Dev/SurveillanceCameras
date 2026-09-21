using Microsoft.Extensions.Configuration;

namespace SurveillanceCameras.Infrastructure.IntegrationTests;

/// <summary>
/// Cấu hình cho test chạy với hạ tầng thật: dùng đúng appsettings của project Web
/// (connection string tới DB thật), có thể ghi đè bằng biến môi trường.
/// </summary>
public static class TestEnvironment
{
    public static IConfiguration Configuration { get; } = Build();

    public static string ConnectionString =>
        Configuration.GetConnectionString("SurveillanceCamerasDb")
        ?? throw new InvalidOperationException("Không tìm thấy ConnectionStrings:SurveillanceCamerasDb.");

    private static IConfiguration Build()
    {
        var webDir = Path.Combine(FindRepositoryRoot(), "src", "Web");

        return new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(webDir, "appsettings.json"), optional: false)
            .AddJsonFile(Path.Combine(webDir, "appsettings.Development.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "SurveillanceCameras.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new DirectoryNotFoundException("Không tìm thấy thư mục gốc repo (SurveillanceCameras.slnx).");
    }
}
