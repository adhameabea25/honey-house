using System.Net;
using System.Net.Http.Json;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Analytics;
using HoneyHouse.Application.Features.Auth;
using HoneyHouse.Application.Features.Cart;
using HoneyHouse.Application.Features.Orders;
using HoneyHouse.Application.Features.Users;
using HoneyHouse.Domain.Enums;
using HoneyHouse.IntegrationTests.Infrastructure;
using Shouldly;

namespace HoneyHouse.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AdminUsersAndAnalyticsTests(HoneyHouseApiFactory factory)
{
    [Fact]
    public async Task Admin_creates_manager_who_can_manage_orders_but_not_users()
    {
        var admin = await factory.CreateAdminClientAsync();
        var email = AuthHelper.UniqueEmail("manager");
        var res = await admin.PostAsJsonAsync("/api/v1/admin/users", new CreateStaffRequest("مدير المخزن", email, "01098765432", "Manager123", ["Manager"]));
        res.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await res.Content.ReadFromJsonAsync<AdminUserDto>(HttpExtensions.Json))!.Roles.ShouldBe(["Manager"]);

        var manager = factory.CreateClient().Authorize((await factory.CreateClient().LoginAsync(email, "Manager123")).AccessToken);
        (await manager.GetAsync("/api/v1/admin/orders")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await manager.GetAsync("/api/v1/admin/inventory")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await manager.GetAsync("/api/v1/admin/users")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await manager.GetAsync("/api/v1/admin/analytics/summary")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await manager.GetAsync("/api/v1/admin/coupons")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        (await admin.PostAsJsonAsync("/api/v1/admin/users", new CreateStaffRequest("x", email, "01098765432", "Manager123", ["Manager"])))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await admin.PostAsJsonAsync("/api/v1/admin/users", new CreateStaffRequest("x", AuthHelper.UniqueEmail(), "01098765432", "Manager123", ["Boss"])))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Disabling_a_user_blocks_login_and_refresh()
    {
        var admin = await factory.CreateAdminClientAsync();
        var client = factory.CreateClient();
        var auth = await client.RegisterAsync();

        var res = await admin.PutAsJsonAsync($"/api/v1/admin/users/{auth.User.Id}/status", new UpdateUserStatusRequest(false));
        (await res.Content.ReadFromJsonAsync<AdminUserDto>(HttpExtensions.Json))!.IsActive.ShouldBeFalse();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(auth.User.Email, AuthHelper.DefaultPassword));
        login.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await login.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("account_disabled");
        (await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(auth.RefreshToken))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        (await admin.PutAsJsonAsync($"/api/v1/admin/users/{auth.User.Id}/status", new UpdateUserStatusRequest(true))).EnsureSuccessStatusCode();
        await client.LoginAsync(auth.User.Email, AuthHelper.DefaultPassword);
    }

    [Fact]
    public async Task Admin_cannot_lock_themselves_out()
    {
        var admin = await factory.CreateAdminClientAsync();
        var me = await admin.GetFromJsonAsync<UserDto>("/api/v1/auth/me", HttpExtensions.Json);
        (await admin.PutAsJsonAsync($"/api/v1/admin/users/{me!.Id}/status", new UpdateUserStatusRequest(false))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await admin.PutAsJsonAsync($"/api/v1/admin/users/{me.Id}/roles", new UpdateRolesRequest(["Customer"]))).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Role_change_promotes_customer_to_manager()
    {
        var admin = await factory.CreateAdminClientAsync();
        var auth = await factory.CreateClient().RegisterAsync();
        var res = await admin.PutAsJsonAsync($"/api/v1/admin/users/{auth.User.Id}/roles", new UpdateRolesRequest(["Customer", "Manager"]));
        (await res.Content.ReadFromJsonAsync<AdminUserDto>(HttpExtensions.Json))!.Roles.OrderBy(r => r).ShouldBe(["Customer", "Manager"]);

        var search = await admin.GetFromJsonAsync<PagedResult<AdminUserDto>>($"/api/v1/admin/users?search={auth.User.Email}", HttpExtensions.Json);
        search!.Items.ShouldHaveSingleItem();
        var managers = await admin.GetFromJsonAsync<PagedResult<AdminUserDto>>("/api/v1/admin/users?role=Manager&pageSize=100", HttpExtensions.Json);
        managers!.Items.ShouldContain(u => u.Id == auth.User.Id);
    }

    [Fact]
    public async Task Analytics_reflect_placed_orders()
    {
        var admin = await factory.CreateAdminClientAsync();
        var before = (await admin.GetFromJsonAsync<SalesSummaryDto>("/api/v1/admin/analytics/summary", HttpExtensions.Json))!;

        var product = await admin.CreateProductAsync(price: 150, stock: 20);
        var (client, _) = await factory.CreateCustomerClientAsync();
        var address = await client.CreateAddressAsync(3); // Alexandria, 60 EGP
        await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(product.Variants[0].Id, 4));
        (await client.PostAsJsonAsync("/api/v1/orders", new PlaceOrderRequest(address.Id, PaymentMethod.CashOnDelivery, null, null))).EnsureSuccessStatusCode();

        var after = (await admin.GetFromJsonAsync<SalesSummaryDto>("/api/v1/admin/analytics/summary", HttpExtensions.Json))!;
        after.Orders.ShouldBe(before.Orders + 1);
        after.Revenue.ShouldBe(before.Revenue + 660);
        after.ItemsSold.ShouldBe(before.ItemsSold + 4);
        after.PendingOrdersNow.ShouldBeGreaterThanOrEqualTo(1);
        after.NewCustomers.ShouldBeGreaterThan(0);
        after.Currency.ShouldBe("EGP");

        var top = await admin.GetFromJsonAsync<List<TopProductDto>>("/api/v1/admin/analytics/top-products?limit=50", HttpExtensions.Json);
        top!.ShouldContain(t => t.ProductId == product.Id && t.QuantitySold == 4 && t.Revenue == 600);

        var daily = await admin.GetFromJsonAsync<List<SalesPointDto>>("/api/v1/admin/analytics/sales?groupBy=day", HttpExtensions.Json);
        daily!.Count.ShouldBeInRange(30, 32);
        daily.Sum(p => p.Revenue).ShouldBe(after.Revenue);

        var monthly = await admin.GetFromJsonAsync<List<SalesPointDto>>(
            $"/api/v1/admin/analytics/sales?groupBy=month&from={DateTime.UtcNow.AddMonths(-3):O}", HttpExtensions.Json);
        monthly!.Count.ShouldBeInRange(3, 5);

        var byStatus = await admin.WithLanguage("ar").GetFromJsonAsync<List<StatusCountDto>>("/api/v1/admin/analytics/orders-by-status", HttpExtensions.Json);
        byStatus!.Count.ShouldBe(8);
        byStatus.Single(s => s.Status == OrderStatus.Pending).StatusName.ShouldBe("قيد المراجعة");

        var byGov = await admin.GetFromJsonAsync<List<GovernorateSalesDto>>("/api/v1/admin/analytics/sales-by-governorate", HttpExtensions.Json);
        byGov!.ShouldContain(g => g.Governorate == "الإسكندرية");
    }

    [Fact]
    public async Task Analytics_reject_invalid_ranges()
    {
        var admin = await factory.CreateAdminClientAsync();
        (await admin.GetAsync($"/api/v1/admin/analytics/summary?from={DateTime.UtcNow:O}&to={DateTime.UtcNow.AddDays(-1):O}"))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await admin.GetAsync($"/api/v1/admin/analytics/sales?from={DateTime.UtcNow.AddYears(-5):O}"))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
