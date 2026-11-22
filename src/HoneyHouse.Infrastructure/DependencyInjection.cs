using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Infrastructure.Email;
using HoneyHouse.Infrastructure.Identity;
using HoneyHouse.Infrastructure.Payments;
using HoneyHouse.Infrastructure.Storage;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Infrastructure.Persistence;
using HoneyHouse.Infrastructure.Persistence.Repositories;
using HoneyHouse.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyHouse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<AppDbContext>((sp, o) => o.UseSqlServer(GetConnectionString(sp.GetRequiredService<IConfiguration>()), sql =>
        {
            sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            sql.EnableRetryOnFailure(3);
            sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        })
        // Variants, order items and inventory rows intentionally survive product soft-deletes
        // (historical orders must stay readable), so required navigations to filtered entities are expected.
        .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

        services.AddDataProtection();
        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireLowercase = true;
                o.Password.RequireDigit = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();

        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.Section));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.Configure<PaymobOptions>(configuration.GetSection(PaymobOptions.Section));
        services.AddHttpClient<IPaymentGateway, PaymobGateway>(c => c.Timeout = TimeSpan.FromSeconds(20));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();

        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.Section));
        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    // Read lazily so hosts (e.g. integration tests) can override configuration after registration.
    private static string GetConnectionString(IConfiguration configuration)
    {
        var cs = configuration.GetConnectionString("DefaultConnection");
        return string.IsNullOrWhiteSpace(cs)
            ? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.")
            : cs;
    }
}
