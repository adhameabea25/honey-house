using System.Net;
using HoneyHouse.Admin.Tests.Infrastructure;
using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyHouse.Admin.Tests;

[Collection(AdminCollection.Name)]
public class UserTests(AdminAppFactory factory)
{
    private async Task<string> IdOf(string email)
    {
        string id = "";
        await factory.WithScopeAsync(async sp => id = (await sp.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email))!.Id);
        return id;
    }

    [Fact]
    public async Task Admin_creates_manager_who_can_then_sign_in()
    {
        var client = await factory.SignedInAsync();
        var email = $"mgr-{Guid.NewGuid():N}@test.local";
        var r = await client.SubmitAsync("/Users/Create", "/Users/Create", new Dictionary<string, string>
        {
            ["FullName"] = "منى المشرفة", ["Email"] = email, ["PhoneNumber"] = "01112345678", ["Password"] = "Manager123", ["Roles"] = Roles.Manager
        });
        r.StatusCode.ShouldBe(HttpStatusCode.Redirect, await r.Content.ReadAsStringAsync());
        r.Location().ShouldStartWith("/Users/Details/");

        var manager = factory.CreateBrowser();
        await manager.LoginAsync(email, "Manager123");
        (await manager.GetPageAsync("/")).ShouldContain("منى المشرفة");
    }

    [Fact]
    public async Task Create_staff_validates_phone_password_and_duplicate_email()
    {
        var client = await factory.SignedInAsync();
        var r = await client.SubmitAsync("/Users/Create", "/Users/Create", new Dictionary<string, string>
        {
            ["FullName"] = "x", ["Email"] = AdminAppFactory.AdminEmail, ["PhoneNumber"] = "123", ["Password"] = "short", ["Roles"] = Roles.Manager
        });
        var html = await r.Content.ReadAsStringAsync();
        html.ShouldContain("رقم الهاتف غير صحيح");
        html.ShouldContain("كلمة المرور يجب ألا تقل عن 8 أحرف");

        var dup = await client.SubmitAsync("/Users/Create", "/Users/Create", new Dictionary<string, string>
        {
            ["FullName"] = "x", ["Email"] = AdminAppFactory.AdminEmail, ["PhoneNumber"] = "01012345678", ["Password"] = "Password123", ["Roles"] = Roles.Manager
        });
        (await dup.Content.ReadAsStringAsync()).ShouldContain("البريد الإلكتروني مستخدم بالفعل");
    }

    [Fact]
    public async Task Disabling_user_blocks_dashboard_session_and_enabling_restores()
    {
        var email = await factory.CreateUserAsync(Roles.Manager);
        var id = await IdOf(email);
        var manager = await factory.SignedInAsync(email);
        var admin = await factory.SignedInAsync();

        (await admin.SubmitAsync($"/Users/Details/{id}", $"/Users/Status/{id}", new { isActive = "false" })).StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await admin.GetPageAsync($"/Users/Details/{id}")).ShouldContain("data-testid=\"user-status\">موقوف<");
        (await manager.GetAsync("/")).Location().ShouldContain("/Account/Login");

        (await admin.SubmitAsync($"/Users/Details/{id}", $"/Users/Status/{id}", new { isActive = "true" })).StatusCode.ShouldBe(HttpStatusCode.Redirect);
        await factory.CreateBrowser().LoginAsync(email);
    }

    [Fact]
    public async Task Promoting_manager_to_admin_unlocks_admin_pages()
    {
        var email = await factory.CreateUserAsync(Roles.Manager);
        var id = await IdOf(email);
        var manager = await factory.SignedInAsync(email);
        (await manager.GetAsync("/Coupons")).Location().ShouldContain("AccessDenied");

        var admin = await factory.SignedInAsync();
        var r = await admin.SubmitAsync($"/Users/Details/{id}", $"/Users/Roles/{id}",
            new List<KeyValuePair<string, string>> { new("roles", Roles.Manager), new("roles", Roles.Admin) });
        r.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        // Same cookie: roles are refreshed on the next request
        await manager.GetPageAsync("/Coupons");
    }

    [Fact]
    public async Task Admin_cannot_disable_or_demote_self()
    {
        var admin = await factory.SignedInAsync();
        var id = await IdOf(AdminAppFactory.AdminEmail);
        await admin.SubmitAsync($"/Users/Details/{id}", $"/Users/Roles/{id}", new List<KeyValuePair<string, string>> { new("roles", Roles.Manager) });
        (await admin.GetPageAsync($"/Users/Details/{id}")).ShouldContain("لا يمكنك إيقاف حسابك");
        await factory.WithScopeAsync(async sp =>
        {
            var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
            (await users.IsInRoleAsync((await users.FindByIdAsync(id))!, Roles.Admin)).ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Empty_roles_are_refused()
    {
        var email = await factory.CreateUserAsync(Roles.Customer);
        var id = await IdOf(email);
        var admin = await factory.SignedInAsync();
        await admin.SubmitAsync($"/Users/Details/{id}", $"/Users/Roles/{id}", new { });
        (await admin.GetPageAsync($"/Users/Details/{id}")).ShouldContain("اختر صلاحية واحدة على الأقل");
    }

    [Fact]
    public async Task List_filters_by_role_and_search()
    {
        var email = await factory.CreateUserAsync(Roles.Customer);
        var admin = await factory.SignedInAsync();
        (await admin.GetPageAsync("/Users?role=Customer&search=" + Uri.EscapeDataString(email))).ShouldContain($">{email}</td>");
        (await admin.GetPageAsync("/Users?role=Manager&search=" + Uri.EscapeDataString(email))).ShouldNotContain($">{email}</td>");
    }

    [Fact]
    public async Task Managers_cannot_open_users()
    {
        var manager = await factory.SignedInAsync(await factory.CreateUserAsync(Roles.Manager));
        (await manager.GetAsync("/Users")).Location().ShouldContain("/Account/AccessDenied");
    }
}
