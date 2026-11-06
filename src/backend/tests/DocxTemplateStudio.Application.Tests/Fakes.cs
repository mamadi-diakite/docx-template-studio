using DocxTemplateStudio.Application.Auth;
using DocxTemplateStudio.Application.EmploymentContracts;
using DocxTemplateStudio.Application.Security;
using DocxTemplateStudio.Application.Storage;
using DocxTemplateStudio.Application.Templates;
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

internal sealed class InMemoryTemplateRepository : ITemplateRepository
{
    public List<Template> Templates { get; } = [];

    public DocumentModule? LastFilter { get; private set; }

    public int SaveCount { get; private set; }

    public Task<IReadOnlyList<Template>> ListAsync(DocumentModule? module, CancellationToken cancellationToken)
    {
        LastFilter = module;

        return Task.FromResult<IReadOnlyList<Template>>([.. Templates.Where(t => module is null || t.Module == module)]);
    }

    public Task<Template?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Templates.SingleOrDefault(t => t.Id == id));

    public void Add(Template template) => Templates.Add(template);

    public void Remove(Template template) => Templates.Remove(template);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken)
    {
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        var key = Guid.NewGuid().ToString("N");
        Files[key] = copy.ToArray();

        return key;
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream(Files[key]));

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        Files.Remove(key);
        return Task.CompletedTask;
    }
}

internal sealed class FakeFieldKeyReader : IFieldKeyReader
{
    public IReadOnlyList<string>? Keys { get; set; } = ["employee.lastName"];

    public byte[]? LastContent { get; private set; }

    public IReadOnlyList<string>? ReadFieldKeys(Stream document)
    {
        using var copy = new MemoryStream();
        document.CopyTo(copy);
        LastContent = copy.ToArray();

        return Keys;
    }
}
