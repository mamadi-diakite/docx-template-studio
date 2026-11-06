namespace DocxTemplateStudio.Application.Templates;

public interface IFieldKeyReader
{
    /// <summary>Reads the tags of the content controls in the body, headers and footers.</summary>
    /// <returns>The trimmed keys in document order without duplicates, or <c>null</c> when the file is not a readable Word document.</returns>
    IReadOnlyList<string>? ReadFieldKeys(Stream document);
}
