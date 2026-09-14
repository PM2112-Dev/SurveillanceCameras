using System.Reflection;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Common;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SurveillanceCameras.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
    
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<WebSource> WebSources => Set<WebSource>();
    
    public DbSet<StorySource> StorySources => Set<StorySource>();

    public DbSet<Category> Categories => Set<Category>();
    
    public DbSet<PromptType> PromptTypes => Set<PromptType>();
    
    public DbSet<Prompt> Prompts => Set<Prompt>();
    
    public DbSet<Chapter> Chapters => Set<Chapter>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
