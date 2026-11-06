namespace DocxTemplateStudio.Application.Contracts.Templates;

/// <param name="FieldKeys">Tags of the content controls found in the template, in document order.</param>
public sealed record TemplateResponse(
    Guid Id,
    string Module,
    string Name,
    string FileName,
    long SizeBytes,
    IReadOnlyList<string> FieldKeys,
    DateTime UploadedAtUtc);
