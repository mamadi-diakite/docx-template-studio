using DocxTemplateStudio.Api.Auth;
using DocxTemplateStudio.Application.Contracts.Templates;
using DocxTemplateStudio.Application.Templates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocxTemplateStudio.Api.Controllers;

[ApiController]
[Route("api/templates")]
[Authorize]
public sealed class TemplatesController(TemplateService service) : ControllerBase
{
    // Leaves room for the multipart envelope and the other form fields.
    private const long MaxRequestBytes = TemplateService.MaxTemplateBytes + 64 * 1024;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TemplateResponse>>> List([FromQuery] string? module, CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(module, cancellationToken);

        return result.Value is { } templates ? Ok(templates) : this.InvalidRequest(result.Errors);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TemplateResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        await service.GetAsync(id, cancellationToken) is { } template ? Ok(template) : NotFound();

    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken) =>
        await service.OpenAsync(id, cancellationToken) is { } download
            ? File(download.Content, TemplateService.ContentType, download.FileName)
            : NotFound();

    [HttpPost]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<ActionResult<TemplateResponse>> Upload(
        [FromForm] string? module,
        [FromForm] string? name,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        await using var content = file?.OpenReadStream() ?? Stream.Null;
        var result = await service.UploadAsync(userId, module, name, file?.FileName, content, file?.Length ?? 0, cancellationToken);

        return result.Value is { } template
            ? CreatedAtAction(nameof(Get), new { id = template.Id }, template)
            : this.InvalidRequest(result.Errors);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
