using DocxTemplateStudio.Application.Common;
using DocxTemplateStudio.Application.Contracts.EmploymentContracts;
using DocxTemplateStudio.Application.EmploymentContracts;
using DocxTemplateStudio.Domain;

namespace DocxTemplateStudio.Application.Tests;

public sealed class ContractServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Start = new(2026, 9, 1);

    private static readonly ContractInput Input =
        new(" Karim ", " Bellamy ", " Sales assistant ", "FixedTerm", Start, Start.AddMonths(6), 2100m);

    private readonly InMemoryContractRepository _contracts = new();
    private readonly ContractService _service;

    public ContractServiceTests()
    {
        _service = new ContractService(_contracts, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Create_saves_the_contract()
    {
        var result = await _service.CreateAsync(Input);

        Assert.Equal(ServiceResultStatus.Ok, result.Status);
        var contract = Assert.Single(_contracts.Contracts);
        Assert.Equal(
            new ContractResponse(contract.Id, "Karim", "Bellamy", "Sales assistant", "FixedTerm", Start, Start.AddMonths(6), 2100m,
                Now.UtcDateTime, Now.UtcDateTime),
            result.Value);
        Assert.Equal(1, _contracts.SaveCount);
    }

    [Fact]
    public async Task Create_with_errors_saves_nothing()
    {
        var result = await _service.CreateAsync(Input with { Type = "Unknown" });

        Assert.Equal(ServiceResultStatus.Invalid, result.Status);
        Assert.Contains("type", result.Errors.Keys);
        Assert.Empty(_contracts.Contracts);
        Assert.Equal(0, _contracts.SaveCount);
    }

    [Fact]
    public async Task Update_changes_the_contract()
    {
        var contract = AddContract("Aubert");

        var result = await _service.UpdateAsync(contract.Id, Input with { Type = "Permanent", EndDate = null });

        Assert.Equal(ServiceResultStatus.Ok, result.Status);
        Assert.Equal("Permanent", result.Value?.Type);
        Assert.Equal("Bellamy", contract.EmployeeLastName);
        Assert.Equal(Now.UtcDateTime, contract.UpdatedAtUtc);
        Assert.Equal(1, _contracts.SaveCount);
    }

    [Fact]
    public async Task Update_of_an_unknown_contract_is_not_found_even_with_errors()
    {
        Assert.Equal(ServiceResultStatus.NotFound, (await _service.UpdateAsync(Guid.NewGuid(), Input)).Status);
        Assert.Equal(ServiceResultStatus.NotFound, (await _service.UpdateAsync(Guid.NewGuid(), Input with { Position = "" })).Status);
    }

    [Fact]
    public async Task Update_with_errors_keeps_the_contract()
    {
        var contract = AddContract("Aubert");

        var result = await _service.UpdateAsync(contract.Id, Input with { GrossMonthlySalary = -5m });

        Assert.Equal(ServiceResultStatus.Invalid, result.Status);
        Assert.Equal("Aubert", contract.EmployeeLastName);
        Assert.Equal(0, _contracts.SaveCount);
    }

    [Fact]
    public async Task Get_and_delete()
    {
        var contract = AddContract("Aubert");

        Assert.Equal("Aubert", (await _service.GetAsync(contract.Id))?.EmployeeLastName);
        Assert.True(await _service.DeleteAsync(contract.Id));
        Assert.Empty(_contracts.Contracts);
        Assert.Null(await _service.GetAsync(contract.Id));
        Assert.False(await _service.DeleteAsync(contract.Id));
        Assert.Equal(1, _contracts.SaveCount);
    }

    [Fact]
    public async Task Search_pages_through_matches()
    {
        foreach (var name in new[] { "Aubert", "Bellamy", "Carpentier", "Delorme", "Ferrand" })
        {
            AddContract(name);
        }

        var page = await _service.SearchAsync("  ", page: 2, pageSize: 2);

        Assert.Equal(["Carpentier", "Delorme"], page.Items.Select(c => c.EmployeeLastName));
        Assert.Equal((2, 2, 5), (page.Page, page.PageSize, page.TotalCount));
        Assert.Equal((null, 2, 2), _contracts.LastSearch);
    }

    [Fact]
    public async Task Search_trims_the_term_and_keeps_page_values_in_range()
    {
        AddContract("Aubert");

        var page = await _service.SearchAsync(" aub ", page: 0, pageSize: 1000);

        Assert.Equal((1, ContractService.MaxPageSize), (page.Page, page.PageSize));
        Assert.Equal(("aub", 0, ContractService.MaxPageSize), _contracts.LastSearch);
        Assert.Single(page.Items);
    }

    private Contract AddContract(string lastName)
    {
        var contract = new Contract("Marion", lastName, "Payroll officer", ContractType.Permanent, Start, null, 2650m, Now.UtcDateTime.AddDays(-1));
        _contracts.Contracts.Add(contract);

        return contract;
    }
}
