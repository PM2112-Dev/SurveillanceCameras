using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Infrastructure.Consumer;
using SurveillanceCameras.Infrastructure.Data;
using SurveillanceCameras.Infrastructure.Kafka.Options;
using SurveillanceCameras.Shared;

namespace SurveillanceCameras.Infrastructure.Kafka;

/// <summary>
/// Extension methods for wiring all Kafka services into the DI container.
/// Call from Infrastructure <see cref="DependencyInjection.AddInfrastructureServices"/>.
/// </summary>
public static class KafkaDependencyInjection
{
    public static void AddKafkaServices(this IHostApplicationBuilder builder)
    {
        // 1. Bind strongly-typed options from appsettings, then let the Aspire-injected
        //    "Kafka" connection string (AppHost's AddKafka(...).WithReference(web) in run mode)
        //    override BootstrapServers — same precedence Postgres uses via GetConnectionString.
        builder.Services.Configure<KafkaOptions>(
            builder.Configuration.GetSection(KafkaOptions.SectionName));

        builder.Services.PostConfigure<KafkaOptions>(options =>
        {
            var connectionString = builder.Configuration.GetConnectionString(Services.Kafka);
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.BootstrapServers = connectionString;
            }
        });

        // 2. Topic registry
        builder.Services.AddSingleton<IKafkaTopicRegistry, KafkaTopicRegistry>();

        // 3. Event bus (direct producer — used by OutboxProcessor)
        builder.Services.AddSingleton<IEventBus, KafkaEventBus>();

        // 4/5. Background services (outbox publisher + consumers) — skipped under design-time
        // tooling. `dotnet build`'s OpenAPI-doc-generation step (Microsoft.Extensions.ApiDescription
        // .Server's dotnet-getdocument.dll) loads this same assembly in-process via
        // HostFactoryResolver and DOES call builder.Build() → app.RunAsync(), which starts every
        // registered IHostedService, before tearing the host down almost immediately. Lazily
        // constructing the Kafka clients (see KafkaEventBus/KafkaConsumerBase) keeps that instant
        // safe for anything that awaits something (like a DB query) before touching the network —
        // but a consumer's ExecuteAsync touches Kafka synchronously as its very first statement, so
        // it fires within that brief window regardless. And that MSBuild step treats ANY stderr line
        // from the subprocess as a build error (LogStandardErrorAsError="true" in the .targets file),
        // so a real connection-refused error from librdkafka's native logging — which isn't fully
        // interceptable from the C# log handlers — fails the build outright. `dotnet ef` design-time
        // commands use the same HostFactoryResolver mechanism, so this guard covers both tools.
        if (!IsDesignTimeTooling())
        {
            builder.Services.AddHostedService<OutboxProcessor>();
            builder.Services.AddHostedService<FetchStoryWebSourceConsumer>();
            // builder.Services.AddHostedService<WikiDichCrawlConsumer>();
        }
    }

    private static bool IsDesignTimeTooling() =>
        Environment.GetCommandLineArgs().Any(arg =>
            // `dotnet build`'s OpenAPI-doc-generation step actually launches "GetDocument.Insider.dll"
            // (dotnet-getdocument.dll is just a thin launcher that resolves and re-execs this
            // TFM-specific tool — checked empirically via Environment.GetCommandLineArgs(), the
            // launcher's own name never shows up here). "ef.dll"/"ef.exe" covers `dotnet ef` design-
            // time commands (migrations, etc.), which use the same HostFactoryResolver mechanism.
            arg.Contains("GetDocument.Insider", StringComparison.OrdinalIgnoreCase) ||
            arg.Contains("dotnet-getdocument", StringComparison.OrdinalIgnoreCase) ||
            arg.Contains("ef.dll", StringComparison.OrdinalIgnoreCase) ||
            arg.EndsWith("ef.exe", StringComparison.OrdinalIgnoreCase));
}

