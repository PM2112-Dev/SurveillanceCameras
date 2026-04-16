using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SurveillanceCameras.Web.Infrastructure;

/// <summary>
/// Adds human-readable summaries and descriptions to identity endpoints.
/// </summary>
internal sealed class IdentityApiOperationTransformer : IOpenApiOperationTransformer
{
    private static readonly Dictionary<string, (string Summary, string Description)> _metadata = new()
    {
        ["api/Users/register"] = ("Register", "Creates a new user account with email and password."),
        ["api/Users/login"] = ("Log in", "Authenticates a user and returns a JWT access token."),
        ["api/Users/refresh"] = ("Refresh token", "Returns a new JWT access token and rotates the refresh token."),
        ["api/Users/manage/info GET"] = ("Get account info", "Returns basic information for the current authenticated user."),
        ["api/Users/logout"] = ("Log out", "Revokes the current user's refresh token and clears local auth state on the client."),
    };

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var path = context.Description.RelativePath ?? "";
        var key = $"{path} {context.Description.HttpMethod?.ToUpperInvariant()}";

        if (_metadata.TryGetValue(key, out var meta) || _metadata.TryGetValue(path, out meta))
        {
            operation.Summary = meta.Summary;
            operation.Description = meta.Description;
        }

        return Task.CompletedTask;
    }
}
