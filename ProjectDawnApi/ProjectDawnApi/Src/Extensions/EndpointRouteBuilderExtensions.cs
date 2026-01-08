namespace ProjectDawnApi;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProjectDawnHubs(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<FarmHub>("/FarmHub");
        endpoints.MapHub<PlayerHub>("/PlayerHub");
        endpoints.MapHub<FarmListHub>("/FarmListHub");

        return endpoints;
    }
}