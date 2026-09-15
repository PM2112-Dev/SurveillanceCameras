using System.Reflection;
using SurveillanceCameras.Application.Common.Interfaces;
using SurveillanceCameras.Domain.Common;
using SurveillanceCameras.Domain.Entities;
using SurveillanceCameras.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
    
    public DbSet<Story> Stories => Set<Story>();
    
    public DbSet<AccountType> AccountTypes => Set<AccountType>();
    
    public DbSet<Account> Accounts => Set<Account>();
    
    public DbSet<Chapter> Chapters => Set<Chapter>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Npgsql only accepts a UTC offset when writing to 'timestamp with time zone'. A client sending
        // a local offset (e.g. +07:00) would otherwise fail with ArgumentException at SaveChanges.
        // Converting on the way in preserves the instant; timestamptz stores UTC regardless.
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<UtcDateTimeOffsetConverter>();
    }

    private sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
    {
        public UtcDateTimeOffsetConverter()
            : base(v => v.ToUniversalTime(), v => v)
        {
        }
    }
}
