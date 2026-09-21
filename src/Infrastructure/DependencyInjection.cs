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
using SurveillanceCameras.Application.Service.TikTokSession;
// using SurveillanceCameras.Application.WikiDichService;
using SurveillanceCameras.Infrastructure.Data;
using SurveillanceCameras.Infrastructure.Data.Interceptors;
using SurveillanceCameras.Infrastructure.Identity;
using SurveillanceCameras.Infrastructure.Kafka;
using SurveillanceCameras.Infrastructure.Repository;
using SurveillanceCameras.Infrastructure.Service.TikTokApi;
using SurveillanceCameras.Infrastructure.Service.TikTokSession;
// using SurveillanceCameras.Infrastructure.Service.WikiDichService;

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

        // builder.Services.AddHttpClient<IWikiDichApiService, WikiDichApiService>();

        builder.Services.AddScoped<WikidichzzzRepository>();
        builder.Services.AddScoped<WikidichRepository>();
        builder.Services.AddScoped<WikicvRepository>();
        builder.Services.AddScoped<ICrawlStoryRepositoryFactory, CrawlStoryRepositoryFactory>();

        builder.Services.Configure<TikTokSessionOptions>(builder.Configuration.GetSection(TikTokSessionOptions.SectionName));
        builder.Services.AddSingleton<TikTokSessionManager>();
        builder.Services.AddSingleton<ITikTokSessionManager>(sp => sp.GetRequiredService<TikTokSessionManager>());

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
