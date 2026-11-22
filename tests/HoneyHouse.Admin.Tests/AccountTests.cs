using System.Net;
using HoneyHouse.Admin.Tests.Infrastructure;
using HoneyHouse.Domain.Common;

namespace HoneyHouse.Admin.Tests;

[Collection(AdminCollection.Name)]
public class AccountTests(AdminAppFactory factory)
{
    [Fact]
    public async Task Anonymous_user_is_redirected_to_login()
    {
        var response = await factory.CreateBrowser().GetAsync("/");
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Location().ShouldContain("/Account/Login");
    }

    [Fact]
    public async Task Login_page_is_arabic_rtl_by_default()
    {
        var html = await factory.CreateBrowser().GetPageAsync("/Account/Login");
        html.ShouldContain("dir=\"rtl\"");
        html.ShouldContain("bootstrap.rtl.min.css");
        html.ShouldContain("تسجيل دخول الإدارة");
    }

    [Fact]
    public async Task Admin_can_sign_in_and_see_dashboard()
    {
        var client = await factory.SignedInAsync();
        var html = await client.GetPageAsync("/");
        html.ShouldContain("بيت العسل");
        html.ShouldContain("مدير النظام");
        html.ShouldContain("/Users"); // admin-only menu item
    }

    [Fact]
    public async Task Return_url_is_honoured_only_when_local()
    {
        var client = factory.CreateBrowser();
        var response = await client.SubmitAsync("/Account/Login", "/Account/Login",
            new { Email = AdminAppFactory.AdminEmail, Password = AdminAppFactory.Password, ReturnUrl = "https://evil.example/" });
        response.Location().ShouldBe("/");

        var client2 = factory.CreateBrowser();
        var local = await client2.SubmitAsync("/Account/Login", "/Account/Login",
            new { Email = AdminAppFactory.AdminEmail, Password = AdminAppFactory.Password, ReturnUrl = "/Orders" });
        local.Location().ShouldBe("/Orders");
    }

    [Fact]
    public async Task Wrong_password_shows_error()
    {
        var response = await factory.CreateBrowser().SubmitAsync("/Account/Login", "/Account/Login",
            new { Email = AdminAppFactory.AdminEmail, Password = "wrong-pass1" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("البريد الإلكتروني أو كلمة المرور غير صحيحة");
    }

    [Fact]
    public async Task Empty_form_shows_required_errors()
    {
        var response = await factory.CreateBrowser().SubmitAsync("/Account/Login", "/Account/Login", new { Email = "", Password = "" });
        (await response.Content.ReadAsStringAsync()).ShouldContain("هذا الحقل مطلوب");
    }

    [Fact]
    public async Task Customer_cannot_sign_in()
    {
        var email = await factory.CreateUserAsync(Roles.Customer);
        var response = await factory.CreateBrowser().SubmitAsync("/Account/Login", "/Account/Login", new { Email = email, Password = AdminAppFactory.Password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("ليس له صلاحية دخول لوحة التحكم");
    }

    [Fact]
    public async Task Disabled_staff_cannot_sign_in()
    {
        var email = await factory.CreateUserAsync(Roles.Manager);
        await factory.UpdateUserAsync(email, async (users, u) => { u.IsActive = false; await users.UpdateAsync(u); });
        var response = await factory.CreateBrowser().SubmitAsync("/Account/Login", "/Account/Login", new { Email = email, Password = AdminAppFactory.Password });
        (await response.Content.ReadAsStringAsync()).ShouldContain("هذا الحساب موقوف");
    }

    [Fact]
    public async Task Manager_signs_in_without_admin_menu()
    {
        var email = await factory.CreateUserAsync(Roles.Manager);
        var client = await factory.SignedInAsync(email);
        var html = await client.GetPageAsync("/");
        html.ShouldContain("مشرف");
        html.ShouldNotContain("href=\"/Users\"");
    }

    [Fact]
    public async Task Disabling_staff_ends_their_session()
    {
        var email = await factory.CreateUserAsync(Roles.Manager);
        var client = await factory.SignedInAsync(email);
        await client.GetPageAsync("/");

        await factory.UpdateUserAsync(email, async (users, u) => { u.IsActive = false; await users.UpdateAsync(u); });

        var response = await client.GetAsync("/");
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Location().ShouldContain("/Account/Login");
    }

    [Fact]
    public async Task Removing_staff_role_ends_their_session()
    {
        var email = await factory.CreateUserAsync(Roles.Manager);
        var client = await factory.SignedInAsync(email);

        await factory.UpdateUserAsync(email, async (users, u) =>
        {
            await users.RemoveFromRoleAsync(u, Roles.Manager);
            await users.AddToRoleAsync(u, Roles.Customer);
        });

        (await client.GetAsync("/")).Location().ShouldContain("/Account/Login");
    }

    [Fact]
    public async Task Logout_ends_session()
    {
        var client = await factory.SignedInAsync();
        var page = await client.GetPageAsync("/");
        var response = await client.PostFormAsync("/Account/Logout", [], BrowserExtensions.Token(page));
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await client.GetAsync("/")).Location().ShouldContain("/Account/Login");
    }

    [Fact]
    public async Task Post_without_antiforgery_token_is_rejected()
    {
        var response = await factory.CreateBrowser().PostFormAsync("/Account/Login",
            [new("Email", AdminAppFactory.AdminEmail), new("Password", AdminAppFactory.Password)], token: null);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Language_switch_turns_ui_english_ltr()
    {
        var client = factory.CreateBrowser();
        var response = await client.SubmitAsync("/Account/Login", "/Language/Set", new { culture = "en", returnUrl = "/Account/Login" });
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Location().ShouldBe("/Account/Login");

        var html = await client.GetPageAsync("/Account/Login");
        html.ShouldContain("dir=\"ltr\"");
        html.ShouldContain("Staff sign in");
        html.ShouldNotContain("bootstrap.rtl.min.css");
    }

    [Fact]
    public async Task Unknown_page_shows_localized_404()
    {
        var client = await factory.SignedInAsync();
        var html = await client.GetPageAsync("/NoSuchPage/Here", HttpStatusCode.NotFound);
        html.ShouldContain("الصفحة غير موجودة");
    }
}
