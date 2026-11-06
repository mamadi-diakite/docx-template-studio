using Microsoft.Extensions.DependencyInjection;
using DocxTemplateStudio.Application.Auth;

namespace DocxTemplateStudio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();

        return services;
    }
}
