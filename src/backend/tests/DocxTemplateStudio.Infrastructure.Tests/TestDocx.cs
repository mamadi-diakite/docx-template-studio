using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocxTemplateStudio.Infrastructure.Tests;

/// <summary>Builds small Word documents with tagged content controls.</summary>
internal static class TestDocx
{
    public static MemoryStream Create(string[] bodyKeys, string[]? headerKeys = null, string[]? footerKeys = null)
    {
        var stream = new MemoryStream();
        using (var word = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var main = word.AddMainDocumentPart();
            main.Document = new Document(new Body(
                new Paragraph(bodyKeys.Select(ContentControl)),
                new Table(new TableRow(new TableCell(new Paragraph(new Run(new Text("Table cell"))))))));

            if (headerKeys is not null)
            {
                main.AddNewPart<HeaderPart>().Header = new Header(new Paragraph(headerKeys.Select(ContentControl)));
            }

            if (footerKeys is not null)
            {
                main.AddNewPart<FooterPart>().Footer = new Footer(new Paragraph(footerKeys.Select(ContentControl)));
            }
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>A content control whose tag is <paramref name="key"/>; an empty key gives a control without tag value.</summary>
    private static OpenXmlElement ContentControl(string key)
    {
        var properties = new SdtProperties(key.Length == 0 ? new Tag() : new Tag { Val = key });

        return new SdtRun(properties, new SdtContentRun(new Run(new Text(key))));
    }
}
