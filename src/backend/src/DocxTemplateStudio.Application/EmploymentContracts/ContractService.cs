using DocxTemplateStudio.Application.Common;
using DocxTemplateStudio.Application.Contracts.Common;
using DocxTemplateStudio.Application.Contracts.EmploymentContracts;
using DocxTemplateStudio.Domain;

namespace DocxTemplateStudio.Application.EmploymentContracts;

public sealed class ContractService(IContractRepository contracts, TimeProvider timeProvider)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public async Task<PagedResponse<ContractResponse>> SearchAsync(
        string? search,
        int page = 1,
        int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (items, totalCount) = await contracts.SearchAsync(
            string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            (page - 1) * pageSize,
            pageSize,
            cancellationToken);

        return new PagedResponse<ContractResponse>([.. items.Select(ToResponse)], page, pageSize, totalCount);
    }

    public async Task<ContractResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await contracts.FindAsync(id, cancellationToken);

        return contract is null ? null : ToResponse(contract);
    }

    public async Task<ServiceResult<ContractResponse>> CreateAsync(ContractInput input, CancellationToken cancellationToken = default)
    {
        var errors = ContractValidator.Validate(input, out var type);
        if (errors.Count > 0)
        {
            return ServiceResult<ContractResponse>.Invalid(errors);
        }

        var contract = new Contract(
            input.EmployeeFirstName,
            input.EmployeeLastName,
            input.Position,
            type,
            input.StartDate,
            input.EndDate,
            input.GrossMonthlySalary,
            Now());
        contracts.Add(contract);
        await contracts.SaveChangesAsync(cancellationToken);

        return ServiceResult<ContractResponse>.Ok(ToResponse(contract));
    }

    public async Task<ServiceResult<ContractResponse>> UpdateAsync(
        Guid id,
        ContractInput input,
        CancellationToken cancellationToken = default)
    {
        var errors = ContractValidator.Validate(input, out var type);
        var contract = await contracts.FindAsync(id, cancellationToken);
        if (contract is null)
        {
            return ServiceResult<ContractResponse>.NotFound;
        }

        if (errors.Count > 0)
        {
            return ServiceResult<ContractResponse>.Invalid(errors);
        }

        contract.Update(
            input.EmployeeFirstName,
            input.EmployeeLastName,
            input.Position,
            type,
            input.StartDate,
            input.EndDate,
            input.GrossMonthlySalary,
            Now());
        await contracts.SaveChangesAsync(cancellationToken);

        return ServiceResult<ContractResponse>.Ok(ToResponse(contract));
    }

    /// <returns><c>false</c> when the contract does not exist.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var contract = await contracts.FindAsync(id, cancellationToken);
        if (contract is null)
        {
            return false;
        }

        contracts.Remove(contract);
        await contracts.SaveChangesAsync(cancellationToken);

        return true;
    }

    private DateTime Now() => timeProvider.GetUtcNow().UtcDateTime;

    private static ContractResponse ToResponse(Contract contract) => new(
        contract.Id,
        contract.EmployeeFirstName,
        contract.EmployeeLastName,
        contract.Position,
        contract.Type.ToString(),
        contract.StartDate,
        contract.EndDate,
        contract.GrossMonthlySalary,
        contract.CreatedAtUtc,
        contract.UpdatedAtUtc);
}
