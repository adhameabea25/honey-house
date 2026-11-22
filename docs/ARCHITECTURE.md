# Honey House — وثيقة المعمارية (Backend)

> الإصدار 1.0 · 2026-11-22 · الحالة: **بانتظار الموافقة** — لن يبدأ أي كود قبل موافقتك.

---

## 1. تحليل المتطلبات

### 1.1 مكونات النظام

| المكون | التقنية | المستخدم |
|---|---|---|
| تطبيق الموبايل | Flutter (Android / iOS) | العملاء |
| الـ Backend API | ASP.NET Core Web API (C#) | تطبيق الموبايل + أي عميل خارجي |
| لوحة التحكم | ASP.NET Core MVC | الإدارة وموظفو المتجر |
| قاعدة البيانات | SQL Server + EF Core | — |

### 1.2 المتطلبات الوظيفية

| # | الميزة | ملاحظات التصميم |
|---|---|---|
| F1 | التسجيل وتسجيل الدخول | ASP.NET Core Identity + JWT Access Token + Refresh Token |
| F2 | إدارة المنتجات والتصنيفات | تصنيفات بمستويين (أب/فرعي)، صور متعددة لكل منتج |
| F3 | أوزان وأسعار مختلفة | جدول `ProductVariants`: كل وزن (250 جم، 500 جم، 1 كجم…) له سعر وSKU ومخزون مستقل |
| F4 | السلة والدفع (Checkout) | سلة محفوظة في قاعدة البيانات لكل مستخدم، تتزامن بين الأجهزة |
| F5 | الدفع عند الاستلام + الدفع الإلكتروني | واجهة `IPaymentGateway`؛ التنفيذ الأول: **Paymob** (الأشهر في مصر: بطاقات، محافظ، فوري) |
| F6 | إدارة الطلبات وتتبعها | حالات الطلب + سجل تاريخي لكل تغيير حالة |
| F7 | إدارة المخزون | المخزون على مستوى الـ Variant، سجل حركات المخزون، تنبيه انخفاض المخزون |
| F8 | الكوبونات والخصومات | نسبة مئوية أو مبلغ ثابت، حد أدنى للطلب، حد أقصى للخصم، عدد مرات الاستخدام |
| F9 | التقييمات والمفضلة | تقييم واحد لكل مستخدم لكل منتج، مراجعة الإدارة قبل النشر |
| F10 | لوحة التحكم والتحليلات | مبيعات حسب الفترة، أفضل المنتجات، الطلبات حسب الحالة، المخزون المنخفض |

### 1.3 المتطلبات غير الوظيفية

- **اللغتان العربية (RTL) والإنجليزية:** الحقول المعروضة للعميل مخزنة بلغتين (`NameAr` / `NameEn`). الـ API يقرأ ترويسة `Accept-Language` (`ar` أو `en`) ويُرجع النص باللغة المطلوبة، ورسائل الأخطاء مترجمة عبر ملفات Resource. اتجاه RTL مسؤولية الواجهات (Flutter و MVC).
- **العملة:** الجنيه المصري EGP فقط، بنوع `decimal(18,2)`. لا يوجد تحويل عملات.
- **الأمان:** كلمات مرور مُشفّرة (Identity)، JWT قصير العمر (15 دقيقة) + Refresh Token (7 أيام) مع التدوير، صلاحيات بالأدوار، Rate Limiting على مسارات الدخول، التحقق من توقيع HMAC في Webhook الدفع.
- **سلامة البيانات:** خصم المخزون وإنشاء الطلب داخل Transaction واحدة مع Concurrency Token لمنع البيع بأكثر من المخزون.
- **الحذف المنطقي (Soft Delete)** للمنتجات والتصنيفات حتى لا تنكسر الطلبات القديمة.
- **لقطة بيانات (Snapshot)** في الطلب: اسم المنتج والسعر والعنوان تُنسخ وقت الطلب، فتعديل المنتج لاحقًا لا يغيّر الطلبات السابقة.
- **التوثيق:** Swagger / OpenAPI مع دعم JWT.
- **السجلات:** Serilog.

### 1.4 الأدوار

| الدور | الصلاحيات |
|---|---|
| `Customer` | التصفح، السلة، الطلبات، التقييمات، المفضلة، العناوين |
| `Manager` | إدارة المنتجات والمخزون والطلبات والتقييمات |
| `Admin` | كل ما سبق + الكوبونات + المستخدمين والأدوار + التحليلات + الإعدادات |

---

## 2. المعمارية (Clean Architecture)

```
          ┌──────────────────────┐   ┌──────────────────────┐
          │  HoneyHouse.API      │   │  HoneyHouse.Admin    │
          │  (Web API + JWT)     │   │  (MVC + Cookies)     │
          └──────────┬───────────┘   └──────────┬───────────┘
                     │                          │
                     ▼                          ▼
          ┌─────────────────────────────────────────────────┐
          │        HoneyHouse.Application                    │
          │  Services, DTOs, Validators, Interfaces          │
          └──────────────────────┬──────────────────────────┘
                                 │
                                 ▼
          ┌─────────────────────────────────────────────────┐
          │        HoneyHouse.Domain                         │
          │  Entities, Enums, Domain rules (بدون أي اعتماديات)│
          └─────────────────────────────────────────────────┘
                                 ▲
          ┌──────────────────────┴──────────────────────────┐
          │        HoneyHouse.Infrastructure                 │
          │  EF Core DbContext, Repositories, Unit of Work,  │
          │  Identity, JWT, Paymob, File Storage             │
          └─────────────────────────────────────────────────┘
```

**قاعدة الاعتماديات:** الطبقات الداخلية لا تعرف شيئًا عن الخارجية. الـ Domain لا يعتمد على أي مكتبة. الـ Application يعرّف الواجهات (`IProductRepository`, `IPaymentGateway`...) والـ Infrastructure ينفّذها.

**الأنماط المستخدمة:**
- **Repository Pattern + Unit of Work:** `IGenericRepository<T>` للعمليات العامة، ومستودعات خاصة عند الحاجة (`IProductRepository`, `IOrderRepository`)، و`IUnitOfWork` لحفظ التغييرات داخل Transaction.
- **Service Layer** في الـ Application (مثل `ProductService`, `OrderService`) تحتوي منطق العمل.
- **DTOs + Mapping** (Mapster أو AutoMapper) فلا تخرج الـ Entities من الـ API أبدًا.
- **FluentValidation** للتحقق من المدخلات.
- **Result Pattern** لإرجاع النجاح/الفشل بدون Exceptions لمنطق العمل.
- **Global Exception Middleware** يُرجع أخطاء موحدة بصيغة `ProblemDetails`.

**لوحة التحكم (MVC):** أقترح أن تستخدم طبقتي Application و Infrastructure **مباشرة** (نفس الخدمات ونفس قاعدة البيانات) مع تسجيل دخول بالـ Cookies، بدل أن تستدعي الـ API عبر HTTP. هذا أسرع وأبسط ويمنع تكرار الكود. سنبنيها في المرحلة 3.

**الإصدار:** ‎.NET 10 (LTS) و EF Core 10.

---

## 3. هيكل المجلدات

```
honey-house/
├── HoneyHouse.sln
├── DEVELOPMENT_PROGRESS.md
├── docker-compose.yml              # SQL Server للتطوير
├── docs/
│   └── ARCHITECTURE.md
├── src/
│   ├── HoneyHouse.Domain/
│   │   ├── Common/                 # BaseEntity, AuditableEntity, ISoftDelete
│   │   ├── Entities/               # Product, ProductVariant, Order, ...
│   │   ├── Enums/                  # OrderStatus, PaymentMethod, ...
│   │   └── Exceptions/
│   ├── HoneyHouse.Application/
│   │   ├── Common/                 # Result, PagedResult, Errors
│   │   ├── Interfaces/
│   │   │   ├── Repositories/       # IGenericRepository, IUnitOfWork, ...
│   │   │   └── Services/           # IPaymentGateway, ITokenService, IFileStorage
│   │   ├── Features/
│   │   │   ├── Auth/               # DTOs, Validators, AuthService
│   │   │   ├── Catalog/            # Categories + Products + Variants
│   │   │   ├── Cart/
│   │   │   ├── Orders/
│   │   │   ├── Payments/
│   │   │   ├── Coupons/
│   │   │   ├── Inventory/
│   │   │   ├── Reviews/
│   │   │   ├── Favorites/
│   │   │   ├── Addresses/
│   │   │   └── Analytics/
│   │   ├── Mapping/
│   │   └── DependencyInjection.cs
│   ├── HoneyHouse.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/     # IEntityTypeConfiguration لكل Entity
│   │   │   ├── Migrations/
│   │   │   ├── Repositories/
│   │   │   ├── UnitOfWork.cs
│   │   │   └── Seed/               # الأدوار، الأدمن، المحافظات، بيانات تجريبية
│   │   ├── Identity/               # ApplicationUser, JwtTokenService
│   │   ├── Payments/               # PaymobGateway, CashOnDeliveryGateway
│   │   ├── Storage/                # LocalFileStorage
│   │   └── DependencyInjection.cs
│   ├── HoneyHouse.API/
│   │   ├── Controllers/
│   │   │   └── Admin/
│   │   ├── Middleware/             # ExceptionHandling, Localization
│   │   ├── Resources/              # SharedResource.ar.resx / .en.resx
│   │   ├── Extensions/
│   │   ├── appsettings.json
│   │   └── Program.cs
│   └── HoneyHouse.Admin/           # المرحلة 3
└── tests/
    ├── HoneyHouse.UnitTests/         # xUnit + FluentAssertions + NSubstitute
    └── HoneyHouse.IntegrationTests/  # WebApplicationFactory + SQL Server حقيقي في Docker
```

---

## 4. مخطط قاعدة البيانات

كل جدول يرث من `BaseEntity`: `Id (int, PK)`, `CreatedAt`, `UpdatedAt`. الجداول التي تدعم الحذف المنطقي فيها `IsDeleted` مع Global Query Filter.

### 4.1 المستخدمون والعناوين

**AspNetUsers** (ApplicationUser يرث IdentityUser)
| العمود | النوع | ملاحظات |
|---|---|---|
| Id | nvarchar(450) | PK (Identity) |
| FullName | nvarchar(150) | |
| Email | nvarchar(256) | فريد |
| PhoneNumber | nvarchar(20) | رقم مصري |
| PreferredLanguage | nvarchar(2) | `ar` أو `en`، الافتراضي `ar` |
| IsActive | bit | |
| CreatedAt | datetime2 | |

**AspNetRoles / AspNetUserRoles** — جداول Identity القياسية (Admin, Manager, Customer).

**RefreshTokens**
| Id | UserId (FK) | TokenHash | ExpiresAt | CreatedAt | RevokedAt | ReplacedByTokenHash |

**Governorates** (المحافظات المصرية الـ 27 مع رسوم الشحن)
| Id | NameAr | NameEn | ShippingFee decimal(18,2) | IsActive |

**Addresses**
| Id | UserId (FK) | GovernorateId (FK) | City | Area | Street | Building | Floor | Apartment | Landmark | RecipientName | Phone | IsDefault |

### 4.2 الكتالوج

**Categories**
| Id | ParentCategoryId (FK, nullable) | NameAr | NameEn | DescriptionAr | DescriptionEn | Slug (فريد) | ImageUrl | SortOrder | IsActive | IsDeleted |

**Products**
| Id | CategoryId (FK) | NameAr | NameEn | DescriptionAr | DescriptionEn | Slug (فريد) | Origin (مثل: سيناء، الواحات) | IsActive | IsFeatured | AverageRating decimal(3,2) | ReviewCount | IsDeleted |

**ProductImages**
| Id | ProductId (FK) | Url | AltText | SortOrder | IsMain |

**ProductVariants** — الأوزان والأسعار والمخزون
| العمود | النوع | ملاحظات |
|---|---|---|
| Id | int | PK |
| ProductId | int | FK |
| Sku | nvarchar(50) | فريد، مثل `HH-SIDR-500` |
| WeightGrams | int | 250، 500، 1000 |
| LabelAr / LabelEn | nvarchar(50) | "500 جرام" / "500 g" |
| Price | decimal(18,2) | EGP |
| CompareAtPrice | decimal(18,2) null | السعر قبل الخصم (للعرض) |
| StockQuantity | int | |
| LowStockThreshold | int | الافتراضي 5 |
| IsActive | bit | |
| RowVersion | rowversion | Concurrency Token لمنع البيع الزائد |

**InventoryTransactions** — سجل كل حركة مخزون
| Id | VariantId (FK) | QuantityChange (+/-) | Type (Restock, Sale, Return, Adjustment, Cancellation) | OrderId (FK null) | Note | CreatedByUserId |

### 4.3 السلة

**Carts** — `Id | UserId (FK, فريد)`
**CartItems** — `Id | CartId (FK) | VariantId (FK) | Quantity` · فريد على (CartId, VariantId)

### 4.4 الكوبونات

**Coupons**
| Id | Code (فريد) | DescriptionAr | DescriptionEn | DiscountType (Percentage, FixedAmount) | Value | MinOrderAmount | MaxDiscountAmount | StartsAt | ExpiresAt | UsageLimit | UsageLimitPerUser | UsedCount | IsActive |

**CouponUsages** — `Id | CouponId | UserId | OrderId | DiscountAmount | UsedAt`

### 4.5 الطلبات والدفع

**Orders**
| العمود | ملاحظات |
|---|---|
| Id, OrderNumber | رقم مقروء فريد، مثل `HH-20261122-0001` |
| UserId | FK |
| Status | Pending, Confirmed, Processing, Shipped, OutForDelivery, Delivered, Cancelled, Returned |
| PaymentMethod | CashOnDelivery, Online |
| PaymentStatus | Pending, Paid, Failed, Refunded |
| Subtotal, DiscountAmount, ShippingFee, Total | decimal(18,2) |
| CouponId | FK null |
| لقطة العنوان | ShipRecipientName, ShipPhone, ShipGovernorate, ShipCity, ShipAddressLine, ShipLandmark |
| CustomerNotes, TrackingNumber | |
| CancelledAt, DeliveredAt | |

**OrderItems**
| Id | OrderId | VariantId | ProductId | ProductNameAr | ProductNameEn | VariantLabelAr | VariantLabelEn | Sku | UnitPrice | Quantity | LineTotal |

**OrderStatusHistory** — `Id | OrderId | FromStatus | ToStatus | Note | ChangedByUserId | ChangedAt`

**Payments**
| Id | OrderId | Provider (Paymob, COD) | ProviderOrderId | ProviderTransactionId | Amount | Status | FailureReason | RawPayload (nvarchar(max)) |

### 4.6 التقييمات والمفضلة

**Reviews** — `Id | ProductId | UserId | Rating (1-5) | Comment | IsApproved | IsVerifiedPurchase` · فريد على (ProductId, UserId)
**Favorites** — `UserId | ProductId | CreatedAt` · مفتاح مركب (UserId, ProductId)

### 4.7 العلاقات (ملخص)

```
User 1─* Address *─1 Governorate
User 1─1 Cart 1─* CartItem *─1 ProductVariant
Category 1─* Category (فرعي)
Category 1─* Product 1─* ProductVariant
Product 1─* ProductImage
Product 1─* Review *─1 User
User *─* Product (Favorites)
User 1─* Order 1─* OrderItem *─1 ProductVariant
Order 1─* OrderStatusHistory
Order 1─* Payment
Coupon 1─* CouponUsage *─1 Order
ProductVariant 1─* InventoryTransaction
```

---

## 5. الـ API Endpoints

**البادئة:** `/api/v1` · **الاستجابة:** JSON · **الترقيم:** `?page=1&pageSize=20` · **اللغة:** ترويسة `Accept-Language: ar|en`

الرموز: 🌐 عام · 👤 عميل مسجّل · 🛠 Manager أو Admin · 👑 Admin فقط

### 5.1 المصادقة `/auth`
| Method | Path | الصلاحية | الوصف |
|---|---|---|---|
| POST | `/auth/register` | 🌐 | إنشاء حساب عميل |
| POST | `/auth/login` | 🌐 | دخول، يُرجع Access + Refresh Token |
| POST | `/auth/refresh` | 🌐 | تجديد التوكن |
| POST | `/auth/logout` | 👤 | إلغاء الـ Refresh Token |
| POST | `/auth/forgot-password` | 🌐 | إرسال رمز إعادة التعيين |
| POST | `/auth/reset-password` | 🌐 | تعيين كلمة مرور جديدة |
| GET | `/auth/me` | 👤 | بيانات المستخدم الحالي |
| PUT | `/auth/me` | 👤 | تعديل الاسم والهاتف واللغة |
| POST | `/auth/change-password` | 👤 | تغيير كلمة المرور |

### 5.2 التصنيفات `/categories`
| GET | `/categories` | 🌐 | شجرة التصنيفات النشطة |
|---|---|---|---|
| GET | `/categories/{id}` | 🌐 | تفاصيل تصنيف |
| POST | `/admin/categories` | 🛠 | إضافة |
| PUT | `/admin/categories/{id}` | 🛠 | تعديل |
| DELETE | `/admin/categories/{id}` | 🛠 | حذف منطقي |

### 5.3 المنتجات `/products`
| GET | `/products` | 🌐 | بحث وفلترة: `search, categoryId, minPrice, maxPrice, inStock, sort=newest\|price_asc\|price_desc\|rating\|bestselling` |
|---|---|---|---|
| GET | `/products/featured` | 🌐 | المنتجات المميزة |
| GET | `/products/{id}` | 🌐 | التفاصيل مع الأوزان والصور |
| GET | `/products/slug/{slug}` | 🌐 | التفاصيل بالـ Slug |
| POST | `/admin/products` | 🛠 | إضافة منتج مع أوزانه |
| PUT | `/admin/products/{id}` | 🛠 | تعديل |
| DELETE | `/admin/products/{id}` | 🛠 | حذف منطقي |
| POST | `/admin/products/{id}/images` | 🛠 | رفع صورة (multipart) |
| DELETE | `/admin/products/{id}/images/{imageId}` | 🛠 | حذف صورة |
| POST | `/admin/products/{id}/variants` | 🛠 | إضافة وزن/سعر |
| PUT | `/admin/variants/{variantId}` | 🛠 | تعديل وزن/سعر |
| DELETE | `/admin/variants/{variantId}` | 🛠 | تعطيل وزن |

### 5.4 المخزون `/admin/inventory`
| GET | `/admin/inventory` | 🛠 | كل الأوزان ومخزونها، `?lowStockOnly=true` |
|---|---|---|---|
| POST | `/admin/inventory/{variantId}/adjust` | 🛠 | إضافة/خصم كمية مع السبب |
| GET | `/admin/inventory/{variantId}/transactions` | 🛠 | سجل الحركات |

### 5.5 السلة `/cart`
| GET | `/cart` | 👤 | السلة مع الإجماليات |
|---|---|---|---|
| POST | `/cart/items` | 👤 | إضافة `{variantId, quantity}` |
| PUT | `/cart/items/{itemId}` | 👤 | تعديل الكمية |
| DELETE | `/cart/items/{itemId}` | 👤 | حذف عنصر |
| DELETE | `/cart` | 👤 | تفريغ السلة |

### 5.6 العناوين والمحافظات
| GET | `/governorates` | 🌐 | المحافظات ورسوم الشحن |
|---|---|---|---|
| GET | `/addresses` | 👤 | عناويني |
| POST | `/addresses` | 👤 | إضافة |
| PUT | `/addresses/{id}` | 👤 | تعديل |
| DELETE | `/addresses/{id}` | 👤 | حذف |
| PUT | `/addresses/{id}/default` | 👤 | تعيين كافتراضي |
| PUT | `/admin/governorates/{id}` | 👑 | تعديل رسوم الشحن |

### 5.7 الكوبونات
| POST | `/coupons/validate` | 👤 | التحقق من كوبون على السلة الحالية وإرجاع قيمة الخصم |
|---|---|---|---|
| GET | `/admin/coupons` | 👑 | القائمة |
| POST | `/admin/coupons` | 👑 | إضافة |
| PUT | `/admin/coupons/{id}` | 👑 | تعديل |
| DELETE | `/admin/coupons/{id}` | 👑 | حذف |

### 5.8 الطلبات والدفع
| POST | `/orders/checkout/preview` | 👤 | حساب الإجمالي قبل التأكيد `{addressId, couponCode}` |
|---|---|---|---|
| POST | `/orders` | 👤 | إنشاء طلب من السلة `{addressId, paymentMethod, couponCode, notes}`؛ للدفع الإلكتروني يُرجع `paymentUrl` |
| GET | `/orders` | 👤 | طلباتي |
| GET | `/orders/{id}` | 👤 | تفاصيل طلب |
| GET | `/orders/{id}/tracking` | 👤 | سجل حالات الطلب |
| POST | `/orders/{id}/cancel` | 👤 | إلغاء (فقط قبل الشحن) |
| POST | `/payments/{orderId}/retry` | 👤 | إعادة محاولة الدفع الإلكتروني |
| POST | `/payments/paymob/webhook` | 🌐 (HMAC) | إشعار Paymob بنتيجة الدفع |
| GET | `/admin/orders` | 🛠 | كل الطلبات، فلترة بالحالة والتاريخ والبحث |
| GET | `/admin/orders/{id}` | 🛠 | التفاصيل |
| PUT | `/admin/orders/{id}/status` | 🛠 | تغيير الحالة `{status, note, trackingNumber}` |
| POST | `/admin/orders/{id}/mark-paid` | 🛠 | تأكيد تحصيل الدفع عند الاستلام |

**دورة حياة الطلب:**
```
Pending ─► Confirmed ─► Processing ─► Shipped ─► OutForDelivery ─► Delivered
   │            │             │                                        │
   └────────────┴─────────────┴─► Cancelled (يُعاد المخزون)            └─► Returned
```
- **الدفع عند الاستلام:** الطلب يبدأ `Pending` ويُخصم المخزون فورًا.
- **الدفع الإلكتروني:** يُخصم المخزون ويُحجز، وإن لم يُدفع خلال 30 دقيقة يُلغى الطلب تلقائيًا ويُعاد المخزون (Background Service).

### 5.9 التقييمات والمفضلة
| GET | `/products/{id}/reviews` | 🌐 | التقييمات المعتمدة |
|---|---|---|---|
| POST | `/products/{id}/reviews` | 👤 | إضافة تقييم |
| PUT | `/reviews/{id}` | 👤 | تعديل تقييمي |
| DELETE | `/reviews/{id}` | 👤 | حذف تقييمي |
| GET | `/admin/reviews` | 🛠 | تقييمات بانتظار المراجعة |
| PUT | `/admin/reviews/{id}/approve` | 🛠 | اعتماد/رفض |
| GET | `/favorites` | 👤 | مفضلتي |
| POST | `/favorites/{productId}` | 👤 | إضافة |
| DELETE | `/favorites/{productId}` | 👤 | حذف |

### 5.10 المستخدمون والتحليلات (إدارة)
| GET | `/admin/users` | 👑 | القائمة مع البحث |
|---|---|---|---|
| PUT | `/admin/users/{id}/roles` | 👑 | تغيير الأدوار |
| PUT | `/admin/users/{id}/status` | 👑 | تفعيل/إيقاف |
| GET | `/admin/analytics/summary?from&to` | 👑 | إجمالي المبيعات، عدد الطلبات، متوسط قيمة الطلب، العملاء الجدد |
| GET | `/admin/analytics/sales?from&to&groupBy=day\|month` | 👑 | المبيعات عبر الزمن |
| GET | `/admin/analytics/top-products?from&to&limit` | 👑 | الأكثر مبيعًا |
| GET | `/admin/analytics/orders-by-status` | 👑 | توزيع الطلبات |

### 5.11 صيغة الأخطاء الموحدة

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "خطأ في التحقق من البيانات",
  "status": 400,
  "errors": { "email": ["البريد الإلكتروني مستخدم بالفعل"] },
  "traceId": "00-abc..."
}
```

---

## 6. الاختبارات

| النوع | الأدوات | يغطي |
|---|---|---|
| Unit Tests | xUnit, FluentAssertions, NSubstitute | منطق الخدمات: حساب الخصم، رسوم الشحن، انتقالات حالة الطلب، التحقق |
| Integration Tests | WebApplicationFactory + SQL Server في Docker | الـ Endpoints كاملة مع قاعدة بيانات حقيقية، المصادقة، الصلاحيات، منع البيع الزائد |

---

## 7. خطة المرحلة 2 (تطوير الـ Backend)

بعد كل مهمة: **Build ← إصلاح الأخطاء ← الاختبارات ← تحديث DEVELOPMENT_PROGRESS.md ← ملخص لك.**

| # | المهمة |
|---|---|
| 2.1 | إنشاء الـ Solution والمشاريع الأربعة + مشاريع الاختبار + Domain Entities |
| 2.2 | Infrastructure: DbContext، Configurations، أول Migration، Seed (الأدوار، حساب أدمن، المحافظات، بيانات عسل تجريبية) |
| 2.3 | Cross-cutting: معالجة الأخطاء، FluentValidation، الترجمة ar/en، Swagger، Serilog |
| 2.4 | المصادقة: Identity + JWT + Refresh Token + الأدوار |
| 2.5 | الكتالوج: التصنيفات، المنتجات، الأوزان، الصور، البحث والفلترة |
| 2.6 | العناوين والمحافظات والسلة |
| 2.7 | الكوبونات |
| 2.8 | الطلبات والـ Checkout والمخزون (Transaction + Concurrency) |
| 2.9 | الدفع: COD + Paymob + Webhook + الإلغاء التلقائي |
| 2.10 | التقييمات والمفضلة |
| 2.11 | APIs الإدارة والتحليلات |

**المراحل التالية:** المرحلة 3 لوحة التحكم MVC · المرحلة 4 تطبيق Flutter · المرحلة 5 النشر.

---

## 8. قرارات تحتاج تأكيدك (اخترت افتراضيًا ما يلي)

1. **بوابة الدفع:** Paymob. (البديل: Fawry أو Kashier.)
2. **رسوم الشحن:** ثابتة لكل محافظة، تعدّلها الإدارة.
3. **الشراء بدون حساب (Guest Checkout):** غير مدعوم، العميل يسجّل أولًا.
4. **التقييمات:** أي عميل مسجّل يقيّم، وتظهر علامة "مشتري موثّق" لمن اشترى المنتج، وتُعتمد من الإدارة قبل الظهور.
5. **تخزين الصور:** على السيرفر محليًا في البداية خلف واجهة `IFileStorage`، ويمكن نقلها لاحقًا إلى Azure Blob أو S3 بدون تعديل باقي الكود.
