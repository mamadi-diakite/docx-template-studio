using DocxTemplateStudio.Domain;
using Microsoft.EntityFrameworkCore;

namespace DocxTemplateStudio.Infrastructure.Tests;

public sealed class DocxTemplateStudioDbContextTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Saves_and_reads_a_contract()
    {
        var contract = new Contract(
            "Anna", "Smith", "Data analyst", ContractType.FixedTerm,
            new DateOnly(2026, 12, 1), new DateOnly(2027, 5, 31), 3250.50m, Now);

        await using (var db = _database.CreateContext())
        {
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var loaded = await db.Contracts.SingleAsync();

            Assert.Equal("Smith", loaded.EmployeeLastName);
            Assert.Equal(ContractType.FixedTerm, loaded.Type);
            Assert.Equal(new DateOnly(2026, 12, 1), loaded.StartDate);
            Assert.Equal(new DateOnly(2027, 5, 31), loaded.EndDate);
            Assert.Equal(3250.50m, loaded.GrossMonthlySalary);
            Assert.Equal(DateTimeKind.Utc, loaded.CreatedAtUtc.Kind);
        }
    }

    [Fact]
    public async Task Saves_and_reads_a_template_with_its_keys()
    {
        var uploader = await AddUserAsync("uploader@example.com");
        var template = new Template(
            DocumentModule.Contracts, "Employment contract", "employment-contract.docx", "key-1", 20480,
            ["employee.lastName", "contract.startDate"], uploader.Id, Now);

        await using (var db = _database.CreateContext())
        {
            db.Templates.Add(template);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var loaded = await db.Templates.Include(t => t.UploadedBy).SingleAsync();

            Assert.Equal(DocumentModule.Contracts, loaded.Module);
            Assert.Equal(["employee.lastName", "contract.startDate"], loaded.FieldKeys);
            Assert.Equal(uploader.Email, loaded.UploadedBy!.Email);
        }
    }

    [Fact]
    public async Task Email_is_unique()
    {
        await AddUserAsync("user@example.com");

        await Assert.ThrowsAsync<DbUpdateException>(() => AddUserAsync("User@Example.com"));
    }

    [Fact]
    public async Task A_user_who_uploaded_templates_cannot_be_deleted()
    {
        var uploader = await AddUserAsync("uploader@example.com");
        await using (var db = _database.CreateContext())
        {
            db.Templates.Add(new Template(DocumentModule.Contracts, "Contract", "c.docx", "key", 10, [], uploader.Id, Now));
            await db.SaveChangesAsync();
        }

        await using var context = _database.CreateContext();
        context.Users.Remove(await context.Users.SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private async Task<User> AddUserAsync(string email)
    {
        var user = new User(email, "User", "hash", Now);
        await using var db = _database.CreateContext();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }
}
