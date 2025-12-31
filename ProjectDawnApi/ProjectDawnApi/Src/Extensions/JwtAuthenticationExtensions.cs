using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ProjectDawnApi;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddProjectDawnAuthentication(
        this IServiceCollection services,
        IConfiguration config)
    {
        var jwtKey = config["Jwt:Key"];
        var jwtIssuer = config["Jwt:Issuer"];
        var jwtAudience = config["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("JWT Key is missing");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        services.AddAuthorization();

        return services;
    }
}
