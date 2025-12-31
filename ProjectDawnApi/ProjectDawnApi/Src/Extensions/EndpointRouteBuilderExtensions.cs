namespace ProjectDawnApi;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapProjectDawnHubs(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<FarmHub>("/farmHub");

        return endpoints;
    }
}
