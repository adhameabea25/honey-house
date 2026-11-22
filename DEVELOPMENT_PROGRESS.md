# Honey House — Development Progress

| Phase | Status |
|---|---|
| 1. Architecture | ✅ Approved ([docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)) |
| 2. Backend API | ✅ Complete (155 tests passing) |
| 3. Admin Dashboard (MVC) | ⏳ Not started |
| 4. Flutter App | ⏳ Not started |
| 5. Deployment | ⏳ Not started |

## Phase 2 — Backend tasks

| # | Task | Status | Build | Tests |
|---|---|---|---|---|
| 2.1 | Solution, projects, Domain entities | ✅ Done | ✅ | ✅ 17 unit |
| 2.2 | DbContext, configurations, migration, seed | ✅ Done | ✅ | ✅ 2 unit + 7 integration |
| 2.3 | Error handling, validation, localization, Swagger, logging | ✅ Done | ✅ | ✅ 12 unit + 7 integration |
| 2.4 | Authentication (JWT + refresh) and roles | ✅ Done | ✅ | ✅ 3 unit + 13 integration |
| 2.5 | Catalog: categories, products, variants, images | ✅ Done | ✅ | ✅ 13 unit + 11 integration |
| 2.6 | Addresses, governorates, cart | ✅ Done | ✅ | ✅ 2 unit + 9 integration |
| 2.7 | Coupons | ✅ Done | ✅ | ✅ 8 unit + 10 integration |
| 2.8 | Orders, checkout, inventory | ✅ Done | ✅ | ✅ 12 integration |
| 2.9 | Payments: COD + Paymob | ✅ Done | ✅ | ✅ 8 unit + 7 integration |
| 2.10 | Reviews and favorites | ✅ Done | ✅ | ✅ 3 unit + 5 integration |
| 2.11 | Admin APIs and analytics | ✅ Done | ✅ | ✅ 6 integration |

## Log

### 2026-11-22 — Task 2.1
- Created `HoneyHouse.slnx` with Clean Architecture projects: Domain, Application, Infrastructure, API, UnitTests, IntegrationTests (.NET 10).
- Domain entities: ApplicationUser, RefreshToken, Governorate, Address, Category, Product, ProductImage, ProductVariant (with RowVersion), InventoryTransaction, Cart/CartItem, Coupon/CouponUsage, Order/OrderItem/OrderStatusHistory, Payment, Review, Favorite.
- Domain rules: `Coupon.CalculateDiscount`, `OrderStateMachine`.
- `docker-compose.yml` for local SQL Server 2022.
- Build: 0 errors, 0 warnings. Unit tests: 17/17 passed.

### 2026-11-22 — Task 2.2
- `AppDbContext` (Identity + 18 domain sets): automatic `CreatedAt`/`UpdatedAt`, soft delete with global query filters, decimal(18,2) for all money columns.
- Fluent configurations for every entity: unique indexes (SKU, slugs, order number, coupon code), check constraints (stock ≥ 0, price ≥ 0, rating 1–5, cart quantity > 0), `rowversion` concurrency token on `ProductVariants`, enums stored as strings.
- Repository pattern: `IGenericRepository<T>` + `IUnitOfWork` with `ExecuteInTransactionAsync` (retry-safe execution strategy).
- Migration `InitialCreate` (26 tables). Applied on startup when `Seed:ApplyMigrations` is true.
- Seeder: roles (Admin, Manager, Customer), admin user from config, 27 Egyptian governorates with shipping fees, sample honey catalog in Development (9 products, 19 weight variants).
- Integration test harness: `HoneyHouseApiFactory` boots the real API on a throw-away SQL Server database per run (`HH_TEST_SQL` overrides the server).
- Build: 0 errors, 0 warnings. Tests: 19 unit + 7 integration passed (incl. negative-stock constraint and concurrent stock update detection).

### 2026-11-22 — Task 2.3
- Localization: request culture from `?lang=` or `Accept-Language` (`ar` default, `en`). Bilingual message catalogue in `Application/Localization/Messages.cs`; FluentValidation built-in messages follow the request culture (Arabic included).
- `Result` / `Result<T>` / `Error` pattern and `ApiControllerBase` mapping errors to localized `ProblemDetails` (400/401/403/404/409/422) with a stable `code` field.
- `ValidationFilter` runs FluentValidation for every action argument; `GlobalExceptionHandler` returns localized 500/409 responses; `UseStatusCodePages` gives problem+json for 404/401/405.
- Swagger (with JWT bearer scheme and XML docs), Serilog request logging, CORS (configurable origins), rate limiter policy `auth`, `/health` with a database check.
- `GET /api/v1/meta`: store name in the requested language, currency EGP, supported languages.
- Shared validation rules: Egyptian mobile numbers, password strength, supported language.
- Build: 0 errors, 0 warnings. Tests: 31 unit + 14 integration passed.

### 2026-11-22 — Task 2.4
- ASP.NET Core Identity (users + roles) with JWT bearer access tokens (15 min, HMAC-SHA256) and refresh tokens (7 days, stored as SHA-256 hashes, rotated on every use, reuse detection revokes all sessions).
- Endpoints: `POST /auth/register`, `/login`, `/refresh`, `/logout`, `/forgot-password`, `/reset-password`, `/change-password`; `GET|PUT /auth/me`.
- Lockout after 5 failed logins (15 min), disabled accounts blocked, password change/reset signs out other sessions, forgot-password never reveals whether an email exists.
- Rate limiting on auth endpoints (10/min per IP, configurable). Localized 401/403 problem responses.
- `IEmailSender` abstraction (development implementation logs emails).
- Build: 0 errors, 0 warnings. Tests: 34 unit + 27 integration passed.

### 2026-11-22 — Task 2.5
- Repositories: `ICategoryRepository`, `IProductRepository` (search with projection, details with includes, slug/SKU uniqueness incl. soft-deleted rows).
- Public: `GET /categories` (2-level tree), `GET /categories/{id}`, `GET /products` (search in Arabic/English names, descriptions and SKU; filters: category incl. sub-categories, min/max price, in stock, featured; sort: newest, price_asc, price_desc, rating, bestselling, name; paging), `GET /products/featured`, `GET /products/{id}`, `GET /products/slug/{slug}`. Responses are localized and priced in EGP; exact stock is hidden (capped at 20).
- Admin (Admin/Manager): category CRUD with two-level rule, slug uniqueness and non-empty delete protection; product CRUD (soft delete) with weight options; add/update/deactivate variants (last active variant protected, stock only via inventory adjustments, initial stock recorded as an inventory transaction); image upload (JPG/PNG/WEBP ≤ 5 MB), set main image, delete image.
- `IFileStorage` with `LocalFileStorage` served from `/uploads`.
- Build: 0 errors, 0 warnings. Tests: 47 unit + 38 integration passed.

### 2026-11-22 — Task 2.6
- `GET /governorates` (public, localized, with shipping fee); `GET|PUT /admin/governorates/{id}` (Admin) to change fees or disable delivery to a governorate.
- Addresses (`/addresses`): list, get, create, update, delete, set default. Max 10 per user, first address becomes default, deleting the default promotes another, Egyptian phone validation, users are isolated from each other.
- Cart (`/cart`): get, add item (merges quantities), update quantity, remove item, clear. Enforces stock and max 20 units per item; deactivated/deleted products stay visible but flagged `isAvailable=false` and are excluded from the subtotal.
- Build: 0 errors, 0 warnings. Tests: 49 unit + 47 integration passed.

### 2026-11-22 — Task 2.7
- `CouponRules.Evaluate`: active flag, start/end window, global usage limit, per-user limit, minimum order; discount = percentage (rounded, capped by max discount) or fixed amount, never above the subtotal. Codes are case-insensitive (stored upper case).
- `POST /coupons/validate`: previews the discount on the customer's current cart.
- Admin (`/admin/coupons`, Admin only): search/paging, get, create, update, delete (blocked once used; deactivate instead).
- Enums are now serialized as strings in API JSON (e.g. `"discountType": "Percentage"`).
- Build: 0 errors, 0 warnings. Tests: 57 unit + 57 integration passed.

### 2026-11-22 — Task 2.8
- Checkout: `POST /orders/checkout/preview` (subtotal, coupon discount or coupon error, governorate shipping fee, total, blocking reasons) and `POST /orders` which, in one database transaction: re-validates cart, address and coupon; snapshots product names, prices and the address into the order; reserves stock (optimistic concurrency on `ProductVariants.RowVersion` + `CHECK (StockQuantity >= 0)` prevent overselling); records `Sale` inventory transactions; records coupon usage; clears the cart; starts payment through `IOrderPaymentHandler` (Cash on Delivery now, online in 2.9).
- Customer: `GET /orders`, `GET /orders/{id}`, `GET /orders/{id}/tracking` (localized status timeline), `POST /orders/{id}/cancel` (only before shipping; returns stock and frees the coupon).
- Admin/Manager: `GET /admin/orders` (filters: status, payment status/method, date range, search by order number/phone/name/email), `GET /admin/orders/{id}` (customer, payments, allowed next statuses), `PUT /admin/orders/{id}/status` (state machine; Cancelled/Returned restock; Delivered marks COD as paid), `POST /admin/orders/{id}/mark-paid`.
- Inventory: `GET /admin/inventory` (search, low-stock filter), `POST /admin/inventory/{variantId}/adjust` (Restock/Adjustment, never below zero), `GET /admin/inventory/{variantId}/transactions` (full audit trail).
- Order numbers look like `HH-261122-7KQ2MX`.
- Build: 0 errors, 0 warnings. Tests: 57 unit + 69 integration passed (incl. a race where two customers buy the last jar: exactly one succeeds, stock ends at 0).

### 2026-11-22 — Task 2.9
- `IPaymentGateway` abstraction with `PaymobGateway` (Intention API + Unified Checkout). Placing an `Online` order creates a Paymob intention (amount in piasters, EGP, billing data, `special_reference` = order number) and returns `paymentUrl`. If Paymob fails, the whole order is rolled back (stock and cart untouched).
- `POST /payments/paymob/webhook?hmac=...`: verifies the HMAC-SHA512 signature (Paymob field order, constant-time compare), is idempotent, never downgrades a paid order, rejects under-payments, auto-confirms paid orders, and flags payments that arrive after cancellation for refund.
- `POST /payments/{orderId}/retry`: new payment attempt for an unpaid online order.
- Background job `UnpaidOrderExpiryService` (every 60 s): cancels online orders unpaid after 30 minutes and returns their stock.
- Paymob is configured under `Paymob` in settings (disabled until keys are provided; then online checkout is offered).
- Build: 0 errors, 0 warnings. Tests: 65 unit + 76 integration passed (HMAC verified against an independently computed vector).

### 2026-11-22 — Task 2.10
- Reviews: `GET /products/{id}/reviews` (approved reviews + summary: average, count, 1–5 distribution; reviewer shown by first name only), `GET /products/{id}/reviews/mine`, `POST /products/{id}/reviews` (one per customer, 1–5 stars, "verified purchase" when the customer has a delivered order with the product), `PUT|DELETE /reviews/{id}` (owner only; edits go back to moderation).
- Moderation (Admin/Manager): `GET /admin/reviews?status=pending|approved|all`, `PUT /admin/reviews/{id}/approve`, `DELETE /admin/reviews/{id}`. Product `AverageRating`/`ReviewCount` are recalculated from approved reviews on every change.
- Favorites: `GET /favorites` (product cards, newest first, hides inactive/deleted products), `GET /favorites/ids`, `POST|DELETE /favorites/{productId}` (idempotent).
- Build: 0 errors, 0 warnings. Tests: 68 unit + 81 integration passed.

### 2026-11-22 — Task 2.11
- Users (Admin): `GET /admin/users` (search, role filter), `GET /admin/users/{id}`, `POST /admin/users` (create staff, e.g. Manager), `PUT /admin/users/{id}/roles`, `PUT /admin/users/{id}/status` (disabling revokes all sessions). An admin cannot disable themselves or drop their own Admin role.
- Analytics (Admin): `GET /admin/analytics/summary` (revenue, paid revenue, orders, average order value, items sold, cancellations, discounts given, new customers, pending orders, low-stock count), `/sales?groupBy=day|month` (continuous series in Cairo local time), `/top-products`, `/orders-by-status` (localized), `/sales-by-governorate`. Default range: last 30 days, max 2 years. Cancelled and returned orders are excluded from revenue.
- Build: 0 errors, 0 warnings. Tests: 68 unit + 87 integration passed.

## Phase 2 summary
- 89 API operations on 68 routes, documented in Swagger (`/swagger`).
- 155 automated tests (68 unit, 87 integration against a real SQL Server).
- End-to-end smoke test on a fresh database: migrate + seed, register, Arabic search, cart, address, COD order (`HH-261122-…`, total in EGP) all succeeded with no server errors in the log.

### Deviations from the architecture document
- Localized messages live in a C# dictionary (`Application/Localization/Messages.cs`) instead of `.resx` files: simpler to review and unit-test. FluentValidation's built-in Arabic messages are used for generic rules.
- Tests use **Shouldly** instead of FluentAssertions (FluentAssertions 8 needs a commercial licence) and DTO mapping is hand-written (no AutoMapper/Mapster dependency).
- Added endpoints not in the original list: `GET /meta`, `GET /favorites/ids`, `GET /products/{id}/reviews/mine`, `POST /admin/users`, `GET /admin/analytics/sales-by-governorate`, `GET /admin/products` and `/admin/categories` listings.

### Before production (Phase 5)
- Provide secrets via environment/secret store: `ConnectionStrings__DefaultConnection`, `Jwt__SigningKey`, `Paymob__*` (then set `Paymob__Enabled=true`), and remove the development admin seed.
- Replace `LoggingEmailSender` with a real email provider and `LocalFileStorage` with blob storage if running more than one server.
- Run migrations as a deployment step (`Seed:ApplyMigrations=false` on the web servers).

## Next: Phase 3 — Admin Dashboard (ASP.NET Core MVC)
