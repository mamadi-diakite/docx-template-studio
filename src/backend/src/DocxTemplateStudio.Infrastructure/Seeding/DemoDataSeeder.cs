namespace DocxTemplateStudio.Infrastructure.Seeding;

public sealed class DemoDataSeeder(DocxTemplateStudioDbContext db, IPasswordHasher passwordHasher, TimeProvider timeProvider)
{
    public const string DemoPassword = "TestDemo123!";

    /// <summary>Fictional accounts of an HR and training team who prepare documents from templates.</summary>
    public static IReadOnlyList<(string Email, string DisplayName)> Accounts { get; } =
    [
        ("julie.lambert@example.com", "Julie Lambert"),
        ("thomas.girard@example.com", "Thomas Girard"),
        ("sarah.fontaine@example.com", "Sarah Fontaine"),
        ("hugo.lefevre@example.com", "Hugo Lefevre"),
        ("ines.garnier@example.com", "Ines Garnier"),
    ];

    /// <summary>Fictional employment contracts that feed the contract templates.</summary>
    public static IReadOnlyList<DemoContract> Contracts { get; } =
    [
        new("Marion", "Aubert", "Payroll officer", ContractType.Permanent, new(2023, 3, 1), null, 2650m),
        new("Karim", "Bellamy", "Sales assistant", ContractType.FixedTerm, new(2026, 9, 1), new(2027, 2, 28), 2100m),
        new("Elise", "Carpentier", "Training coordinator", ContractType.Permanent, new(2021, 1, 4), null, 2900m),
        new("Yanis", "Delorme", "Web developer intern", ContractType.Internship, new(2026, 10, 5), new(2027, 3, 26), 650m),
        new("Clara", "Ferrand", "Accounting apprentice", ContractType.Apprenticeship, new(2025, 9, 1), new(2027, 8, 31), 1150m),
        new("Hugo", "Gautier", "Warehouse operator", ContractType.FixedTerm, new(2026, 11, 2), new(2027, 5, 1), 1900m),
        new("Nadia", "Lemaire", "HR manager", ContractType.Permanent, new(2019, 6, 17), null, 3800m),
        new("Tom", "Renaud", "Marketing intern", ContractType.Internship, new(2026, 4, 1), new(2026, 9, 30), 650m),
    ];

    /// <returns><c>false</c> when the database already holds users.</returns>
    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(cancellationToken))
        {
            return false;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        db.Users.AddRange(Accounts.Select(
            account => new User(account.Email, account.DisplayName, passwordHasher.Hash(DemoPassword), now)));
        db.Contracts.AddRange(Contracts.Select(c => new Contract(
            c.FirstName, c.LastName, c.Position, c.Type, c.StartDate, c.EndDate, c.GrossMonthlySalary, now)));

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}

public sealed record DemoContract(
    string FirstName,
    string LastName,
    string Position,
    ContractType Type,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal GrossMonthlySalary);
