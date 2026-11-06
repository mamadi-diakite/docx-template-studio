using DocxTemplateStudio.Domain;
using DocxTemplateStudio.Infrastructure.Persistence;

namespace DocxTemplateStudio.Infrastructure.Tests;

public sealed class ContractRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    public ContractRepositoryTests()
    {
        using var db = _database.CreateContext();
        db.Contracts.AddRange(
            NewContract("Nadia", "Lemaire", "HR manager"),
            NewContract("Marion", "Aubert", "Payroll officer"),
            NewContract("Elise", "Carpentier", "Training coordinator"),
            NewContract("Anne", "Aubert", "Sales_assistant 100%"));
        db.SaveChanges();
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Lists_all_contracts_by_last_then_first_name()
    {
        await using var db = _database.CreateContext();

        var (items, total) = await new ContractRepository(db).SearchAsync(null, 0, 10, default);

        Assert.Equal(4, total);
        Assert.Equal(["Anne", "Marion", "Elise", "Nadia"], items.Select(c => c.EmployeeFirstName));
    }

    [Fact]
    public async Task Pages_through_the_results()
    {
        await using var db = _database.CreateContext();

        var (items, total) = await new ContractRepository(db).SearchAsync(null, 1, 2, default);

        Assert.Equal(4, total);
        Assert.Equal(["Marion", "Elise"], items.Select(c => c.EmployeeFirstName));
    }

    [Theory]
    [InlineData("aubert", 2)]
    [InlineData("NADIA", 1)]
    [InlineData("coordinator", 1)]
    [InlineData("%", 1)]
    [InlineData("_", 1)]
    [InlineData("\\", 0)]
    [InlineData("nobody", 0)]
    public async Task Searches_names_and_position_ignoring_case_and_wildcards(string search, int expected)
    {
        await using var db = _database.CreateContext();

        var (items, total) = await new ContractRepository(db).SearchAsync(search, 0, 10, default);

        Assert.Equal(expected, total);
        Assert.Equal(expected, items.Count);
    }

    [Fact]
    public async Task Adds_finds_and_removes_a_contract()
    {
        var contract = NewContract("Tom", "Renaud", "Marketing intern");
        await using (var db = _database.CreateContext())
        {
            var repository = new ContractRepository(db);
            repository.Add(contract);
            await repository.SaveChangesAsync(default);
        }

        await using (var db = _database.CreateContext())
        {
            var repository = new ContractRepository(db);
            var found = await repository.FindAsync(contract.Id, default);
            repository.Remove(found!);
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        Assert.Null(await new ContractRepository(check).FindAsync(contract.Id, default));
    }

    private static Contract NewContract(string firstName, string lastName, string position) =>
        new(firstName, lastName, position, ContractType.Permanent, new DateOnly(2024, 1, 1), null, 2500m, Now);
}
