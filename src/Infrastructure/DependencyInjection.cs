using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repository;
using SurveillanceCameras.Application.Repository.CrawlStory;
using SurveillanceCameras.Application.Repository.AccountSession;
using SurveillanceCameras.Application.Repository.AI;
using SurveillanceCameras.Application.Repository.TikTok;
using SurveillanceCameras.Infrastructure.Data;
using SurveillanceCameras.Infrastructure.Data.Interceptors;
using SurveillanceCameras.Infrastructure.Identity;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Repository;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;
using SurveillanceCameras.Infrastructure.Repository.CrawlStory.WikiDich;
using SurveillanceCameras.Infrastructure.Repository.AccountSession;
using SurveillanceCameras.Infrastructure.Repository.Tyt;
using SurveillanceCameras.Infrastructure.Repository.Media;
using SurveillanceCameras.Infrastructure.BackgroundJobs;
using SurveillanceCameras.Application.Repository.Media;
using SurveillanceCameras.Application.Repository.Tyt;
using SurveillanceCameras.Infrastructure.Repository.AI;
using SurveillanceCameras.Infrastructure.Repository.TikTok;

namespace SurveillanceCameras.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(Services.Database);
        Guard.Against.Null(connectionString, message: $"Connection string '{Services.Database}' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, OutboxInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddHttpClient(ConfigurableCrawlStoryRepository.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                // Nhiều trang truyện (sau Cloudflare) chặn request không giống trình duyệt.
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0 Safari/537.36");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "vi-VN,vi;q=0.9,en;q=0.8");
            })
            // Tự theo redirect trong repository để kiểm tra host ở từng bước; cookie gắn thủ công theo tài khoản.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                AutomaticDecompression = System.Net.DecompressionMethods.All
            });
        builder.Services.AddScoped<ConfigurableCrawlStoryRepository>();

        // Bỏ resilience mặc định của ServiceDefaults (AddStandardResilienceHandler: tổng 30 giây/request, tự thử lại cả POST)
        // cho client gọi AI: sinh nội dung chương có thể lâu hơn 30 giây, và thử lại POST là trả tiền AI thêm lần nữa —
        // khi lỗi, AIPromptRunner đã tự chuyển sang API key khác.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers còn ở dạng thử nghiệm
        builder.Services.AddHttpClient(AIModelRepository.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(20))
            .RemoveAllResilienceHandlers();
        builder.Services.AddScoped<IAIModelRepository, AIModelRepository>();
        // Sinh văn bản có thể lâu (chương dài, model có "thinking"), nên timeout dài hơn lấy danh sách model.
        builder.Services.AddHttpClient(AITextRepository.HttpClientName, client => client.Timeout = TimeSpan.FromMinutes(5))
            .RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001
        builder.Services.AddScoped<IAITextRepository, AITextRepository>();
        builder.Services.AddScoped<WikidichzzzRepository>();
        builder.Services.AddScoped<WikidichRepository>();
        builder.Services.AddScoped<WikicvRepository>();
        builder.Services.AddScoped<ICrawlStoryRepositoryFactory, CrawlStoryRepositoryFactory>();

        builder.Services.Configure<AccountSessionOptions>(builder.Configuration.GetSection(AccountSessionOptions.SectionName));
        builder.Services.AddSingleton<AccountSessionManagerRepository>();
        builder.Services.AddSingleton<IAccountSessionManagerRepository>(sp => sp.GetRequiredService<AccountSessionManagerRepository>());
        builder.Services.AddScoped<ITytRepository, TytRepository>();

        builder.Services.Configure<MediaOptions>(builder.Configuration.GetSection(MediaOptions.SectionName));
        builder.Services.AddHttpClient(LocalMediaRepository.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0 Safari/537.36");
        });
        builder.Services.AddScoped<IMediaRepository, LocalMediaRepository>();

        builder.Services.Configure<MovieDailySyncOptions>(builder.Configuration.GetSection(MovieDailySyncOptions.SectionName));
        builder.Services.AddHostedService<MovieDailySyncService>();

        builder.Services.AddTikTokApi(builder.Configuration);

        var jwtSection = builder.Configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var signingKey = jwtSection["SigningKey"];

        Guard.Against.NullOrWhiteSpace(issuer, message: "Jwt:Issuer is not configured.");
        Guard.Against.NullOrWhiteSpace(audience, message: "Jwt:Audience is not configured.");
        Guard.Against.NullOrWhiteSpace(signingKey, message: "Jwt:SigningKey is not configured.");
        if (signingKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters long.");
        }

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        builder.Services.AddAuthorizationBuilder();

        builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddApiEndpoints();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();

        builder.AddKafkaServices();
    }
}
