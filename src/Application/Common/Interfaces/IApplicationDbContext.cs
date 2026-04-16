﻿using SurveillanceCameras.Domain.Common;
using SurveillanceCameras.Domain.Entities;

namespace SurveillanceCameras.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }
    
    DbSet<Area> Areas { get; }
    
    DbSet<Camera> Cameras { get; }
    
    DbSet<Story> Stories { get; }

    /// <summary>Outbox pattern: pending integration events awaiting Kafka publish.</summary>
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
