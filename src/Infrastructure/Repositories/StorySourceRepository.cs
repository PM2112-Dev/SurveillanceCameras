using Microsoft.EntityFrameworkCore;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repositories;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Infrastructure.Base;

namespace SurveillanceCameras.Infrastructure.Repositories;

public class StorySourceRepository : BaseRepository<StorySource>, IStorySourceRepository
{
    public StorySourceRepository(IApplicationDbContext context) : base(context)
    {}
    
    protected override DbSet<StorySource> DbSet => _context.StorySources;
}
