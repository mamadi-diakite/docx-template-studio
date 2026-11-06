using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocxTemplateStudio.Application.Contracts.Auth;
using DocxTemplateStudio.Application.Contracts.Common;
using DocxTemplateStudio.Application.Contracts.EmploymentContracts;
using Microsoft.AspNetCore.Mvc;

namespace DocxTemplateStudio.Api.IntegrationTests;

public sealed class ContractsEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    private static readonly ContractInput Input =
        new("Lea", "Moulin", "Office manager", "FixedTerm", new DateOnly(2026, 12, 1), new DateOnly(2027, 5, 31), 2300m);

    [Fact]
    public async Task Lists_demo_contracts_with_search_and_paging()
    {
        var client = await LoggedInClientAsync();

        var firstPage = await client.GetFromJsonAsync<PagedResponse<ContractResponse>>("/api/contracts?pageSize=3");
        var search = await client.GetFromJsonAsync<PagedResponse<ContractResponse>>("/api/contracts?search=intern");

        Assert.Equal(3, firstPage!.Items.Count);
        Assert.True(firstPage.TotalCount >= 8);
        Assert.Equal(["Delorme", "Renaud"], search!.Items.Select(c => c.EmployeeLastName));
    }

    [Fact]
    public async Task Creates_reads_updates_and_deletes_a_contract()
    {
        var client = await LoggedInClientAsync();

        var created = await client.PostAsJsonAsync("/api/contracts", Input);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var contract = await created.Content.ReadFromJsonAsync<ContractResponse>();
        Assert.Equal($"/api/contracts/{contract!.Id}", created.Headers.Location?.AbsolutePath);
        Assert.Equal(new DateOnly(2027, 5, 31), contract.EndDate);

        var updated = await client.PutAsJsonAsync($"/api/contracts/{contract.Id}", Input with { Type = "Permanent", EndDate = null });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var read = await client.GetFromJsonAsync<ContractResponse>($"/api/contracts/{contract.Id}");
        Assert.Equal("Permanent", read?.Type);
        Assert.Null(read?.EndDate);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/contracts/{contract.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/contracts/{contract.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/contracts/{contract.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/api/contracts/{contract.Id}", Input)).StatusCode);
    }

    [Fact]
    public async Task Invalid_contract_returns_every_error()
    {
        var client = await LoggedInClientAsync();
        var invalid = Input with { EmployeeFirstName = "", Type = "Freelance", GrossMonthlySalary = -1m };

        var created = await client.PostAsJsonAsync("/api/contracts", invalid);
        var existing = await client.GetFromJsonAsync<PagedResponse<ContractResponse>>("/api/contracts?pageSize=1");
        var updated = await client.PutAsJsonAsync($"/api/contracts/{existing!.Items[0].Id}", invalid);

        foreach (var response in new[] { created, updated })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            Assert.Equal(["employeeFirstName", "grossMonthlySalary", "type"], problem!.Errors.Keys.Order());
        }
    }

    [Fact]
    public async Task Contracts_need_a_token()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/contracts")).StatusCode);
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
}
