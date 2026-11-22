using System.Globalization;
using HoneyHouse.Admin.Localization;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Domain.Common;
using HoneyHouse.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace HoneyHouse.Admin.Common;

public static class AdminServiceExtensions
{
    public const string StaffPolicy = "Staff";
    public const string AdminPolicy = "AdminOnly";
    public static readonly string[] SupportedCultures = ["ar", "en"];

    public static IServiceCollection AddAdminServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllersWithViews(o =>
        {
            o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            // Forms are validated with FluentValidation and localized text, not MVC's English defaults
            o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            var m = o.ModelBindingMessageProvider;
            m.SetValueMustNotBeNullAccessor(_ => AdminText.Static("required"));
            m.SetMissingBindRequiredValueAccessor(_ => AdminText.Static("required"));
            m.SetValueIsInvalidAccessor(_ => AdminText.Static("invalid_value"));
            m.SetAttemptedValueIsInvalidAccessor((_, _) => AdminText.Static("invalid_value"));
            m.SetUnknownValueIsInvalidAccessor(_ => AdminText.Static("invalid_value"));
            m.SetValueMustBeANumberAccessor(_ => AdminText.Static("invalid_number"));
            m.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => AdminText.Static("invalid_value"));
            m.SetNonPropertyUnknownValueIsInvalidAccessor(() => AdminText.Static("invalid_value"));
            m.SetNonPropertyValueMustBeANumberAccessor(() => AdminText.Static("invalid_number"));
        });
        // Emit Arabic text as-is instead of &#x...; entities (smaller pages, readable source)
        services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
            o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));
        services.AddHttpContextAccessor();
        services.RemoveAll<ICurrentUser>();
        services.AddScoped<ICurrentUser, AdminCurrentUser>();
        services.AddSingleton<IText, AdminText>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.Cookie.Name = "hh_admin";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Strict;
                o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.LoginPath = "/Account/Login";
                o.LogoutPath = "/Account/Logout";
                o.AccessDeniedPath = "/Account/AccessDenied";
                o.ExpireTimeSpan = TimeSpan.FromHours(configuration.GetValue("Admin:SessionHours", 8));
                o.SlidingExpiration = true;
                o.EventsType = typeof(StaffCookieEvents);
            });
        services.AddScoped<StaffCookieEvents>();

        services.AddAuthorizationBuilder()
            .AddPolicy(StaffPolicy, p => p.RequireRole(Roles.Admin, Roles.Manager))
            .AddPolicy(AdminPolicy, p => p.RequireRole(Roles.Admin));

        services.Configure<RequestLocalizationOptions>(o =>
        {
            var cultures = SupportedCultures.Select(CreateCulture).ToList();
            o.DefaultRequestCulture = new RequestCulture(cultures[0]);
            o.SupportedCultures = cultures;
            o.SupportedUICultures = cultures;
            o.RequestCultureProviders =
            [
                new QueryStringRequestCultureProvider { QueryStringKey = "lang", UIQueryStringKey = "lang" },
                new CookieRequestCultureProvider()
            ];
        });

        return services;
    }

    /// <summary>
    /// UI language only: numbers keep Latin digits and "." decimals and dates stay Gregorian in both languages,
    /// so prices typed in forms and shown in tables read the same for every user.
    /// </summary>
    private static CultureInfo CreateCulture(string name)
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(name).Clone();
        culture.NumberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        culture.DateTimeFormat.Calendar = new GregorianCalendar();
        return culture;
    }

    /// <summary>Serves product images saved by the dashboard so staff can preview them (same folder the API serves).</summary>
    public static WebApplication UseUploadedFiles(this WebApplication app)
    {
        var storage = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
        var root = Path.GetFullPath(storage.RootPath);
        Directory.CreateDirectory(root);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(root),
            RequestPath = storage.RequestPath
        });
        return app;
    }
}
