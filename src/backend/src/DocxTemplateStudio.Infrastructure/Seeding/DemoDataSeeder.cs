using Microsoft.EntityFrameworkCore;
using DocxTemplateStudio.Application.Security;
using DocxTemplateStudio.Domain;
using DocxTemplateStudio.Infrastructure.Persistence;

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

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }
}
