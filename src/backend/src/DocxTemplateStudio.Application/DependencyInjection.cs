using Microsoft.Extensions.DependencyInjection;
using DocxTemplateStudio.Application.Auth;
using DocxTemplateStudio.Application.EmploymentContracts;

namespace DocxTemplateStudio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ContractService>();

        return services;
    }
}
