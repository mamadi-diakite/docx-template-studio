using System.IO.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplateStudio.Application.Templates;

namespace DocxTemplateStudio.Infrastructure.Templates;

/// <summary>Reads content control tags with the Open XML SDK, without Microsoft Word.</summary>
public sealed class OpenXmlFieldKeyReader : IFieldKeyReader
{
    public IReadOnlyList<string>? ReadFieldKeys(Stream document)
    {
        ArgumentNullException.ThrowIfNull(document);

        try
        {
            using var word = WordprocessingDocument.Open(document, isEditable: false);
            if (word.MainDocumentPart is not { } main)
            {
                return null;
            }

            var roots = new List<OpenXmlElement?> { main.Document };
            roots.AddRange(main.HeaderParts.Select(part => part.Header));
            roots.AddRange(main.FooterParts.Select(part => part.Footer));

            return
            [
                .. roots
                    .OfType<OpenXmlElement>()
                    .SelectMany(root => root.Descendants<Tag>())
                    .Select(tag => ((string?)tag.Val)?.Trim())
                    .OfType<string>()
                    .Where(key => key.Length > 0)
                    .Distinct(StringComparer.Ordinal),
            ];
        }
        catch (Exception exception) when (exception is OpenXmlPackageException or FileFormatException or InvalidDataException)
        {
            return null;
        }
    }
}
