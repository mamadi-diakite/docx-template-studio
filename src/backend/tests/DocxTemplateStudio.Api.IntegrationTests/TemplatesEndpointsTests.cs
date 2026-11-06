using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocxTemplateStudio.Api.Auth;
using DocxTemplateStudio.Application.Contracts.Auth;
using DocxTemplateStudio.Application.Contracts.Templates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DocxTemplateStudio.Api.IntegrationTests;

public sealed class TemplatesEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    [Fact]
    public async Task Uploads_lists_downloads_and_deletes_a_template()
    {
        var client = await LoggedInClientAsync();
        var docx = CreateDocx("employee.lastName", "contract.startDate", "employee.lastName");

        var created = await client.PostAsync("/api/templates", Form("Contracts", "Permanent contract", "permanent.docx", docx));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var template = await created.Content.ReadFromJsonAsync<TemplateResponse>();
        Assert.Equal($"/api/templates/{template!.Id}", created.Headers.Location?.AbsolutePath);
        Assert.Equal(["employee.lastName", "contract.startDate"], template.FieldKeys);
        Assert.Equal("Contracts", template.Module);

        var contracts = await client.GetFromJsonAsync<List<TemplateResponse>>("/api/templates?module=contracts");
        var missions = await client.GetFromJsonAsync<List<TemplateResponse>>("/api/templates?module=MissionOrders");
        Assert.Contains(contracts!, t => t.Id == template.Id);
        Assert.DoesNotContain(missions!, t => t.Id == template.Id);
        Assert.Equal("Permanent contract", (await client.GetFromJsonAsync<TemplateResponse>($"/api/templates/{template.Id}"))?.Name);

        var download = await client.GetAsync($"/api/templates/{template.Id}/file");
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal("permanent.docx", download.Content.Headers.ContentDisposition?.FileNameStar);
        Assert.Equal(docx, await download.Content.ReadAsByteArrayAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/templates/{template.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/templates/{template.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/templates/{template.Id}/file")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/templates/{template.Id}")).StatusCode);
        Assert.Empty(Directory.GetFiles(factory.StorageRootPath));
    }

    [Fact]
    public async Task Invalid_uploads_return_every_error()
    {
        var client = await LoggedInClientAsync();

        var notWord = await client.PostAsync("/api/templates", Form("Invoices", null, "renamed.docx", "plain text"u8.ToArray()));
        var noFile = await client.PostAsync("/api/templates", new MultipartFormDataContent { { new StringContent("Contracts"), "module" } });

        await AssertInvalidAsync(notWord, "file", "module");
        await AssertInvalidAsync(noFile, "file", "name");
    }

    [Fact]
    public async Task Unknown_module_filter_returns_a_validation_problem()
    {
        var client = await LoggedInClientAsync();

        await AssertInvalidAsync(await client.GetAsync("/api/templates?module=Invoices"), "module");
    }

    [Fact]
    public async Task Templates_need_a_token_with_a_user()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/templates")).StatusCode);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithoutSubject());
        var response = await client.PostAsync("/api/templates", Form("Contracts", "Name", "a.docx", CreateDocx()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static MultipartFormDataContent Form(string module, string? name, string fileName, byte[] content)
    {
        var form = new MultipartFormDataContent { { new StringContent(module), "module" } };
        if (name is not null)
        {
            form.Add(new StringContent(name), "name");
        }

        form.Add(new ByteArrayContent(content), "file", fileName);
        return form;
    }

    private static byte[] CreateDocx(params string[] keys)
    {
        using var stream = new MemoryStream();
        using (var word = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            word.AddMainDocumentPart().Document = new Document(new Body(new Paragraph(
                keys.Select(key => new SdtRun(new SdtProperties(new Tag { Val = key }), new SdtContentRun(new Run(new Text(key))))))));
        }

        return stream.ToArray();
    }

    private static async Task AssertInvalidAsync(HttpResponseMessage response, params string[] fields)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(fields, problem!.Errors.Keys.Order());
    }

    private async Task<HttpClient> LoggedInClientAsync()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("julie.lambert@example.com", "TestDemo123!"));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    private string TokenWithoutSubject()
    {
        var settings = factory.Services.GetRequiredService<JwtSettings>();

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { [JwtClaims.Email] = "ghost@example.com" },
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
        });
    }
}
