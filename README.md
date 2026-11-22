# Honey House 🍯

E-commerce platform for natural honey and bee products in Egypt. Arabic (RTL) and English, prices in EGP.

| Component | Tech | Status |
|---|---|---|
| Backend API | ASP.NET Core Web API (.NET 10), EF Core 10, SQL Server, Clean Architecture, JWT | ✅ Phase 2 complete |
| Admin dashboard | ASP.NET Core MVC (reuses Application + Infrastructure), cookie auth, Bootstrap 5 RTL | ✅ Phase 3 complete |
| Mobile app | Flutter (Android / iOS) | Phase 4 |

Docs: [Architecture](docs/ARCHITECTURE.md) · [Development progress](DEVELOPMENT_PROGRESS.md)

## Run locally

```bash
docker compose up -d                         # SQL Server 2022 on localhost:1433
dotnet build
dotnet test                                  # unit + integration tests (needs the SQL Server above)
dotnet run --project src/HoneyHouse.API      # http://localhost:5xxx/swagger
dotnet run --project src/HoneyHouse.Admin    # admin dashboard on http://localhost:5100
```

In Development the API migrates the database and seeds roles, the 27 governorates, a sample honey catalog and an admin user
(`admin@honeyhouse.local` / `Admin12345`, development only). The same account signs in to the admin dashboard.

## Solution layout

```
src/HoneyHouse.Domain          entities, enums, domain rules (no dependencies)
src/HoneyHouse.Application     services, DTOs, validators, repository/service interfaces, localization
src/HoneyHouse.Infrastructure  EF Core, repositories, Identity/JWT, Paymob, file storage, seeding
src/HoneyHouse.API             controllers, middleware, auth, Swagger, background jobs
src/HoneyHouse.Admin           MVC admin dashboard (Arabic RTL / English), calls Application services directly
tests/HoneyHouse.UnitTests
tests/HoneyHouse.IntegrationTests   real API + throw-away SQL Server database (override server with HH_TEST_SQL)
tests/HoneyHouse.Admin.Tests        real dashboard + throw-away database, posts forms with antiforgery tokens
```

## Admin dashboard

| Page | Admin | Manager |
|---|---|---|
| Home: sales KPIs and charts | ✅ | operations only (pending orders, low stock, latest orders) |
| Categories, products, weights and prices, images | ✅ | ✅ |
| Inventory: stock adjustments and history | ✅ | ✅ |
| Orders: filters, status changes, mark paid, invoice, CSV export | ✅ | ✅ |
| Reviews moderation | ✅ | ✅ |
| Coupons, shipping fees per governorate, users and staff | ✅ | — |

Arabic is the default; the language button switches to English (LTR). Uploaded images go to `FileStorage:RootPath`;
in Development it points at the API's `wwwroot/uploads` so the API serves the same files.

## API overview (`/api/v1`)

Send `Accept-Language: ar` or `en` (or `?lang=`). Errors are `application/problem+json` with a stable `code`.

| Area | Endpoints |
|---|---|
| Auth | `POST auth/register`, `auth/login`, `auth/refresh`, `auth/logout`, `auth/forgot-password`, `auth/reset-password`, `auth/change-password`; `GET/PUT auth/me` |
| Catalog | `GET categories`, `categories/{id}`, `products` (search/filter/sort/paging), `products/featured`, `products/{id}`, `products/slug/{slug}` |
| Shopping | `GET governorates`; `addresses` CRUD + `PUT addresses/{id}/default`; `cart` get/add/update/remove/clear; `POST coupons/validate` |
| Orders | `POST orders/checkout/preview`, `POST orders`, `GET orders`, `GET orders/{id}`, `GET orders/{id}/tracking`, `POST orders/{id}/cancel` |
| Payments | `POST payments/{orderId}/retry`, `POST payments/paymob/webhook` |
| Reviews & favorites | `GET/POST products/{id}/reviews`, `GET products/{id}/reviews/mine`, `PUT/DELETE reviews/{id}`; `GET favorites`, `GET favorites/ids`, `POST/DELETE favorites/{productId}` |
| Admin (Admin, Manager) | `admin/categories`, `admin/products`, `admin/variants`, product images, `admin/inventory`, `admin/orders` (+ status, mark-paid), `admin/reviews` |
| Admin (Admin only) | `admin/coupons`, `admin/governorates`, `admin/users`, `admin/analytics/*` |

## Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server |
| `Jwt:SigningKey` | ≥ 32 chars, keep secret |
| `Paymob:*` | `Enabled`, `SecretKey`, `PublicKey`, `HmacSecret`, `IntegrationIds`, `NotificationUrl`, `RedirectionUrl` |
| `Cors:AllowedOrigins` | allowed web origins |
| `RateLimiting:AuthPermitPerMinute` | auth endpoints per IP (default 10) |
| `Seed:*` | `ApplyMigrations`, `SampleData`, `AdminEmail`, `AdminPassword` |
