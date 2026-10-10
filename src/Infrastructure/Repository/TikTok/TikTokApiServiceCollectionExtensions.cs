using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Repository.TikTok;

namespace SurveillanceCameras.Infrastructure.Repository.TikTok;

public static class TikTokApiServiceCollectionExtensions
{
    private const string HttpClientName = "TikTokApi";

    public static IServiceCollection AddTikTokApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TikTokApiOptions>(configuration.GetSection(TikTokApiOptions.SectionName));

        services.AddHttpClient(HttpClientName, (sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<TikTokApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            // Cookie được gán thủ công vào từng request; tắt cookie container để handler không ghi đè.
            // HttpClient bị singleton giữ suốt vòng đời nên tự xoay vòng kết nối để còn nhận thay đổi DNS.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                UseCookies = false,
                AutomaticDecompression = System.Net.DecompressionMethods.All,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            });

        // Singleton: giữ một trình duyệt Chrome headless dùng chung, mỗi request mở một context riêng.
        services.AddSingleton<ITikTokRepository>(sp => new TikTokRepository(
            sp.GetRequiredService<IOptions<TikTokApiOptions>>(),
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName)));

        return services;
    }
}
