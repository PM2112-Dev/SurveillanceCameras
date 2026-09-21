using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Service.TikTokApi;
using SurveillanceCameras.Application.TikTokApi;

namespace SurveillanceCameras.Infrastructure.Service.TikTokApi;

public static class TikTokApiServiceCollectionExtensions
{
    public static IServiceCollection AddTikTokApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TikTokApiOptions>(configuration.GetSection(TikTokApiOptions.SectionName));

        services.AddHttpClient<ITikTokApiService, TikTokApiService>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<TikTokApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            // Cookie được gán thủ công vào từng request; tắt cookie container để handler không ghi đè.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                UseCookies = false,
                AutomaticDecompression = System.Net.DecompressionMethods.All
            });

        // Singleton: giữ một trình duyệt Chrome headless dùng chung, mỗi request mở một context riêng.
        services.AddSingleton<ITikTokProfileService, TikTokProfileService>();

        return services;
    }
}
