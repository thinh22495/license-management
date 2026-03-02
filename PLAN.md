# License Management System - Implementation Plan

## Context
Xây dựng hệ thống quản lý giấy phép phần mềm hoàn chỉnh, hỗ trợ cả offline và online, chống giả mạo bằng chữ ký số Ed25519. Hệ thống gồm frontend (Next.js + Ant Design), backend (ASP.NET Core 10), PostgreSQL, triển khai bằng Docker.

---

## 1. Tech Stack

### Backend - ASP.NET Core 10 (C#)
- **ASP.NET Core 10** (Web API, Minimal APIs hoặc Controllers)
- **Entity Framework Core 10** (ORM + Code-First Migrations)
- **Npgsql.EntityFrameworkCore.PostgreSQL** (EF Core provider cho PostgreSQL)
- **ASP.NET Core Identity** (user management, password hashing với PBKDF2/Argon2)
- **Microsoft.AspNetCore.Authentication.JwtBearer** (JWT authentication)
- **FluentValidation** (request validation)
- **MediatR** (CQRS pattern, tách command/query)
- **Hangfire + Redis** (background jobs: email, payment callback, license expiry check)
- **MailKit** (gửi email, thay thế SmtpClient)
- **WebPush-NetCore** (push notification)
- **System.Security.Cryptography** (Ed25519 signing - native .NET)
- **Swashbuckle / NSwag** (Swagger/OpenAPI docs)
- **AspNetCoreRateLimit** (rate limiting)
- **Serilog** (structured logging)
- **AutoMapper** (DTO mapping)

### Frontend - Next.js + Ant Design
- **Next.js 14** (App Router, SSR)
- **Ant Design 5** (UI components, hỗ trợ locale tiếng Việt)
- **Zustand** (state management)
- **TanStack Query v5** + Axios (HTTP client)
- **React Hook Form + Zod** (forms + validation)

### Infrastructure
- **PostgreSQL 16** (database)
- **Redis 8** (cache + Hangfire queue)
- **Nginx** (reverse proxy + TLS)
- **MinIO** (object storage cho ảnh sản phẩm)
- **Docker + Docker Compose** (container orchestration)

---

## 2. Cấu trúc thư mục

```
license-management/
├── backend/
│   └── LicenseManagement/
│       ├── LicenseManagement.sln
│       ├── src/
│       │   ├── LicenseManagement.Api/           # Web API project (entry point)
│       │   │   ├── Program.cs
│       │   │   ├── appsettings.json
│       │   │   ├── Controllers/
│       │   │   │   ├── AuthController.cs
│       │   │   │   ├── UsersController.cs
│       │   │   │   ├── ProductsController.cs
│       │   │   │   ├── LicensePlansController.cs
│       │   │   │   ├── LicensesController.cs
│       │   │   │   ├── PaymentsController.cs
│       │   │   │   ├── NotificationsController.cs
│       │   │   │   └── AdminController.cs
│       │   │   ├── Filters/
│       │   │   │   └── ApiExceptionFilter.cs
│       │   │   ├── Middleware/
│       │   │   │   └── RequestLoggingMiddleware.cs
│       │   │   └── Dockerfile
│       │   │
│       │   ├── LicenseManagement.Application/   # Business logic (CQRS + MediatR)
│       │   │   ├── Common/
│       │   │   │   ├── Interfaces/              # IRepository, IUnitOfWork, etc.
│       │   │   │   ├── Behaviors/               # ValidationBehavior, LoggingBehavior
│       │   │   │   ├── Models/                  # PagedResult, ApiResponse
│       │   │   │   └── Mappings/                # AutoMapper profiles
│       │   │   ├── Auth/
│       │   │   │   ├── Commands/                # RegisterCommand, LoginCommand
│       │   │   │   ├── Queries/
│       │   │   │   ├── DTOs/
│       │   │   │   └── Validators/
│       │   │   ├── Users/
│       │   │   │   ├── Commands/                # CreateUser, UpdateUser, LockUser
│       │   │   │   ├── Queries/                 # GetUsers, GetUserById
│       │   │   │   ├── DTOs/
│       │   │   │   └── Validators/
│       │   │   ├── Products/
│       │   │   ├── LicensePlans/
│       │   │   ├── Licenses/
│       │   │   │   ├── Commands/                # PurchaseLicense, ActivateLicense, RevokeLicense
│       │   │   │   ├── Queries/
│       │   │   │   ├── DTOs/
│       │   │   │   └── Validators/
│       │   │   ├── Payments/
│       │   │   │   ├── Commands/                # CreateTopUp, ProcessCallback
│       │   │   │   ├── Gateways/                # IPaymentGateway interface
│       │   │   │   └── DTOs/
│       │   │   └── Notifications/
│       │   │       ├── Commands/                # SendNotification
│       │   │       └── DTOs/
│       │   │
│       │   ├── LicenseManagement.Domain/        # Entities + Domain logic
│       │   │   ├── Entities/
│       │   │   │   ├── User.cs
│       │   │   │   ├── Product.cs
│       │   │   │   ├── LicenseProduct.cs
│       │   │   │   ├── UserLicense.cs
│       │   │   │   ├── LicenseActivation.cs
│       │   │   │   ├── Transaction.cs
│       │   │   │   ├── Notification.cs
│       │   │   │   ├── PushSubscription.cs
│       │   │   │   ├── SigningKey.cs
│       │   │   │   └── LicenseEvent.cs
│       │   │   ├── Enums/
│       │   │   │   ├── UserRole.cs
│       │   │   │   ├── LicenseStatus.cs
│       │   │   │   ├── TransactionType.cs
│       │   │   │   └── PaymentMethod.cs
│       │   │   └── Common/
│       │   │       └── BaseEntity.cs
│       │   │
│       │   └── LicenseManagement.Infrastructure/ # Data access + External services
│       │       ├── Data/
│       │       │   ├── AppDbContext.cs
│       │       │   ├── Configurations/          # EF Core Fluent API configs
│       │       │   │   ├── UserConfiguration.cs
│       │       │   │   ├── ProductConfiguration.cs
│       │       │   │   ├── UserLicenseConfiguration.cs
│       │       │   │   └── ...
│       │       │   ├── Migrations/
│       │       │   └── Seed/
│       │       │       └── DbSeeder.cs
│       │       ├── Repositories/
│       │       │   └── GenericRepository.cs
│       │       ├── Services/
│       │       │   ├── JwtService.cs
│       │       │   ├── LicenseCryptoService.cs  # Ed25519 sign/verify
│       │       │   ├── EmailService.cs          # MailKit
│       │       │   ├── WebPushService.cs
│       │       │   ├── FileStorageService.cs    # MinIO
│       │       │   └── Payment/
│       │       │       ├── IPaymentGateway.cs
│       │       │       ├── MoMoGateway.cs
│       │       │       ├── VnPayGateway.cs
│       │       │       └── ZaloPayGateway.cs
│       │       ├── BackgroundJobs/
│       │       │   ├── LicenseExpiryJob.cs
│       │       │   └── NotificationJob.cs
│       │       └── DependencyInjection.cs       # Service registration
│       │
│       └── tests/
│           ├── LicenseManagement.UnitTests/
│           └── LicenseManagement.IntegrationTests/
│
├── frontend/
│   ├── src/
│   │   ├── app/
│   │   │   ├── layout.tsx
│   │   │   ├── page.tsx                         # Landing page
│   │   │   ├── (auth)/
│   │   │   │   ├── login/page.tsx
│   │   │   │   ├── register/page.tsx
│   │   │   │   └── forgot-password/page.tsx
│   │   │   ├── (user)/
│   │   │   │   ├── dashboard/page.tsx
│   │   │   │   ├── licenses/page.tsx
│   │   │   │   ├── licenses/[id]/page.tsx
│   │   │   │   ├── topup/page.tsx
│   │   │   │   ├── transactions/page.tsx
│   │   │   │   ├── profile/page.tsx
│   │   │   │   └── notifications/page.tsx
│   │   │   └── (admin)/
│   │   │       ├── layout.tsx                   # Admin sidebar layout
│   │   │       ├── dashboard/page.tsx
│   │   │       ├── products/page.tsx
│   │   │       ├── products/[id]/page.tsx
│   │   │       ├── license-plans/page.tsx
│   │   │       ├── licenses/page.tsx
│   │   │       ├── users/page.tsx
│   │   │       ├── users/[id]/page.tsx
│   │   │       └── notifications/page.tsx
│   │   ├── components/
│   │   │   ├── ui/                              # Shared UI primitives
│   │   │   ├── layouts/                         # AdminLayout, UserLayout, Header, Sidebar
│   │   │   ├── forms/                           # ProductForm, LicensePlanForm
│   │   │   ├── tables/                          # UsersTable, LicensesTable
│   │   │   ├── modals/
│   │   │   └── cards/
│   │   ├── lib/
│   │   │   ├── api/                             # Axios client + endpoint functions
│   │   │   ├── hooks/                           # useAuth, useLicenses, useNotifications
│   │   │   ├── stores/                          # Zustand stores
│   │   │   └── utils/                           # format VND, date formatters
│   │   └── types/                               # TypeScript types
│   ├── public/sw.js                             # Service worker push
│   ├── Dockerfile
│   └── package.json
│
├── nginx/
│   └── nginx.conf
├── docker-compose.yml
├── docker-compose.dev.yml
├── .env.example
├── .gitignore
└── README.md
```

---

## 3. Kiến trúc Clean Architecture (.NET)

```
                    ┌─────────────────────┐
                    │   Api (Controllers)  │  ← HTTP layer, DI, Middleware
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │    Application       │  ← CQRS (MediatR), DTOs, Validators
                    │  (Commands/Queries)  │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │      Domain          │  ← Entities, Enums, Domain logic
                    │   (no dependencies)  │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │   Infrastructure     │  ← EF Core, External services
                    │  (DB, Email, Payment)│     (MoMo, VNPay, MinIO)
                    └─────────────────────┘
```

---

## 4. Database Schema (PostgreSQL + EF Core)

### Bảng chính:

**users** - Id(UUID), Email, Phone, PasswordHash, FullName, Role(enum: User/Admin), Balance(long VND), IsLocked, EmailVerified, AvatarUrl, CreatedAt, UpdatedAt

**products** - Id(UUID), Name, Slug(UNIQUE), Description, IconUrl, WebsiteUrl, IsActive, Metadata(JSONB), CreatedAt, UpdatedAt

**license_products** (gói license) - Id, ProductId(FK), Name, DurationDays, MaxActivations, Price(long VND), Features(JSONB), IsActive, CreatedAt, UpdatedAt

**user_licenses** (license đã mua) - Id, UserId(FK), LicenseProductId(FK), LicenseKey(UNIQUE signed token), Status(enum: Active/Expired/Revoked/Suspended), ActivatedAt, ExpiresAt, CurrentActivations, CreatedAt, UpdatedAt

**license_activations** (binding phần cứng) - Id, UserLicenseId(FK), HardwareId(SHA256), MachineName, IpAddress, ActivatedAt, LastSeenAt, IsActive, UNIQUE(UserLicenseId, HardwareId)

**transactions** - Id, UserId(FK), Type(enum: TopUp/Purchase/Refund/Renewal), Amount, BalanceBefore, BalanceAfter, PaymentMethod(enum: MoMo/VNPay/ZaloPay/Balance), PaymentRef, Status(enum: Pending/Completed/Failed), RelatedLicenseId(FK), CreatedAt, UpdatedAt

**notifications** - Id, UserId(FK nullable=broadcast), Title, Body, Type(enum), Channels(string[]), IsRead, SentAt, CreatedAt

**push_subscriptions** - Id, UserId(FK), Endpoint, P256dhKey, AuthKey, UNIQUE(UserId, Endpoint)

**signing_keys** (keypair Ed25519 per product) - Id, ProductId(FK), Algorithm, PublicKey, PrivateKeyEnc(AES-256-GCM), IsActive, CreatedAt, RotatedAt

**license_events** (audit log) - Id, UserLicenseId(FK), EventType, Details(JSONB), IpAddress, CreatedAt

---

## 5. Cơ chế License (Offline + Anti-forgery)

### Thuật toán: Ed25519 (System.Security.Cryptography)
- Server tạo keypair Ed25519 cho mỗi sản phẩm
- Private key lưu encrypted (AES-256-GCM) trong DB, dùng MASTER_KEY từ env
- Public key nhúng vào phần mềm client

### License Token Format:
```
BASE64URL(LICENSE_DATA).BASE64URL(ED25519_SIGNATURE)
```
LICENSE_DATA (JSON): `{ lid, pid, uid, tier, features, maxAct, iat, exp, hwid }`

### Hardware Fingerprint:
```csharp
HWID = SHA256(cpuId + mbSerial + diskSerial + osInstallId + macAddress)
```

### Flow kích hoạt:
1. Client gửi `POST /api/v1/licenses/activate` với { licenseKey, hardwareId, machineName }
2. Server validate: key tồn tại, chưa vượt max_activations, account không bị khóa
3. Server ký payload bằng Ed25519 private key → trả về signedLicenseToken + publicKey
4. Client lưu token encrypted trên disk (encrypt bằng HWID-derived key)

### Validate offline (client-side):
1. Client đọc token → decrypt → split DATA + SIGNATURE
2. Verify signature bằng public key nhúng sẵn
3. Check: exp > now, hwid == current machine HWID, features hợp lệ
4. **Không cần kết nối mạng** → phần mềm hoạt động bình thường

### Heartbeat (online, periodic):
- Mỗi 24-72h client gọi `POST /api/v1/licenses/heartbeat`
- Kiểm tra license chưa bị thu hồi, refresh token nếu gần hết hạn
- Grace period 30 ngày offline trước khi giới hạn tính năng

### LicenseCryptoService.cs (core file):
```csharp
public class LicenseCryptoService
{
    // GenerateKeyPair() → (publicKey, privateKey)
    // SignLicense(LicensePayload payload, byte[] privateKey) → string token
    // VerifyLicense(string token, byte[] publicKey) → LicensePayload?
    // EncryptPrivateKey(byte[] privateKey, string masterKey) → string encrypted
    // DecryptPrivateKey(string encrypted, string masterKey) → byte[]
}
```

---

## 6. API Endpoints chính

### Auth: `/api/v1/auth`
- POST `/register` - Đăng ký (email, phone, password, fullName)
- POST `/login` - Đăng nhập → { accessToken, refreshToken }
- POST `/refresh` - Refresh access token
- POST `/forgot-password` - Gửi email reset password
- POST `/reset-password` - Reset password với token
- POST `/verify-email` - Xác thực email
- POST `/logout` - Invalidate refresh token

### Users (Admin): `/api/v1/users`
- GET `/` - Danh sách users (paginated, filterable)
- GET `/{id}` - Chi tiết user
- PUT `/{id}` - Cập nhật user
- PUT `/{id}/lock` - Khóa/mở khóa
- DELETE `/{id}` - Soft-delete

### Profile (User): `/api/v1/me`
- GET `/` - Profile
- PUT `/` - Cập nhật profile
- GET `/balance` - Số dư
- GET `/transactions` - Lịch sử giao dịch
- GET `/licenses` - License đã mua
- GET `/notifications` - Thông báo

### Products: `/api/v1/products`
- GET `/` (public) - Danh sách sản phẩm
- GET `/{slug}` (public) - Chi tiết
- POST `/` [Admin] - Tạo mới
- PUT `/{id}` [Admin] - Cập nhật
- DELETE `/{id}` [Admin] - Xóa

### License Plans: `/api/v1/products/{productId}/plans`
- GET `/` - Danh sách gói
- POST `/` [Admin], PUT `/{id}` [Admin], DELETE `/{id}` [Admin]

### Licenses: `/api/v1/licenses`
- POST `/purchase` - Mua license (trừ balance)
- POST `/activate` - Kích hoạt trên máy (client gọi)
- POST `/deactivate` - Hủy kích hoạt
- POST `/heartbeat` - Heartbeat check
- POST `/validate` - Online validate
- POST `/renew` - Gia hạn
- GET `/{id}` - Chi tiết
- GET `/` [Admin] - Tất cả licenses
- PUT `/{id}/revoke` [Admin] - Thu hồi
- PUT `/{id}/suspend` [Admin] - Tạm dừng
- PUT `/{id}/reinstate` [Admin] - Khôi phục

### Payments: `/api/v1/payments`
- POST `/topup` - Tạo lệnh nạp tiền → trả paymentUrl
- POST `/momo/callback` - MoMo IPN
- POST `/vnpay/callback` - VNPay IPN
- POST `/zalopay/callback` - ZaloPay callback
- GET `/topup/{id}/status` - Check trạng thái

### Notifications: `/api/v1/notifications`
- POST `/` [Admin] - Gửi thông báo
- GET `/` [Admin] - Danh sách đã gửi

### Admin Dashboard: `/api/v1/admin/stats`
- GET `/overview` - Tổng quan (users, revenue, licenses)
- GET `/revenue` - Doanh thu theo thời gian
- GET `/licenses` - Thống kê license theo sản phẩm

---

## 7. Payment Integration (MoMo, VNPay, ZaloPay)

### Abstract Gateway Pattern (C#):
```csharp
public interface IPaymentGateway
{
    Task<CreateOrderResult> CreateOrderAsync(CreateOrderParams request);
    bool VerifyCallback(string payload, string signature);
    CallbackResult ParseCallback(string payload);
}
```

Implement: `MoMoGateway`, `VnPayGateway`, `ZaloPayGateway`

### Flow:
1. User chọn nạp tiền → `POST /payments/topup` → tạo Transaction(Pending)
2. Server gọi gateway API → trả paymentUrl cho frontend redirect
3. User thanh toán trên trang gateway
4. Gateway gửi IPN callback → Server verify HMAC signature → cập nhật balance (atomic with row lock)
5. Gửi notification cho user

### Bảo mật:
- Verify HMAC-SHA256 signature từ gateway
- Idempotency (transaction ID) chống double-credit
- Log toàn bộ callback payload cho audit

---

## 8. Notification System

- **In-app**: Lưu DB + đẩy realtime qua **SSE** (Server-Sent Events) từ ASP.NET Core
- **Email**: MailKit + Razor/Scriban templates, gửi async qua Hangfire
- **Web Push**: VAPID keys + WebPush-NetCore + Service Worker
- **Scheduled jobs** (Hangfire): Cảnh báo license sắp hết hạn (7 ngày, 1 ngày trước)

---

## 9. Docker Architecture

```yaml
services:
  postgres:       # PostgreSQL 16 Alpine, volume: pgdata
  redis:          # Redis 7 Alpine (password), volume: redisdata
  minio:          # MinIO object storage, volume: miniodata
  backend:        # ASP.NET Core 10 (port 5000), depends: postgres, redis
  hangfire-worker:# Cùng image backend, chạy Hangfire server
  frontend:       # Next.js 14 (port 3000)
  nginx:          # Reverse proxy (80/443 → backend + frontend)
```

### Backend Dockerfile (multi-stage):
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/LicenseManagement.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 5000
ENTRYPOINT ["dotnet", "LicenseManagement.Api.dll"]
```

### Nginx routing:
- `/api/*` → backend:5000
- `/hangfire` → backend:5000 (Hangfire dashboard, admin only)
- `/` → frontend:3000

---

## 10. Security

- **JWT**: Access token 15min + Refresh token 7 ngày (HttpOnly Secure cookie, rotation)
- **RBAC**: ASP.NET Core `[Authorize(Roles = "Admin")]` + Policy-based authorization
- **Rate limiting**: AspNetCoreRateLimit - 100 req/min chung, 5/min cho auth
- **Password**: ASP.NET Core Identity (PBKDF2) hoặc custom Argon2id
- **License**: Ed25519 signing + hardware binding + encrypted client storage
- **Private key**: AES-256-GCM với MASTER_ENCRYPTION_KEY từ environment
- **DB**: Internal Docker network only, non-root PostgreSQL user
- **CORS**: Strict origin whitelist
- **HTTPS**: TLS 1.2+ qua Nginx
- **Input validation**: FluentValidation trên mọi request DTO
- **SQL injection**: EF Core parameterized queries (built-in protection)
- **Logging**: Serilog structured logging → console + file (hoặc Seq/ELK)

---

## 11. Phases triển khai

### Phase 1: Foundation
- Init solution Clean Architecture (4 projects: Api, Application, Domain, Infrastructure)
- Cấu hình EF Core + PostgreSQL + Code-First Migrations cho toàn bộ schema
- Auth module: Register, Login, JWT access+refresh, email verification
- Users module: CRUD, lock/unlock
- Docker Compose: PostgreSQL + Redis + backend
- Init Next.js + Ant Design
- Login/Register pages + auth flow (JWT storage, refresh interceptor)

### Phase 2: Products & License Core
- Products CRUD (controller + MediatR handlers)
- License plans CRUD
- **LicenseCryptoService** (Ed25519 sign/verify - CRITICAL)
- Licenses module: purchase (trừ balance), activate, deactivate, heartbeat, validate
- Admin pages: product management (CRUD forms + tables)
- User pages: license purchase flow + license dashboard
- Seed data: sample products + plans

### Phase 3: Payments
- IPaymentGateway interface + MoMoGateway, VnPayGateway, ZaloPayGateway
- Top-up flow: tạo order → redirect → IPN callback → update balance
- Hangfire jobs cho async payment processing
- Frontend: Top-up page (chọn gateway + amount) + transaction history
- Test với sandbox environments (MoMo test, VNPay sandbox)

### Phase 4: Notifications
- MailKit + email templates (Razor/Scriban)
- Web Push (VAPID + Service Worker)
- SSE endpoint cho realtime in-app notifications
- Admin: trang gửi notification (target user/broadcast, channels)
- Hangfire scheduled: license expiry warnings (7 ngày, 1 ngày)

### Phase 5: Security & Admin Dashboard
- Rate limiting (AspNetCoreRateLimit)
- CORS strict configuration
- Refresh token rotation + revocation
- Admin dashboard: charts (revenue, users, licenses over time) dùng Ant Design Charts
- Nginx + TLS + security headers

### Phase 6: Testing & Deployment
- Unit tests: LicenseCryptoService, payment gateways
- Integration tests: Auth flow, purchase flow, activation flow
- Production docker-compose (resource limits, healthchecks)
- CI/CD pipeline (GitHub Actions: build, test, Docker push)
- Database backup strategy (pg_dump cron)
- API documentation (Swagger)

---

## Verification

1. **Auth flow**: Đăng ký → verify email → login → access protected routes → refresh token
2. **License flow**: Admin tạo product → tạo plan → user mua (trừ balance) → activate với HWID → validate offline (verify Ed25519 signature)
3. **Payment flow**: User top-up qua MoMo sandbox → callback IPN → balance updated → notification
4. **Notification**: Admin gửi notification → user nhận realtime qua SSE + email
5. **Docker**: `docker-compose up` → tất cả services healthy, Nginx proxy hoạt động
6. **Security**: Modify license token → verify fail; copy license sang máy khác → HWID mismatch → reject
