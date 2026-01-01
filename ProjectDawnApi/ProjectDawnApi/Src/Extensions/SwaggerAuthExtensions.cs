using Microsoft.AspNetCore.Builder;

namespace ProjectDawnApi
{
    public static class SwaggerAuthExtensions
    {
        public static IApplicationBuilder UseSwaggerBasicAuth(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SwaggerAuthMiddleware>();
        }
    }
}