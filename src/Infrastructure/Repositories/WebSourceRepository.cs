using Microsoft.EntityFrameworkCore;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Application.Repositories;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Infrastructure.Base;

namespace SurveillanceCameras.Infrastructure.Repositories;

public class WebSourceRepository : BaseRepository<WebSource>, IWebSourceRepository
{
    public WebSourceRepository(IApplicationDbContext context) : base(context)
    {
    }

    protected override DbSet<WebSource> DbSet => _context.WebSources;
}
