using FluentValidation;
using HoneyHouse.Application.Features.Addresses;
using HoneyHouse.Application.Features.Auth;
using HoneyHouse.Application.Features.Cart;
using HoneyHouse.Application.Features.Catalog;
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
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductAdminService, ProductAdminService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<ICartService, CartService>();
        return services;
    }
}
