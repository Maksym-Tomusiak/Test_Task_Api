using Api.Modules.Validators;
using FluentValidation;

namespace Api.Modules;

public static class SetupModule
{
    public static void SetupServices(this IServiceCollection services)
    {
        services.AddValidators();
        services.AddValidationFilter();
    }

    private static void AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Program>();
    }
    
    private static void AddValidationFilter(this IServiceCollection services)
    {
        services.AddScoped<ValidationFilter>();
    }
}