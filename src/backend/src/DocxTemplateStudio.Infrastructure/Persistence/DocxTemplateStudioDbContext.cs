using DocxTemplateStudio.Domain;
using Microsoft.EntityFrameworkCore;

namespace DocxTemplateStudio.Infrastructure.Persistence;

public sealed class DocxTemplateStudioDbContext(DbContextOptions<DocxTemplateStudioDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<Template> Templates => Set<Template>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocxTemplateStudioDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
}
