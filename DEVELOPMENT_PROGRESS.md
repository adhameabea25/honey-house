# Honey House — Development Progress

| Phase | Status |
|---|---|
| 1. Architecture | ✅ Approved ([docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)) |
| 2. Backend API | 🚧 In progress |
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
| 2.5 | Catalog: categories, products, variants, images | ⏳ | | |
| 2.6 | Addresses, governorates, cart | ⏳ | | |
| 2.7 | Coupons | ⏳ | | |
| 2.8 | Orders, checkout, inventory | ⏳ | | |
| 2.9 | Payments: COD + Paymob | ⏳ | | |
| 2.10 | Reviews and favorites | ⏳ | | |
| 2.11 | Admin APIs and analytics | ⏳ | | |

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
