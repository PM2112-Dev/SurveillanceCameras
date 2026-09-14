using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Infrastructure.Data;
using SurveillanceCameras.Web.Infrastructure;
using SurveillanceCameras.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUser>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        builder.Services.AddProblemDetails();

        // DEV-ONLY: auto-authenticate as the seeded Administrator when no Bearer token is sent,
        // so testing via Scalar doesn't require pasting a JWT each time. Requests that DO send a
        // real token still go through normal JWT validation. Never registered outside Development.
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddAuthentication(options =>
                {
                    // DefaultAuthenticateScheme/DefaultChallengeScheme take precedence over
                    // DefaultScheme when non-null — AddInfrastructureServices() already set both
                    // to "Bearer", so both must be overridden here, not just DefaultScheme.
                    options.DefaultScheme = "DevOrJwt";
                    options.DefaultAuthenticateScheme = "DevOrJwt";
                    options.DefaultChallengeScheme = "DevOrJwt";
                })
                .AddScheme<AuthenticationSchemeOptions, DevAutoAuthenticationHandler>(
                    DevAutoAuthenticationHandler.SchemeName, _ => { })
                .AddPolicyScheme("DevOrJwt", "DevOrJwt", options =>
                {
                    options.ForwardDefaultSelector = context =>
                        context.Request.Headers.ContainsKey("Authorization")
                            ? JwtBearerDefaults.AuthenticationScheme
                            : DevAutoAuthenticationHandler.SchemeName;
                });
        }

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<JwtBearerOpenApiDocumentTransformer>();
            options.AddOperationTransformer<ApiExceptionOperationTransformer>();
            options.AddOperationTransformer<IdentityApiOperationTransformer>();
        });

        builder.Services.AddCors();
    }

    public static void AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }
    }
}
