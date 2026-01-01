using Microsoft.AspNetCore.Http;
using System.Text;

namespace ProjectDawnApi
{
    public class SwaggerAuthMiddleware
    {
        private readonly RequestDelegate next;

        public SwaggerAuthMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only protect swagger
            if (!context.Request.Path.StartsWithSegments("/swagger"))
            {
                await next(context);
                return;
            }

            const string realm = "Swagger";

            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                Challenge(context, realm);
                return;
            }

            var auth = authHeader.ToString();
            if (!auth.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                Challenge(context, realm);
                return;
            }

            string decoded;
            try
            {
                var encoded = auth["Basic ".Length..].Trim();
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            }
            catch
            {
                Challenge(context, realm);
                return;
            }

            var parts = decoded.Split(':', 2);
            if (parts.Length != 2)
            {
                Challenge(context, realm);
                return;
            }

            var username = parts[0];
            var password = parts[1];

            var expectedUser = Environment.GetEnvironmentVariable("SWAGGER_USER");
            var expectedPass = Environment.GetEnvironmentVariable("SWAGGER_PASSWORD");

            if (string.IsNullOrEmpty(expectedUser) || string.IsNullOrEmpty(expectedPass))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Swagger credentials not configured.");
                return;
            }

            if (username != expectedUser || password != expectedPass)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            await next(context);
        }

        private static void Challenge(HttpContext context, string realm)
        {
            context.Response.Headers["WWW-Authenticate"] = $"Basic realm=\"{realm}\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    }
}