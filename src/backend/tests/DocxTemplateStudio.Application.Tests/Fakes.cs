using DocxTemplateStudio.Application.Auth;
using DocxTemplateStudio.Application.EmploymentContracts;
using DocxTemplateStudio.Application.Security;
using DocxTemplateStudio.Application.Users;
using DocxTemplateStudio.Domain;

namespace DocxTemplateStudio.Application.Tests;

internal sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public int SaveCount { get; private set; }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Email == email));

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

    public void Add(User user) => Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

/// <summary>Readable fake: the hash is the password with a prefix.</summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public int HashCount { get; private set; }

    public string Hash(string password)
    {
        HashCount++;
        return "hashed:" + password;
    }

    public bool Verify(string passwordHash, string password) => passwordHash == "hashed:" + password;
}

internal sealed class FakeTokenIssuer : IAccessTokenIssuer
{
    public static readonly DateTime ExpiresAt = new(2026, 11, 6, 10, 0, 0, DateTimeKind.Utc);

    public AccessToken Issue(User user) => new($"token-for-{user.Id}", ExpiresAt);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class InMemoryContractRepository : IContractRepository
{
    public List<Contract> Contracts { get; } = [];

    public (string? Search, int Skip, int Take) LastSearch { get; private set; }

    public int SaveCount { get; private set; }

    public Task<(IReadOnlyList<Contract> Items, int TotalCount)> SearchAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        LastSearch = (search, skip, take);
        var matches = Contracts.Where(c => search is null || c.EmployeeLastName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        return Task.FromResult<(IReadOnlyList<Contract>, int)>(([.. matches.Skip(skip).Take(take)], matches.Count));
    }

    public Task<Contract?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Contracts.SingleOrDefault(c => c.Id == id));

    public void Add(Contract contract) => Contracts.Add(contract);

    public void Remove(Contract contract) => Contracts.Remove(contract);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
