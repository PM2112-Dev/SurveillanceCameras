using Microsoft.EntityFrameworkCore;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repositories;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Infrastructure.Base;

namespace SurveillanceCameras.Infrastructure.Repositories;

public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
{
    public CategoryRepository(IApplicationDbContext context) : base(context)
    {}

    protected override DbSet<Category> DbSet => _context.Categories;
}
