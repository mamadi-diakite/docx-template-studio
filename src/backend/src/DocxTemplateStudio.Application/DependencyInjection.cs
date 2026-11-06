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
