using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.Http;

namespace Application;

public static class ConfigureApplication
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddWolverineHttp();
        
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }
}