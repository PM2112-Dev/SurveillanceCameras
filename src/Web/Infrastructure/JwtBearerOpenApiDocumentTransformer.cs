using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SurveillanceCameras.Web.Infrastructure;

internal sealed class JwtBearerOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    internal const string BearerSchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[BearerSchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Name = "Authorization",
            Description = "Enter JWT access token."
        };

        return Task.CompletedTask;
    }
}

