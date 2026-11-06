using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DocxTemplateStudio.Infrastructure.Persistence;
using DocxTemplateStudio.Infrastructure.Seeding;

namespace DocxTemplateStudio.Api.IntegrationTests;

public sealed class DemoDataStartupTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    [Fact]
    public async Task Development_startup_seeds_demo_data_when_enabled()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DocxTemplateStudioDbContext>();

        Assert.Equal(DemoDataSeeder.Accounts.Count, await db.Users.CountAsync());
    }
}

public sealed class ProductionDemoDataStartupTests(ProductionDemoDataApiFactory factory)
    : IClassFixture<ProductionDemoDataApiFactory>
{
    [Fact]
    public async Task Startup_outside_development_never_seeds_demo_data()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DocxTemplateStudioDbContext>();

        Assert.Equal(0, await db.Users.CountAsync());
    }
}
