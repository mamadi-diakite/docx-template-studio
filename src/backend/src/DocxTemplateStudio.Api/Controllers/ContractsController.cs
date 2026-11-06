using DocxTemplateStudio.Application.Common;
using DocxTemplateStudio.Application.Contracts.Common;
using DocxTemplateStudio.Application.Contracts.EmploymentContracts;
using DocxTemplateStudio.Application.EmploymentContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocxTemplateStudio.Api.Controllers;

[ApiController]
[Route("api/contracts")]
[Authorize]
public sealed class ContractsController(ContractService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ContractResponse>>> Search(
        [FromQuery] string? search,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ContractService.DefaultPageSize) =>
        Ok(await service.SearchAsync(search, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContractResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        await service.GetAsync(id, cancellationToken) is { } contract ? Ok(contract) : NotFound();

    [HttpPost]
    public async Task<ActionResult<ContractResponse>> Create(ContractInput input, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(input, cancellationToken);

        return result.Value is { } contract
            ? CreatedAtAction(nameof(Get), new { id = contract.Id }, contract)
            : this.InvalidRequest(result.Errors);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContractResponse>> Update(Guid id, ContractInput input, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, input, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Ok => Ok(result.Value),
            ServiceResultStatus.NotFound => NotFound(),
            _ => this.InvalidRequest(result.Errors),
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
