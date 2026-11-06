using DocxTemplateStudio.Domain;

namespace DocxTemplateStudio.Application.EmploymentContracts;

public interface IContractRepository
{
    /// <param name="search">Matched against first name, last name and position, ignoring case.</param>
    /// <returns>One page ordered by last name then first name, and the total count of matches.</returns>
    Task<(IReadOnlyList<Contract> Items, int TotalCount)> SearchAsync(
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<Contract?> FindAsync(Guid id, CancellationToken cancellationToken);

    void Add(Contract contract);

    void Remove(Contract contract);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
