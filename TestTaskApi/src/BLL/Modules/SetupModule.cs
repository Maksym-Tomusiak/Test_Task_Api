using System.Reflection;
using BLL.Modules.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BLL.Modules;

public static class SetupModule
{
    public static void SetupServices(this IServiceCollection services)
    {
        services.AddValidators();
        services.AddValidationFilter();
    }

    private static void AddValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }
    
    private static void AddValidationFilter(this IServiceCollection services)
    {
        services.AddScoped<ValidationFilter>();
    }
}