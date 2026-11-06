using Microsoft.Extensions.DependencyInjection;
using DocxTemplateStudio.Application.Auth;
using DocxTemplateStudio.Application.EmploymentContracts;
using DocxTemplateStudio.Application.Templates;

namespace DocxTemplateStudio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ContractService>();
        services.AddScoped<TemplateService>();

        return services;
    }
}
