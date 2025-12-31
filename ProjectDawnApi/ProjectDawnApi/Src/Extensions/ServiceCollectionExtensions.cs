using ProjectDawnApi.Src.DBCommunicators.Farm;
using ProjectDawnApi.Src.DBCommunicators.Player;
using ProjectDawnApi.Src.Services.Farm;
using ProjectDawnApi.Src.Services.Player;

namespace ProjectDawnApi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProjectDawnServices(
        this IServiceCollection services)
    {
        services.AddScoped<FarmSessionService>();
        services.AddScoped<PlayerTransformationService>();
        services.AddScoped<FarmObjectService>();
        services.AddScoped<FarmSessionDBCommunicator>();
        services.AddScoped<PlayerInventoryDBCommunicator>();
        services.AddScoped<PlayerInventoryService>();

        return services;
    }
}