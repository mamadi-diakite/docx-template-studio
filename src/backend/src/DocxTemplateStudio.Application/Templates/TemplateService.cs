namespace DocxTemplateStudio.Application.Templates;

public sealed record TemplateDownload(string FileName, Stream Content);

/// <summary>Templates are shared by every user of the workspace.</summary>
public sealed class TemplateService(
    ITemplateRepository templates,
    IFileStorage storage,
    IFieldKeyReader fieldKeyReader,
    TimeProvider timeProvider)
{
    public const long MaxTemplateBytes = 10 * 1024 * 1024;
    public const string ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private const string ModuleMessage = "Choose a module.";

    public async Task<ServiceResult<IReadOnlyList<TemplateResponse>>> ListAsync(
        string? module,
        CancellationToken cancellationToken = default)
    {
        DocumentModule? filter = null;
        if (!string.IsNullOrWhiteSpace(module))
        {
            if (ParseModule(module) is not { } parsed)
            {
                return ServiceResult<IReadOnlyList<TemplateResponse>>.Invalid(
                    new Dictionary<string, string[]> { ["module"] = [ModuleMessage] });
            }

            filter = parsed;
        }

        var found = await templates.ListAsync(filter, cancellationToken);

        return ServiceResult<IReadOnlyList<TemplateResponse>>.Ok([.. found.Select(ToResponse)]);
    }

    public async Task<TemplateResponse?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await templates.FindAsync(id, cancellationToken);

        return template is null ? null : ToResponse(template);
    }

    /// <param name="name">Display name; the file name without its extension when empty.</param>
    /// <param name="length">Size announced by the upload, checked before the content is read.</param>
    public async Task<ServiceResult<TemplateResponse>> UploadAsync(
        Guid userId,
        string? module,
        string? name,
        string? fileName,
        Stream content,
        long length,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var errors = new Dictionary<string, string[]>();
        var parsedModule = ParseModule(module);
        if (parsedModule is null)
        {
            errors["module"] = [ModuleMessage];
        }

        var file = Path.GetFileName(fileName)?.Trim();
        var fileError = length switch
        {
            <= 0 => "Choose a .docx file that is not empty.",
            > MaxTemplateBytes => $"The file is larger than {MaxTemplateBytes / 1024 / 1024} MB.",
            _ when string.IsNullOrEmpty(file) => "The file has no name.",
            _ when file.Length > Template.FileNameMaxLength => $"The file name is longer than {Template.FileNameMaxLength} characters.",
            _ when !file.EndsWith(Template.FileExtension, StringComparison.OrdinalIgnoreCase) => "Only Word .docx files can be used as templates.",
            _ => null,
        };

        var templateName = string.IsNullOrWhiteSpace(name)
            ? Path.GetFileNameWithoutExtension(file ?? string.Empty).Trim()
            : name.Trim();
        if (templateName.Length == 0)
        {
            errors["name"] = ["Name is required."];
        }
        else if (templateName.Length > Template.NameMaxLength)
        {
            errors["name"] = [$"Use at most {Template.NameMaxLength} characters."];
        }

        await using var buffer = new MemoryStream();
        IReadOnlyList<string>? fieldKeys = null;
        if (fileError is null)
        {
            await content.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            fieldKeys = fieldKeyReader.ReadFieldKeys(buffer);
            fileError = fieldKeys switch
            {
                null => "This file is not a readable Word document.",
                _ when fieldKeys.FirstOrDefault(k => k.Length > Template.FieldKeyMaxLength) is { } longKey =>
                    $"The field key \"{longKey[..20]}…\" is longer than {Template.FieldKeyMaxLength} characters.",
                _ => null,
            };
        }

        if (fileError is not null)
        {
            errors["file"] = [fileError];
        }

        if (errors.Count > 0)
        {
            return ServiceResult<TemplateResponse>.Invalid(errors);
        }

        buffer.Position = 0;
        var key = await storage.SaveAsync(buffer, cancellationToken);
        var template = new Template(
            parsedModule!.Value,
            templateName,
            file!,
            key,
            buffer.Length,
            fieldKeys!,
            userId,
            timeProvider.GetUtcNow().UtcDateTime);
        templates.Add(template);
        await templates.SaveChangesAsync(cancellationToken);

        return ServiceResult<TemplateResponse>.Ok(ToResponse(template));
    }

    public async Task<TemplateDownload?> OpenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await templates.FindAsync(id, cancellationToken);

        return template is null
            ? null
            : new TemplateDownload(template.FileName, await storage.OpenReadAsync(template.StorageKey, cancellationToken));
    }

    /// <returns><c>false</c> when the template does not exist.</returns>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await templates.FindAsync(id, cancellationToken);
        if (template is null)
        {
            return false;
        }

        templates.Remove(template);
        await templates.SaveChangesAsync(cancellationToken);
        await storage.DeleteAsync(template.StorageKey, cancellationToken);

        return true;
    }

    private static DocumentModule? ParseModule(string? value) =>
        Enum.GetNames<DocumentModule>().FirstOrDefault(n => string.Equals(n, value?.Trim(), StringComparison.OrdinalIgnoreCase)) is { } name
            ? Enum.Parse<DocumentModule>(name)
            : null;

    private static TemplateResponse ToResponse(Template t) =>
        new(t.Id, t.Module.ToString(), t.Name, t.FileName, t.SizeBytes, t.FieldKeys, t.UploadedAtUtc);
}
