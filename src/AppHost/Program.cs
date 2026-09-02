using SurveillanceCameras.Shared;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca-env");

// Locally, connect straight to the PostgreSQL instance already running on the machine
// (see ConnectionStrings:SurveillanceCamerasDb) instead of spinning up a Docker container.
// When publishing, provision a real Azure Postgres Flexible Server.
IResourceBuilder<IResourceWithConnectionString> databaseServer = builder.ExecutionContext.IsPublishMode
    ? builder
        .AddAzurePostgresFlexibleServer(Services.DatabaseServer)
        .WithPasswordAuthentication()
        .AddDatabase(Services.Database)
    : builder.AddConnectionString(Services.Database);

var web = builder.AddProject<Projects.Web>(Services.WebApi)
    .WithReference(databaseServer)
    .WaitFor(databaseServer)
    .WithExternalHttpEndpoints()
    .WithAspNetCoreEnvironment()
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar API Reference";
        url.Url = "/scalar";
    });

if (builder.ExecutionContext.IsRunMode)
{
    builder.AddJavaScriptApp(Services.WebFrontend, "./../Web/ClientApp")
        .WithRunScript("start")
        .WithReference(web)
        .WaitFor(web)
        .WithHttpEndpoint(env: "PORT")
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
