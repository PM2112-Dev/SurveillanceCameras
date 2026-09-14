using Microsoft.EntityFrameworkCore;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Common;
using SurveillanceCameras.Domain.Enums;

namespace SurveillanceCameras.Infrastructure.Base;

/// <summary>
/// Base repository providing common CRUD operations.
/// Audit fields (Created, CreatedBy, LastModified, LastModifiedBy, BaseStatus)
/// are automatically set by AuditableEntityInterceptor.
/// </summary>
/// <typeparam name="T">Entity type that inherits from BaseAuditableEntity</typeparam>
public abstract class BaseRepository<T> : ICommonRepository<T> where T : BaseAuditableEntity
{
    protected readonly IApplicationDbContext _context;

    protected BaseRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    protected abstract DbSet<T> DbSet { get; }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<int> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public virtual async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Soft delete - sets BaseStatus = Deleted
    /// </summary>
    public virtual async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
        {
            return 0;
        }

        entity.BaseStatus = BaseStatus.Deleted;
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
