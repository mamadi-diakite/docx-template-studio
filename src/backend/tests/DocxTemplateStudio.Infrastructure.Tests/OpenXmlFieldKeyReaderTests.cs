namespace DocxTemplateStudio.Infrastructure.Tests;

public sealed class OpenXmlFieldKeyReaderTests
{
    private readonly OpenXmlFieldKeyReader _reader = new();

    [Fact]
    public void Reads_keys_from_body_headers_and_footers_in_order_without_duplicates()
    {
        using var document = TestDocx.Create(
            ["employee.lastName", " employee.firstName ", "employee.lastName", "", "   "],
            headerKeys: ["company.name"],
            footerKeys: ["contract.startDate", "company.name"]);

        var keys = _reader.ReadFieldKeys(document);

        Assert.Equal(["employee.lastName", "employee.firstName", "company.name", "contract.startDate"], keys);
    }

    [Fact]
    public void A_document_without_content_controls_has_no_keys()
    {
        using var document = TestDocx.Create([]);

        Assert.Empty(_reader.ReadFieldKeys(document)!);
    }

    [Fact]
    public void Returns_null_for_bytes_that_are_not_a_zip()
    {
        using var document = new MemoryStream("not a Word document"u8.ToArray());

        Assert.Null(_reader.ReadFieldKeys(document));
    }

    [Fact]
    public void Returns_null_for_a_zip_that_is_not_a_package()
    {
        using var document = new MemoryStream();
        using (var zip = new ZipArchive(document, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(zip.CreateEntry("readme.txt").Open());
            writer.Write("plain zip");
        }

        document.Position = 0;

        Assert.Null(_reader.ReadFieldKeys(document));
    }

    [Fact]
    public void Rejects_a_missing_stream()
    {
        Assert.Throws<ArgumentNullException>(() => _reader.ReadFieldKeys(null!));
    }
}
