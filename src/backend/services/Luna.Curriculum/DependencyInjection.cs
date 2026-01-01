namespace Luna.Identity;

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Luna.Identity.Tokens;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IJwtTokenService, DevJwtTokenService>();

        var key = config["Auth:DevJwtKey"] ?? "CHANGE_ME_DEV_ONLY_32+_CHARS_LONG________";
        var issuer = config["Auth:Issuer"] ?? "luna-dev";
        var audience = config["Auth:Audience"] ?? "luna-dev";

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // dev; in prod: true
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(15),
                };

                // Enables: ws://.../ws/sessions?token=...
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query["token"].ToString();
                        var path = ctx.HttpContext.Request.Path;

                        if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/ws"))
                            ctx.Token = accessToken;

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            // Basic roles; refine later
            options.AddPolicy("StudentOnly", p => p.RequireRole("student"));
            options.AddPolicy("ParentOnly", p => p.RequireRole("parent"));
        });

        return services;
    }
}