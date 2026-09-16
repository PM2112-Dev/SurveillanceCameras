using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Common;
using System.Text.Json;

namespace SurveillanceCameras.Infrastructure.Data.Interceptors;

/// <summary>
/// EF Core interceptor implementing the Outbox Pattern.
/// Before saving, converts pending IntegrationEvents (attached to entities) into
/// OutboxMessage rows — written in the SAME transaction as the domain state change.
/// This guarantees at-least-once delivery to Kafka.
/// </summary>
public class OutboxInterceptor : SaveChangesInterceptor
{
    private readonly IKafkaTopicRegistry _topicRegistry;

    public OutboxInterceptor(IKafkaTopicRegistry topicRegistry)
    {
        _topicRegistry = topicRegistry;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AppendOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AppendOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AppendOutboxMessages(DbContext? context)
    {
        if (context is null) return;

        // Collect integration events staged on entities
        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.IntegrationEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (entities.Count == 0) return;

        var integrationEvents = entities.SelectMany(e => e.IntegrationEvents).ToList();

        // Clear immediately, same as DispatchDomainEventsInterceptor does for domain events —
        // the DbContext (and its tracked entities) outlives a single SaveChanges call within a
        // request scope, so a second SaveChanges on the same entity would otherwise re-queue and
        // duplicate-publish the same events.
        foreach (var entity in entities)
        {
            entity.ClearIntegrationEvents();
        }

        foreach (var evt in integrationEvents)
        {
            var topic = _topicRegistry.GetTopic(evt.EventType);
            var payload = JsonSerializer.Serialize(evt, evt.GetType());
            var outbox = OutboxMessage.Create(evt.EventType, topic, payload);
            context.Set<OutboxMessage>().Add(outbox);
        }
    }
}


