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

// Kafka is only needed for local development message brokering; not provisioned in publish mode.
// docker-compose/host Kafka isn't available on every dev machine, so this stays run-mode only,
// same reasoning as the Postgres branch above but Kafka has no "local, already-installed" option yet.
IResourceBuilder<IResourceWithConnectionString>? kafka = builder.ExecutionContext.IsRunMode
    ? builder.AddKafka(Services.Kafka).WithKafkaUI()
    : null;

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

if (kafka is not null)
{
    web = web.WithReference(kafka).WaitFor(kafka);
}

if (builder.ExecutionContext.IsRunMode)
{
    builder.AddJavaScriptApp(Services.WebFrontend, "./../Web/ClientApp")
        .WithRunScript("start")
        .WithReference(web)
        .WaitFor(web)
        // Cố định cổng: token đăng nhập lưu trong localStorage theo origin (localhost:<cổng>), cổng ngẫu nhiên
        // mỗi lần chạy sẽ làm mất phiên đăng nhập. Không qua proxy để Vite nghe thẳng trên đúng cổng này.
        .WithHttpEndpoint(port: 44447, env: "PORT", isProxied: false)
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
