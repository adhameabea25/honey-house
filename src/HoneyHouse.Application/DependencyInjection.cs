using FluentValidation;
using HoneyHouse.Application.Features.Auth;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Application.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyHouse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        services.AddSingleton<ILanguageContext, CultureLanguageContext>();
        services.AddSingleton<ILocalizer, Localizer>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
