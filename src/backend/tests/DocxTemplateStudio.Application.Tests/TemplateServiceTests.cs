using DocxTemplateStudio.Application.Common;
using DocxTemplateStudio.Application.Templates;
using DocxTemplateStudio.Domain;

namespace DocxTemplateStudio.Application.Tests;

public sealed class TemplateServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly byte[] Content = [0x50, 0x4B, 3, 4];

    private readonly InMemoryTemplateRepository _templates = new();
    private readonly InMemoryFileStorage _storage = new();
    private readonly FakeFieldKeyReader _reader = new();
    private readonly TemplateService _service;

    public TemplateServiceTests()
    {
        _service = new TemplateService(_templates, _storage, _reader, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Upload_stores_the_file_and_lists_its_field_keys()
    {
        _reader.Keys = ["employee.lastName", "contract.startDate"];

        var result = await UploadAsync(" contracts ", "  Permanent contract ", "C:\\fakepath\\ permanent.DOCX ");

        Assert.Equal(ServiceResultStatus.Ok, result.Status);
        var template = Assert.Single(_templates.Templates);
        Assert.Equal(
            ("Contracts", "Permanent contract", "permanent.DOCX", 4L, Now.UtcDateTime),
            (result.Value!.Module, result.Value.Name, result.Value.FileName, result.Value.SizeBytes, result.Value.UploadedAtUtc));
        Assert.Equal(["employee.lastName", "contract.startDate"], result.Value.FieldKeys);
        Assert.Equal(UserId, template.UploadedById);
        Assert.Equal(Content, _storage.Files[template.StorageKey]);
        Assert.Equal(Content, _reader.LastContent);
        Assert.Equal(1, _templates.SaveCount);
    }

    [Fact]
    public async Task Upload_without_name_uses_the_file_name()
    {
        var result = await UploadAsync("MissionOrders", " ", "mission-order.docx");

        Assert.Equal("mission-order", result.Value?.Name);
    }

    [Theory]
    [InlineData(0L, "template.docx", "Choose a .docx file that is not empty.")]
    [InlineData(TemplateService.MaxTemplateBytes + 1, "template.docx", "The file is larger than 10 MB.")]
    [InlineData(4L, null, "The file has no name.")]
    [InlineData(4L, "folder/", "The file has no name.")]
    [InlineData(4L, "long", "The file name is longer than 255 characters.")]
    [InlineData(4L, "template.doc", "Only Word .docx files can be used as templates.")]
    [InlineData(4L, "template.pdf", "Only Word .docx files can be used as templates.")]
    public async Task Upload_checks_the_file_before_reading_it(long length, string? fileName, string message)
    {
        fileName = fileName == "long" ? new string('a', 251) + ".docx" : fileName;

        var result = await _service.UploadAsync(UserId, "Contracts", "Template", fileName, new MemoryStream(Content), length);

        Assert.Equal([message], result.Errors["file"]);
        Assert.Null(_reader.LastContent);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Upload_rejects_a_file_that_is_not_a_word_document()
    {
        _reader.Keys = null;

        var result = await UploadAsync("Contracts", "Template", "renamed.docx");

        Assert.Equal(["This file is not a readable Word document."], result.Errors["file"]);
        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task Upload_rejects_a_field_key_longer_than_allowed()
    {
        _reader.Keys = ["employee.lastName", "employee." + new string('x', Template.FieldKeyMaxLength)];

        var result = await UploadAsync("Contracts", "Template", "template.docx");

        Assert.Equal(["The field key \"employee.xxxxxxxxxxx…\" is longer than 100 characters."], result.Errors["file"]);
    }

    [Theory]
    [InlineData(null, "Name", "module", "Choose a module.")]
    [InlineData("Invoices", "Name", "module", "Choose a module.")]
    [InlineData("Contracts", "long", "name", "Use at most 150 characters.")]
    [InlineData("Contracts", null, "name", "Name is required.")]
    public async Task Upload_checks_the_module_and_the_name(string? module, string? name, string field, string message)
    {
        name = name == "long" ? new string('n', Template.NameMaxLength + 1) : name;

        var result = await _service.UploadAsync(UserId, module, name, ".docx", new MemoryStream(Content), Content.Length);

        Assert.Equal([message], result.Errors[field]);
        Assert.Empty(_templates.Templates);
    }

    [Fact]
    public async Task Upload_reports_every_error_and_needs_content()
    {
        var result = await _service.UploadAsync(UserId, "", null, null, new MemoryStream(), 0);

        Assert.Equal(["file", "module", "name"], result.Errors.Keys.Order());
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UploadAsync(UserId, "Contracts", "Name", "a.docx", null!, 1));
    }

    [Fact]
    public async Task Lists_all_templates_or_one_module()
    {
        await UploadAsync("Contracts", "Contract", "contract.docx");
        await UploadAsync("MissionOrders", "Mission", "mission.docx");

        var all = await _service.ListAsync(null);
        var missions = await _service.ListAsync(" missionorders ");

        Assert.Equal(2, all.Value?.Count);
        Assert.Equal(["Mission"], missions.Value!.Select(t => t.Name));
        Assert.Equal(DocumentModule.MissionOrders, _templates.LastFilter);
    }

    [Fact]
    public async Task List_rejects_an_unknown_module()
    {
        var result = await _service.ListAsync("Invoices");

        Assert.Equal(["Choose a module."], result.Errors["module"]);
    }

    [Fact]
    public async Task Gets_downloads_and_deletes_a_template()
    {
        var id = (await UploadAsync("Contracts", "Contract", "contract.docx")).Value!.Id;

        Assert.Equal("Contract", (await _service.GetAsync(id))?.Name);
        var download = await _service.OpenAsync(id);
        Assert.Equal("contract.docx", download?.FileName);
        Assert.Equal(Content, ((MemoryStream)download!.Content).ToArray());

        Assert.True(await _service.DeleteAsync(id));
        Assert.Empty(_storage.Files);
        Assert.Null(await _service.GetAsync(id));
        Assert.Null(await _service.OpenAsync(id));
        Assert.False(await _service.DeleteAsync(id));
    }

    private Task<ServiceResult<Contracts.Templates.TemplateResponse>> UploadAsync(string module, string? name, string fileName) =>
        _service.UploadAsync(UserId, module, name, fileName, new MemoryStream(Content), Content.Length);
}
