using HoneyHouse.API.Common;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Application.Localization;
using HoneyHouse.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HoneyHouse.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configure from JwtOptions lazily so test hosts can override settings.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((o, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                o.MapInboundClaims = true;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtTokenService.CreateSigningKey(jwt.SigningKey),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                o.Events = new JwtBearerEvents
                {
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        await WriteProblemAsync(ctx.HttpContext, StatusCodes.Status401Unauthorized, "unauthorized");
                    },
                    OnForbidden = ctx => WriteProblemAsync(ctx.HttpContext, StatusCodes.Status403Forbidden, "forbidden")
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static Task WriteProblemAsync(HttpContext http, int status, string code)
    {
        var l = http.RequestServices.GetRequiredService<ILocalizer>();
        http.Response.StatusCode = status;
        var problem = new ProblemDetails { Status = status, Title = l[code], Type = $"https://httpstatuses.io/{status}" };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = http.TraceIdentifier;
        return http.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
    }
}
