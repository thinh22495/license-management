# License Management System

Hệ thống quản lý giấy phép phần mềm hoàn chỉnh, hỗ trợ cả **offline** và **online**, chống giả mạo bằng **chữ ký số ECDSA P-256** + hardware fingerprinting. Quản lý, tạo, kích hoạt, thu hồi license cho nhiều sản phẩm phần mềm cùng lúc.

## Tech Stack

| Layer | Công nghệ |
|-------|-----------|
| **Backend** | ASP.NET Core 10, Entity Framework Core 10, MediatR (CQRS), FluentValidation |
| **Frontend** | Next.js 14 (App Router), Ant Design 5, Zustand, TanStack Query, React Hook Form + Zod |
| **Database** | PostgreSQL 16, Redis 8 (cache + Hangfire queue) |
| **Infra** | Docker, Nginx (reverse proxy + TLS), Hangfire (background jobs) |
| **Payment** | MoMo, VNPay, ZaloPay (sandbox-ready) |
| **Security** | JWT (access + refresh token rotation), ECDSA P-256 license signing, rate limiting, CORS |

## Kiến trúc

```
┌─────────────────────────┐
│   Api (Controllers)     │  ← HTTP, Filters, Middleware, DI
└───────────┬─────────────┘
            │
┌───────────▼─────────────┐
│      Application        │  ← CQRS Commands/Queries, DTOs, Validators
└───────────┬─────────────┘
            │
┌───────────▼─────────────┐
│        Domain           │  ← Entities, Enums, Value Objects (no dependencies)
└───────────┬─────────────┘
            │
┌───────────▼─────────────┐
│    Infrastructure       │  ← EF Core, Payment Gateways, Email, JWT, Crypto
└─────────────────────────┘
```

## Cấu trúc thư mục

```
license-management/
├── backend/LicenseManagement/
│   ├── src/
│   │   ├── LicenseManagement.Api/            # Web API entry point
│   │   │   ├── Controllers/                  # 9 controllers
│   │   │   ├── Filters/                      # ApiExceptionFilter
│   │   │   └── Dockerfile
│   │   ├── LicenseManagement.Application/    # Business logic (CQRS)
│   │   │   ├── Auth/                         # Register, Login, Refresh, Logout
│   │   │   ├── Users/                        # CRUD, Lock/Unlock
│   │   │   ├── Products/                     # CRUD products
│   │   │   ├── LicensePlans/                 # CRUD license plans
│   │   │   ├── Licenses/                     # Purchase, Activate, Revoke, Heartbeat
│   │   │   ├── Payments/                     # TopUp, Callbacks, Gateways
│   │   │   ├── Notifications/                # Send, Mark Read, SSE
│   │   │   └── Dashboard/                    # Stats, Charts
│   │   ├── LicenseManagement.Domain/         # Entities + Enums
│   │   └── LicenseManagement.Infrastructure/ # DB, Services, Jobs
│   └── tests/
│       ├── LicenseManagement.UnitTests/      # 25 unit tests
│       └── LicenseManagement.IntegrationTests/ # 6 integration tests
├── frontend/
│   └── src/
│       ├── app/(auth)/                       # Login, Register
│       ├── app/(user)/                       # Products, Licenses, TopUp, Profile
│       ├── app/(admin)/admin/                # Dashboard, Products, Licenses, Users, Notifications
│       ├── components/                       # NotificationBell, shared components
│       ├── lib/api/                          # API clients (auth, products, licenses, payments, etc.)
│       └── types/                            # TypeScript type definitions
├── nginx/nginx.conf                          # Reverse proxy, rate limiting, security headers
├── scripts/                                  # Backup scripts (pg_dump + cron)
├── docker-compose.yml                        # Development
├── docker-compose.prod.yml                   # Production (healthchecks, resource limits)
└── .github/workflows/ci.yml                  # CI/CD pipeline
```

## Tính năng

### Quản trị viên (Admin)
- Dashboard tổng quan với biểu đồ doanh thu, license, users (Line, Column, Pie charts)
- CRUD sản phẩm phần mềm + gói license (giá, thời hạn, max activations, features)
- Quản lý license: xem tất cả, thu hồi, tạm dừng, khôi phục
- Quản lý người dùng: danh sách, khóa/mở khóa tài khoản
- Gửi thông báo (broadcast/cá nhân) qua web + email

### Người dùng (User)
- Đăng ký/đăng nhập với JWT authentication
- Duyệt sản phẩm, mua license (trừ balance)
- Kích hoạt license trên máy (hardware binding)
- Nạp tiền qua MoMo, VNPay, ZaloPay
- Quản lý license đã mua, gia hạn
- Xem lịch sử giao dịch
- Nhận thông báo realtime (SSE) + email

### Cơ chế License

```
License Token = BASE64URL(LICENSE_DATA) . BASE64URL(ECDSA_SIGNATURE)
```

- **ECDSA P-256** signing cho mỗi sản phẩm (keypair riêng)
- Private key encrypted bằng **AES-256-GCM** với MASTER_KEY
- **Hardware Fingerprint**: `SHA256(cpuId + mbSerial + diskSerial + osInstallId + macAddress)`
- **Offline validation**: Client verify signature bằng public key nhúng sẵn, không cần mạng
- **Heartbeat**: Periodic online check (24-72h), grace period 30 ngày offline

### Payment Integration

| Gateway | Signing | Format |
|---------|---------|--------|
| MoMo | HMAC-SHA256 | JSON |
| VNPay | HMAC-SHA512 | Query string (SortedDictionary) |
| ZaloPay | HMAC-SHA256 (dual-key) | Form-encoded |

Flow: User chọn nạp tiền → tạo Transaction(Pending) → redirect gateway → IPN callback → verify signature → cập nhật balance → notification.

## API Endpoints

| Group | Prefix | Endpoints |
|-------|--------|-----------|
| Auth | `/api/v1/auth` | register, login, refresh, logout |
| Profile | `/api/v1/me` | get/update profile, balance |
| Products | `/api/v1/products` | list (public), CRUD (admin) |
| License Plans | `/api/v1/license-plans` | list by product, CRUD (admin) |
| Licenses | `/api/v1/licenses` | purchase, activate, deactivate, heartbeat, validate, renew, revoke/suspend/reinstate (admin) |
| Payments | `/api/v1/payments` | topup, transactions, MoMo/VNPay/ZaloPay callbacks |
| Notifications | `/api/v1/notifications` | list, unread count, mark read, SSE stream, send (admin) |
| Dashboard | `/api/v1/dashboard` | stats, charts (admin) |
| Users | `/api/v1/users` | list, lock/unlock (admin) |
| Health | `/api/v1/health` | health check |

## Cài đặt & Chạy

### Yêu cầu
- Docker & Docker Compose
- .NET 10 SDK (phát triển)
- Node.js 20+ (phát triển)

### Development

```bash
# 1. Clone repo
git clone <repo-url>
cd license-management

# 2. Copy và cấu hình environment
cp .env.example .env

# 3. Chạy infrastructure (PostgreSQL + Redis)
docker compose up -d postgres redis

# 4. Chạy backend
cd backend/LicenseManagement
dotnet restore
dotnet run --project src/LicenseManagement.Api

# 5. Chạy frontend (terminal khác)
cd frontend
npm install
npm run dev
```

Hoặc chạy tất cả bằng Docker:

```bash
docker compose up --build
```

- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Hangfire Dashboard: http://localhost:5000/hangfire

### Tài khoản mặc định (seed data)

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@licensemanagement.com | Admin@123 |
| User | user@demo.com | User@123 |

Seed data bao gồm 3 sản phẩm mẫu (ScreenCapture Pro, CodeEditor Ultimate, DataSync Manager) và 7 gói license.

### Production

```bash
# 1. Cấu hình environment
cp .env.production.example .env

# 2. Sửa .env với giá trị production thật
# (JWT_SECRET, DB_PASSWORD, MASTER_KEY, payment keys, SMTP, domain...)

# 3. Deploy
docker compose -f docker-compose.prod.yml up -d
```

Production bao gồm:
- Healthchecks cho tất cả services
- Resource limits (CPU/Memory)
- Log rotation (json-file driver)
- Auto backup database (pg_dump daily via cron)
- SSL-ready Nginx configuration

## Testing

```bash
cd backend/LicenseManagement

# Chạy tất cả tests
dotnet test

# Chạy riêng unit tests (25 tests)
dotnet test tests/LicenseManagement.UnitTests/

# Chạy riêng integration tests (6 tests)
dotnet test tests/LicenseManagement.IntegrationTests/
```

**Unit Tests** (25 tests):
- `LicenseCryptoServiceTests` - Key generation, sign/verify, tamper detection, encrypt/decrypt
- `PasswordHasherTests` - Hash, verify, salt uniqueness, invalid input handling
- `JwtServiceTests` - Token generation, validation, refresh tokens
- `VnPayGatewayTests` - Order creation, signature verification, callback parsing

**Integration Tests** (6 tests):
- `AuthFlowTests` - Register, duplicate email, login, wrong password, protected endpoints

## CI/CD

GitHub Actions pipeline (`.github/workflows/ci.yml`):
1. **Backend Build & Test** - Restore, build, run unit + integration tests
2. **Frontend Build & Lint** - Install, type check, lint, build
3. **Docker Build** - Build & push images to GHCR (on push to main)
4. **Deploy** - SSH deploy to production server (on push to main)

## Database Schema

| Bảng | Mô tả |
|------|-------|
| `users` | Tài khoản (email, role, balance, lock status) |
| `products` | Sản phẩm phần mềm |
| `license_products` | Gói license (giá, thời hạn, max activations, features) |
| `user_licenses` | License đã mua (key, status, expiry) |
| `license_activations` | Binding phần cứng (hardware ID, machine name) |
| `transactions` | Giao dịch nạp tiền / mua license |
| `notifications` | Thông báo web + email |
| `signing_keys` | ECDSA keypair per product (private key encrypted) |
| `license_events` | Audit log |
| `push_subscriptions` | Push notification subscriptions |

## Bảo mật

- **JWT**: Access token 15 phút + Refresh token 7 ngày (HttpOnly cookie, rotation)
- **RBAC**: `[Authorize(Roles = "Admin")]` trên các endpoint admin
- **Rate Limiting**: ASP.NET (AspNetCoreRateLimit) + Nginx (dual layer)
  - General: 100 req/min, Auth: 5/min, Activate: 10/min, TopUp: 5/min
- **License**: ECDSA P-256 signing + hardware binding + encrypted storage
- **Private Key**: AES-256-GCM encrypted với MASTER_KEY từ environment
- **Security Headers**: X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, Referrer-Policy, Permissions-Policy
- **CORS**: Strict origin whitelist
- **Input Validation**: FluentValidation pipeline behavior trên mọi request
- **SQL Injection**: EF Core parameterized queries (built-in protection)
- **Logging**: Serilog structured logging
