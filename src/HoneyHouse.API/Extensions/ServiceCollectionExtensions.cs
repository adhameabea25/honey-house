using System.Globalization;
using System.Threading.RateLimiting;
using HoneyHouse.API.Middleware;
using HoneyHouse.Application.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

namespace HoneyHouse.API.Extensions;

public static class ServiceCollectionExtensions
{
    public const string AuthRateLimitPolicy = "auth";
    public static readonly string[] SupportedCultures = ["ar", "en"];

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(o => o.Filters.Add<ValidationFilter>())
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(o => o.SuppressModelStateInvalidFilter = false);
        services.AddScoped<ValidationFilter>();

        services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
            ctx.ProblemDetails.Type ??= $"https://httpstatuses.io/{ctx.ProblemDetails.Status}";
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Map automatic [ApiController] model-binding errors (e.g. malformed JSON) to our localized format
        services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = ctx =>
        {
            var l = ctx.HttpContext.RequestServices.GetRequiredService<ILocalizer>();
            var problem = new ValidationProblemDetails(ctx.ModelState) { Status = 400, Title = l["validation_failed"] };
            problem.Extensions["code"] = "validation_failed";
            return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
        });

        services.Configure<RequestLocalizationOptions>(o =>
        {
            var cultures = SupportedCultures.Select(c => new CultureInfo(c)).ToList();
            o.DefaultRequestCulture = new RequestCulture("ar");
            o.SupportedCultures = cultures;
            o.SupportedUICultures = cultures;
            o.RequestCultureProviders =
            [
                new QueryStringRequestCultureProvider { QueryStringKey = "lang", UIQueryStringKey = "lang" },
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        });

        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(AuthRateLimitPolicy, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = configuration.GetValue("RateLimiting:AuthPermitPerMinute", 10),
                    Window = TimeSpan.FromMinutes(1)
                }));
            o.OnRejected = async (ctx, ct) =>
            {
                var l = ctx.HttpContext.RequestServices.GetRequiredService<ILocalizer>();
                await ctx.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = 429, Title = l["too_many_requests"], Extensions = { ["code"] = "too_many_requests" }
                }, ct);
            };
        });

        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(o => o.AddDefaultPolicy(p =>
        {
            if (origins.Length == 0) p.AllowAnyOrigin(); else p.WithOrigins(origins);
            p.AllowAnyHeader().AllowAnyMethod();
        }));

        services.AddHealthChecks().AddDbContextCheck<HoneyHouse.Infrastructure.Persistence.AppDbContext>("database");

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Honey House API",
                Version = "v1",
                Description = "E-commerce API for natural honey and bee products (Egypt). Send `Accept-Language: ar|en`. Currency: EGP."
            });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the access token returned by /api/v1/auth/login"
            });
            o.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
            });
            var xml = Path.Combine(AppContext.BaseDirectory, "HoneyHouse.API.xml");
            if (File.Exists(xml)) o.IncludeXmlComments(xml);
        });

        return services;
    }
}
