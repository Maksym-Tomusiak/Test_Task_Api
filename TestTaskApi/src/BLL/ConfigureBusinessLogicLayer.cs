using BLL.Modules;
using BLL.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BLL;

public static class ConfigureBusinessLogicLayer
{
    public static void ConfigureBusinessLogic(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddServices(configuration);
        
        services.SetupServices();
    }
}