// using MediatR;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using SurveillanceCameras.Application.Common.Interfaces;
// using SurveillanceCameras.Application.Local.StorySources.Commands.CreateStorySource;
// using SurveillanceCameras.Application.Local.WebSources.IntegrationEvents;
// using SurveillanceCameras.Application.Local.WebSources.Queries.FetchStoryListPage;
// using SurveillanceCameras.Infrastructure.Kafka;
// using SurveillanceCameras.Infrastructure.Kafka.Options;
//
// namespace SurveillanceCameras.Infrastructure.Consumer;
//
// /// <summary>
// /// Crawls one page of a WebSource's story listing. It never touches our own DB, so there's no
// /// "entity" to attach events to (that pattern is only for Command handlers with an EF-tracked
// /// aggregate + the Outbox). Each story found is created in-process via ISender — no Kafka hop
// /// needed for that, it's just a DB insert. Only pagination goes back through Kafka: if the page
// /// has a next page, this republishes FetchStoryWebSourceIntegrationEvent with the next page's
// /// URL, which is what drives crawling across multiple messages instead of one long-running loop.
// /// </summary>
// public sealed class FetchStoryWebSourceConsumer : KafkaConsumerBase<FetchStoryWebSourceIntegrationEvent>
// {
//     private readonly IServiceScopeFactory _scopeFactory;
//
//     public FetchStoryWebSourceConsumer(
//         IServiceScopeFactory scopeFactory,
//         IOptions<KafkaOptions> options,
//         IKafkaTopicRegistry topicRegistry,
//         ILogger<FetchStoryWebSourceConsumer> logger)
//         : base(options, topicRegistry, logger)
//     {
//         _scopeFactory = scopeFactory;
//     }
//
//     protected override async Task HandleAsync(FetchStoryWebSourceIntegrationEvent integrationEvent,
//         CancellationToken cancellationToken)
//     {
//         if (string.IsNullOrWhiteSpace(integrationEvent.LinkRaw))
//         {
//             return;
//         }
//
//         await using var scope = _scopeFactory.CreateAsyncScope();
//         var sender = scope.ServiceProvider.GetRequiredService<ISender>();
//         var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
//
//         var page = await sender.Send(new FetchStoryListPageQuery
//         {
//             PageUrl = integrationEvent.LinkRaw
//         }, cancellationToken);
//
//         foreach (var story in page.Stories.Where(story => !string.IsNullOrWhiteSpace(story.Link)))
//         {
//             await sender.Send(new CreateStorySourceCommand
//             {
//                 WebSourceId = integrationEvent.WebSourceId,
//                 Title = story.Title,
//                 LinkRaw = story.Link
//             }, cancellationToken);
//         }
//
//         if (!string.IsNullOrWhiteSpace(page.NextPageUrl))
//         {
//             await eventBus.PublishAsync(new FetchStoryWebSourceIntegrationEvent
//             {
//                 WebSourceId = integrationEvent.WebSourceId,
//                 LinkRaw = page.NextPageUrl
//             }, cancellationToken);
//         }
//     }
// }
