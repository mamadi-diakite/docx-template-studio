using DocxTemplateStudio.Application.EmploymentContracts;
using DocxTemplateStudio.Domain;
using Microsoft.EntityFrameworkCore;

namespace DocxTemplateStudio.Infrastructure.Persistence;

public sealed class ContractRepository(DocxTemplateStudioDbContext db) : IContractRepository
{
    private const string LikeEscape = "\\";

    public async Task<(IReadOnlyList<Contract> Items, int TotalCount)> SearchAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = db.Contracts.AsNoTracking();
        if (search is not null)
        {
            // SQLite LIKE ignores case for ASCII letters; wildcards typed by the user are matched literally.
            var pattern = "%" + search.Replace(LikeEscape, LikeEscape + LikeEscape).Replace("%", LikeEscape + "%").Replace("_", LikeEscape + "_") + "%";
            query = query.Where(c =>
                EF.Functions.Like(c.EmployeeFirstName, pattern, LikeEscape)
                || EF.Functions.Like(c.EmployeeLastName, pattern, LikeEscape)
                || EF.Functions.Like(c.Position, pattern, LikeEscape));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.EmployeeLastName)
            .ThenBy(c => c.EmployeeFirstName)
            .ThenBy(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<Contract?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Contracts.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(Contract contract) => db.Contracts.Add(contract);

    public void Remove(Contract contract) => db.Contracts.Remove(contract);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
