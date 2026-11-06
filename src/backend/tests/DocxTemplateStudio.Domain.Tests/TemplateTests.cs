namespace DocxTemplateStudio.Domain.Tests;

public sealed class TemplateTests
{
    private static readonly Guid UploaderId = Guid.NewGuid();

    [Fact]
    public void Creates_a_template_with_unique_keys_in_document_order()
    {
        var template = new Template(
            DocumentModule.Contracts,
            " Employment contract ",
            " employment-contract.DOCX ",
            "key-1",
            20480,
            ["employee.lastName", "contract.startDate", "employee.lastName"],
            UploaderId,
            TestDates.Now);

        Assert.NotEqual(Guid.Empty, template.Id);
        Assert.Equal(DocumentModule.Contracts, template.Module);
        Assert.Equal("Employment contract", template.Name);
        Assert.Equal("employment-contract.DOCX", template.FileName);
        Assert.Equal("key-1", template.StorageKey);
        Assert.Equal(20480, template.SizeBytes);
        Assert.Equal(["employee.lastName", "contract.startDate"], template.FieldKeys);
        Assert.Equal(UploaderId, template.UploadedById);
        Assert.Equal(TestDates.Now, template.UploadedAtUtc);
    }

    [Fact]
    public void A_template_can_have_no_keys()
    {
        var template = NewTemplate([]);

        Assert.Empty(template.FieldKeys);
    }

    [Fact]
    public void Keys_are_case_sensitive()
    {
        var template = NewTemplate(["employee.name", "Employee.Name"]);

        Assert.Equal(2, template.FieldKeys.Length);
    }

    [Theory]
    [InlineData("contract.pdf")]
    [InlineData("contract.doc")]
    [InlineData("contract.docx.exe")]
    public void Rejects_files_that_are_not_docx(string fileName)
    {
        var error = Assert.Throws<ArgumentException>(() => new Template(
            DocumentModule.Contracts, "Contract", fileName, "key", 100, [], UploaderId, TestDates.Now));

        Assert.Equal("fileName", error.ParamName);
    }

    [Fact]
    public void Rejects_blank_or_too_long_keys()
    {
        Assert.Throws<ArgumentException>(() => NewTemplate(["employee.name", " "]));
        Assert.Throws<ArgumentException>(() => NewTemplate([new string('k', Template.FieldKeyMaxLength + 1)]));
    }

    [Fact]
    public void Rejects_missing_key_list()
    {
        Assert.Throws<ArgumentNullException>(() => NewTemplate(null!));
    }

    [Theory]
    [InlineData(" ", "a.docx", "key", 100L, "name")]
    [InlineData("Contract", " ", "key", 100L, "fileName")]
    [InlineData("Contract", "a.docx", " ", 100L, "storageKey")]
    [InlineData("Contract", "a.docx", "key", 0L, "sizeBytes")]
    public void Rejects_invalid_file_details(string name, string fileName, string storageKey, long size, string paramName)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new Template(
            DocumentModule.Contracts, name, fileName, storageKey, size, [], UploaderId, TestDates.Now));

        Assert.Equal(paramName, error.ParamName);
    }

    [Fact]
    public void Rejects_unknown_module()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new Template(
            (DocumentModule)9, "Contract", "a.docx", "key", 100, [], UploaderId, TestDates.Now));

        Assert.Equal("module", error.ParamName);
    }

    [Fact]
    public void Rejects_empty_uploader()
    {
        var error = Assert.Throws<ArgumentException>(() => new Template(
            DocumentModule.Contracts, "Contract", "a.docx", "key", 100, [], Guid.Empty, TestDates.Now));

        Assert.Equal("uploadedById", error.ParamName);
    }

    [Fact]
    public void Rejects_non_utc_upload_date()
    {
        var error = Assert.Throws<ArgumentException>(() => new Template(
            DocumentModule.Contracts, "Contract", "a.docx", "key", 100, [], UploaderId,
            DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Local)));

        Assert.Equal("uploadedAtUtc", error.ParamName);
    }

    private static Template NewTemplate(IEnumerable<string> keys) =>
        new(DocumentModule.Contracts, "Contract", "contract.docx", "key", 100, keys, UploaderId, TestDates.Now);
}
