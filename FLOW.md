# FLOW

Tài liều này mô tả flow để viết api

## 1) Module Domain

### 1.1 Tạo entity mới

- **Vị trí**: `src\Domain\Entities`

- **Cách tạo**: Tạo class mới kế thừa `BaseAuditableEntity`

**Example**:

```csharp
public class Example : BaseAuditableEntity
{
    // Các thuộc tính khác của entity
}
```

### 1.2 Tạo Event

- **Vị trí**: `src\Domain\Events`

- **Cách tạo**: Tạo class mới kế thừa `BaseEvent`

**Example**:

```csharp
public class ExampleCompletedEvent : BaseEvent
{
    public ExampleCompletedEvent(Entity entity)
    {
        Entity = entity;
    }
    
    public Entity Entity { get; }
}
```

## 2) Module Application

### 2.1 Thêm Entity vào IApplicationDbContext

- **Vị trí**: `src\Application\Common\Interfaces\IApplicationDbContext.cs`

- **Cách tạo**: Thêm `DbSet<Entity>` vào interface

**Example**:

```csharp
public interface IApplicationDbContext
{
    // Các DbSet khác
    
    DbSet<Entity> Entity { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
```

### 2.2 Tạo Command/Query mới

#### 2.2.1 Tạo Command

- **Vị trí**: `src\Application\Service\{FeatureName}\Commands`

- **Cách tạo**: Tạo record mới kế thừa `IRequest<TRespone>`

**Example Create**:

- **Vị trí**: `src\Application\Service\{FeatureName}\Commands\CreateExample\CreateExample.cs`

```csharp
public record CreateExampleCommand : IRequest<int> // Trả về Id của entity mới tạo
{
    public string? Title { get; init; }
    
    // Các thuộc tính khác của command
}

public class CreateExampleCommandHandler : IRequestHandler<CreateExampleCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateExampleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateExampleCommand request, CancellationToken cancellationToken)
    {
        var entity = new Entity { Title = request.Title}; // Bổ sung các thuộc tính khác nếu cần
        
        _context.Entitys.Add(entity); // Examples là DbSet<Example> trong ApplicationDbContext
        
        await _context.SaveChangesAsync(cancellationToken);
        
        return entity.Id;
    }
}
```

- **Vị trí**: `src\Application\Service\{FeatureName}\Commands\CreateExample\CreateExampleCommandValidator.cs`

```csharp
public class CreateExampleCommandValidator : AbstractValidator<CreateExampleCommand>
{
    public CreateExampleCommandValidator()
    {
        // Thêm các rule validation cho command
        // Xem thêm tại VALIDATION.md
    }
}
```

**Example Update**:

- **Vị trí**: `src\Application\Service\{FeatureName}\Commands\UpdateExample\UpdateExample.cs`

```csharp
public record UpdateExampleCommand : IRequest
{
    public int Id { get; init; }
    
    public string? Title { get; init; }
    
    // Các thuộc tính khác của command
}

public class UpdateExampleCommandHandler : IRequestHandler<UpdateExampleCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateExampleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(UpdateExampleCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Examples
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        entity.Title = request.Title;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

- **Vị trí**: `src\Application\Service\{FeatureName}\Commands\UpdateExample\UpdateExampleCommandValidator.cs`

```csharp
public class UpdateExampleCommandValidator : AbstractValidator<CreateExampleCommand>
{
    public UpdateExampleCommandValidator()
    {
        // Thêm các rule validation cho command
        // Xem thêm tại VALIDATION.md
    }
}
```

**Example Delete**:

- **Vị trí**: `src\Application\Service\{FeatureName}\Commands\DeleteExample\DeleteExample.cs`

```csharp
public record DeleteExampleCommand : IRequest;

public class DeleteExampleCommandHandler : IRequestHandler<DeleteExampleCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteExampleCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteExampleCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Examples
            .FindAsync([request.Id], cancellationToken);

        Guard.Against.NotFound(request.Id, entity);

        _context.Examples.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }

}
```

#### 2.2.2 Tạo Query

Todo

## 3) Module Infrastructure

### 3.1 Thêm DbSet<Entity> vào ApplicationDbContext

- **Vị trí**: `src\Infrastructure\Data\ApplicationDbContext.cs`

- **Cách tạo**: Thêm `DbSet<Entity>` vào class `ApplicationDbContext`

**Example**:

```csharp
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    // Các DbSet khác
    
    public DbSet<Entity> Entity => Set<Entity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}

```

### 3.2 Tạo EntityTypeConfiguration

- **Vị trí**: `src\Infrastructure\Persistence\Configurations`

- **Cách tạo**: Tạo class mới kế thừa `IEntityTypeConfiguration<Entity>`

**Example**:

```csharp
public class ExampleConfiguration : IEntityTypeConfiguration<Example>
{
    public void Configure(EntityTypeBuilder<Example> builder)
    {
        builder.Property(t => t.Title)
            .HasMaxLength(200)
            .IsRequired();
    }
}

```