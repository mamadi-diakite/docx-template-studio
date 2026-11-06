using Microsoft.Extensions.DependencyInjection;
using DocxTemplateStudio.Application.Auth;
using DocxTemplateStudio.Application.EmploymentContracts;
using DocxTemplateStudio.Application.Security;
using DocxTemplateStudio.Application.Users;

namespace DocxTemplateStudio.Application.Tests;

public sealed class ApplicationDependencyInjectionTests
{
    [Fact]
    public void Registers_the_auth_service()
    {
        var services = new ServiceCollection()
            .AddApplication()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IUserRepository, InMemoryUserRepository>()
            .AddSingleton<IPasswordHasher, FakePasswordHasher>()
            .AddSingleton<IAccessTokenIssuer, FakeTokenIssuer>()
            .AddSingleton<IContractRepository, InMemoryContractRepository>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<AuthService>());
        Assert.NotNull(scope.ServiceProvider.GetService<ContractService>());
    }
}
