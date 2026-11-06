using DocxTemplateStudio.Domain;
using DocxTemplateStudio.Infrastructure.Persistence;

namespace DocxTemplateStudio.Infrastructure.Tests;

public sealed class TemplateRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();
    private readonly User _user = new("julie.lambert@example.com", "Julie Lambert", "hash", Now);

    public TemplateRepositoryTests()
    {
        using var db = _database.CreateContext();
        db.Users.Add(_user);
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Lists_templates_by_module_then_name_then_date_with_an_optional_filter()
    {
        var later = NewTemplate(DocumentModule.Contracts, "Permanent contract", Now.AddHours(1));
        var earlier = NewTemplate(DocumentModule.Contracts, "permanent contract", Now);
        var fixedTerm = NewTemplate(DocumentModule.Contracts, "Fixed-term contract", Now);
        var mission = NewTemplate(DocumentModule.MissionOrders, "Mission order", Now);
        await using (var db = _database.CreateContext())
        {
            var repository = new TemplateRepository(db);
            foreach (var template in new[] { mission, later, earlier, fixedTerm })
            {
                repository.Add(template);
            }

            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        var checkRepository = new TemplateRepository(check);

        var all = await checkRepository.ListAsync(null, default);
        var missions = await checkRepository.ListAsync(DocumentModule.MissionOrders, default);

        Assert.Equal([fixedTerm.Id, earlier.Id, later.Id, mission.Id], all.Select(t => t.Id));
        Assert.Equal(["employee.lastName", "company.name"], all[0].FieldKeys);
        Assert.Equal([mission.Id], missions.Select(t => t.Id));
    }

    [Fact]
    public async Task Finds_and_removes_a_template()
    {
        var template = NewTemplate(DocumentModule.TrainingAgreements, "Training agreement", Now);
        await using (var db = _database.CreateContext())
        {
            db.Templates.Add(template);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var repository = new TemplateRepository(db);
            repository.Remove((await repository.FindAsync(template.Id, default))!);
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        Assert.Null(await new TemplateRepository(check).FindAsync(template.Id, default));
    }

    private Template NewTemplate(DocumentModule module, string name, DateTime uploadedAtUtc) =>
        new(module, name, $"{name}.docx", Guid.NewGuid().ToString("N"), 1024, ["employee.lastName", "company.name"], _user.Id, uploadedAtUtc);
}
