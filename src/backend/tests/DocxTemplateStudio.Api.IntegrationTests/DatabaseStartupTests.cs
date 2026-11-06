using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DocxTemplateStudio.Infrastructure.Persistence;

namespace DocxTemplateStudio.Api.IntegrationTests;

public sealed class DatabaseStartupTests(DocxTemplateStudioApiFactory factory) : IClassFixture<DocxTemplateStudioApiFactory>
{
    [Fact]
    public async Task Startup_applies_all_migrations()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DocxTemplateStudioDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(0, await db.Users.CountAsync());
    }
}
