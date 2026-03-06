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

## Luồng kích hoạt & sử dụng License

Hệ thống có 2 phía: **Web Portal** (quản lý trên trình duyệt) và **Client Software** (phần mềm desktop sử dụng license). Dưới đây là toàn bộ luồng hoạt động:

### Flow 1: Mua license trên Web Portal

```
Người dùng (Browser)                    Backend Server
      │                                      │
      │── [1] Nạp tiền (TopUp) ────────────>│  POST /api/v1/payments/topup
      │    (MoMo / VNPay / ZaloPay)          │  → tạo Transaction(Pending)
      │<── redirect tới payment gateway ─────│  → trả paymentUrl
      │── thanh toán trên gateway ─────────>│
      │    gateway gửi IPN callback ───────>│  POST /api/v1/payments/{gateway}/callback
      │                                      │  → verify signature, cập nhật balance
      │                                      │
      │── [2] Duyệt sản phẩm, chọn gói ───>│  GET /api/v1/products
      │                                      │  GET /api/v1/products/{id}/plans
      │                                      │
      │── [3] Mua license ─────────────────>│  POST /api/v1/licenses/purchase
      │    { licensePlanId }                 │  → trừ balance, tạo UserLicense
      │<── trả về LicenseKey (LM-XXXXX) ────│  → key format: LM-{UUID}[0:24]
      │                                      │
      │── [4] Copy key, dán vào software ───│  (trang /licenses, nút Copy)
```

**Kết quả:** Người dùng có license key → cần nhập vào phần mềm desktop để kích hoạt.

---

### Flow 2: Nhập key (Redeem) - Dành cho key được tặng/mua ngoài

```
Người dùng (Browser)                    Backend Server
      │                                      │
      │    Nhận key từ admin/người khác       │
      │    (email, tin nhắn, v.v.)            │
      │                                      │
      │── [1] Vào trang /licenses ──────────>│
      │    Nhấn nút "Nhập key"               │
      │                                      │
      │── [2] Gửi key ─────────────────────>│  POST /api/v1/licenses/redeem
      │    { licenseKey: "LM-XXXXX" }        │  → Tìm key chưa gán user (Pending)
      │                                      │  → Gán UserId = current user
      │                                      │  → Status: Pending → Active
      │<── Thành công, hiển thị license ─────│  → Trả về LicenseDto
      │                                      │
      │    Key giờ thuộc về user này          │
      │    Hiển thị trong "License của tôi"  │
```

**Lưu ý:** Admin có thể tạo key không gán user (key trống) → User nhập key để nhận license.

---

### Flow 3: Kích hoạt license trong phần mềm (Client Software - Online)

```
Client Software (Desktop)               Backend Server
      │                                      │
      │── [1] Thu thập Hardware Fingerprint   │
      │    HWID = SHA256(cpuId + mbSerial     │
      │    + diskSerial + osInstallId         │
      │    + macAddress)                      │
      │                                      │
      │── [2] Gửi yêu cầu kích hoạt ──────>│  POST /api/v1/licenses/activate
      │    { licenseKey, hardwareId,          │
      │      machineName }                    │
      │                                      │  Validate:
      │                                      │  ✓ Key tồn tại?
      │                                      │  ✓ Status == Active?
      │                                      │  ✓ Chưa hết hạn?
      │                                      │  ✓ User chưa bị khóa?
      │                                      │  ✓ Chưa vượt max activations?
      │                                      │
      │<── [3] Trả về ─────────────────────│  { SignedLicenseToken, PublicKey }
      │    SignedLicenseToken =              │  Token = Base64Url(JSON).Base64Url(Sig)
      │    PublicKey (ECDSA P-256)            │
      │                                      │
      │── [4] Lưu trữ local:                │
      │    • SignedLicenseToken               │
      │      (encrypt bằng HWID-derived key) │
      │    • Nhúng PublicKey vào app          │
      │    • Ghi thời điểm kích hoạt         │
```

**Token Payload:**
```json
{
  "Lid": "license-uuid",
  "Pid": "product-uuid",
  "Uid": "user-uuid",
  "Tier": "Professional",
  "Features": ["feature1", "feature2"],
  "MaxAct": 3,
  "Iat": 1709712000,
  "Exp": 1741248000,
  "Hwid": "sha256-hardware-fingerprint"
}
```

---

### Flow 4: Kiểm tra license Offline (Không cần mạng)

```
Client Software (Desktop)               Không cần server
      │
      │── [1] Đọc token từ local storage
      │    (decrypt bằng HWID-derived key)
      │
      │── [2] Split: DATA_PART.SIGNATURE_PART
      │
      │── [3] Verify chữ ký ECDSA P-256
      │    bằng PublicKey nhúng sẵn trong app
      │    → Nếu sai: LICENSE INVALID (bị sửa đổi)
      │
      │── [4] Parse JSON payload
      │    → Check Exp > now (chưa hết hạn)
      │    → Check Hwid == HWID hiện tại (đúng máy)
      │    → Check Features (quyền sử dụng)
      │
      │── [5] Nếu tất cả OK → MỞ KHÓA PHẦN MỀM
      │    Nếu FAIL → THÔNG BÁO LỖI + KHÓA TÍNH NĂNG
```

**Ưu điểm:** Phần mềm hoạt động bình thường **không cần internet**. Token được ký ECDSA nên **không thể giả mạo**.

---

### Flow 5: Heartbeat - Kiểm tra định kỳ (Online)

```
Client Software (Desktop)               Backend Server
      │                                      │
      │── [Mỗi 24-72h] ──────────────────>│  POST /api/v1/licenses/heartbeat
      │    { licenseKey, hardwareId }        │
      │                                      │  → Cập nhật LastSeenAt
      │                                      │  → Kiểm tra: bị thu hồi? tạm dừng?
      │                                      │  → Nếu token gần hết hạn (< 7 ngày):
      │                                      │     ký lại token mới (refresh)
      │<── Trả về ─────────────────────────│  { Status, SignedLicenseToken?, ExpiresAt }
      │                                      │
      │    Xử lý theo Status:               │
      │    • "Active" → tiếp tục bình thường│
      │    • "Revoked" → KHÓA ngay lập tức  │
      │    • "Suspended" → KHÓA + "liên hệ  │
      │      admin"                          │
      │    • Có token mới → cập nhật local   │
      │                                      │
      │    GRACE PERIOD: 30 ngày offline     │
      │    → Nếu > 30 ngày không heartbeat:  │
      │       giới hạn tính năng             │
```

---

### Flow 6: Hủy kích hoạt thiết bị

**Cách 1: Từ Client Software**
```
Client Software (Desktop)               Backend Server
      │                                      │
      │── Gửi yêu cầu hủy ───────────────>│  POST /api/v1/licenses/deactivate
      │    { licenseKey, hardwareId }        │  → IsActive = false
      │                                      │  → CurrentActivations -= 1
      │<── "Đã hủy kích hoạt" ─────────────│  → Ghi LicenseEvent
      │                                      │
      │── Xóa token local                   │
```

**Cách 2: Từ Web Portal (Remote Deactivate)**
```
Người dùng (Browser)                    Backend Server
      │                                      │
      │── Vào trang /licenses ─────────────>│  GET /api/v1/licenses/{id}/activations
      │    Xem danh sách thiết bị            │  → Trả về list thiết bị đã kích hoạt
      │                                      │
      │── Nhấn "Hủy kích hoạt" trên ──────>│  DELETE /api/v1/licenses/{id}/activations/{aid}
      │    thiết bị cần hủy                  │  → Verify user sở hữu license
      │                                      │  → IsActive = false
      │<── Thành công ─────────────────────│  → CurrentActivations -= 1
      │                                      │
      │    Thiết bị bị hủy sẽ nhận          │
      │    heartbeat trả "NeedsReactivation" │
```

---

### Flow 7: Validate Online (Kiểm tra trực tuyến)

```
Client Software (Desktop)               Backend Server
      │                                      │
      │── Gửi yêu cầu validate ───────────>│  POST /api/v1/licenses/validate
      │    { licenseKey, hardwareId }        │  → Kiểm tra toàn bộ:
      │                                      │     key, status, expiry, user locked,
      │                                      │     hardware binding
      │<── Trả về ─────────────────────────│  { Valid, Status, ExpiresAt, Tier, Features }
      │                                      │
      │    Nếu Valid == false:               │
      │    → Xử lý theo Status:             │
      │      "expired" → yêu cầu gia hạn    │
      │      "revoked" → khóa phần mềm      │
      │      "suspended" → liên hệ admin     │
      │      "not_activated" → yêu cầu       │
      │        kích hoạt lại                 │
```

---

### Flow 8: Gia hạn License

```
Người dùng (Browser)                    Backend Server
      │                                      │
      │── [1] Vào trang /licenses ─────────>│  GET /api/v1/licenses/my
      │    Nhấn nút "Gia hạn"               │
      │                                      │
      │── [2] Gửi yêu cầu gia hạn ────────>│  POST /api/v1/licenses/renew
      │    { licenseId }                     │  → Trừ balance theo giá gói
      │                                      │  → ExpiresAt += DurationDays
      │                                      │  → Status = Active (nếu đã Expired)
      │<── Thành công, ExpiresAt mới ───────│  → Ghi Transaction + LicenseEvent
      │                                      │
      │    Heartbeat tiếp theo sẽ trả về     │
      │    token mới với Exp cập nhật        │
```

---

### Tổng quan luồng hoàn chỉnh

```
┌──────────────────────────────────────────────────────────────────┐
│                        WEB PORTAL                                │
│                                                                  │
│  [Nạp tiền] → [Mua license] → [Nhận key] ──┐                   │
│                                              │                   │
│  [Nhận key từ admin] → [Nhập key/Redeem] ───┤                   │
│                                              ▼                   │
│                                    [Quản lý license]             │
│                                    • Xem thiết bị               │
│                                    • Hủy kích hoạt từ xa        │
│                                    • Gia hạn                     │
└──────────────────────────┬───────────────────────────────────────┘
                           │ Copy key
                           ▼
┌──────────────────────────────────────────────────────────────────┐
│                     CLIENT SOFTWARE                              │
│                                                                  │
│  [Nhập key] → [Kích hoạt online] → [Nhận Token + PublicKey]     │
│                                              │                   │
│                                              ▼                   │
│                                    [Sử dụng phần mềm]           │
│                                    • Verify offline (ECDSA)      │
│                                    • Heartbeat 24-72h            │
│                                    • Grace period 30 ngày        │
│                                    • Auto-refresh token          │
└──────────────────────────────────────────────────────────────────┘
```

## Hướng dẫn tích hợp Client SDK

Phần này dành cho nhà phát triển phần mềm muốn tích hợp hệ thống license vào ứng dụng desktop.

### 1. Tạo Hardware Fingerprint

**C# (.NET):**
```csharp
using System.Management;
using System.Security.Cryptography;
using System.Text;

public static string GenerateHardwareId()
{
    var sb = new StringBuilder();

    // CPU ID
    using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
        foreach (var obj in searcher.Get())
            sb.Append(obj["ProcessorId"]);

    // Motherboard Serial
    using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
        foreach (var obj in searcher.Get())
            sb.Append(obj["SerialNumber"]);

    // Disk Serial
    using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive WHERE Index=0"))
        foreach (var obj in searcher.Get())
            sb.Append(obj["SerialNumber"]?.ToString()?.Trim());

    // OS Install ID
    using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_OperatingSystem"))
        foreach (var obj in searcher.Get())
            sb.Append(obj["SerialNumber"]);

    // MAC Address
    using (var searcher = new ManagementObjectSearcher(
        "SELECT MACAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=True"))
        foreach (var obj in searcher.Get())
        { sb.Append(obj["MACAddress"]); break; }

    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
    return Convert.ToHexString(hash).ToLower();
}
```

**Python:**
```python
import hashlib, subprocess, uuid, platform

def generate_hardware_id() -> str:
    parts = []

    # CPU ID (Windows)
    if platform.system() == "Windows":
        result = subprocess.run(
            ["wmic", "cpu", "get", "ProcessorId"],
            capture_output=True, text=True
        )
        parts.append(result.stdout.strip().split("\n")[-1].strip())

    # MAC Address
    mac = ':'.join(f'{uuid.getnode():012x}'[i:i+2] for i in range(0, 12, 2))
    parts.append(mac)

    # Machine ID (Linux: /etc/machine-id, Windows: MachineGuid registry)
    try:
        with open("/etc/machine-id") as f:
            parts.append(f.read().strip())
    except FileNotFoundError:
        result = subprocess.run(
            ["reg", "query",
             r"HKLM\SOFTWARE\Microsoft\Cryptography",
             "/v", "MachineGuid"],
            capture_output=True, text=True
        )
        for line in result.stdout.split("\n"):
            if "MachineGuid" in line:
                parts.append(line.split()[-1])

    combined = "".join(parts)
    return hashlib.sha256(combined.encode()).hexdigest()
```

### 2. Gọi API kích hoạt

```csharp
// C# - Kích hoạt license
public async Task<ActivationResult> ActivateLicense(string licenseKey)
{
    var hwid = GenerateHardwareId();
    var payload = new {
        licenseKey = licenseKey,
        hardwareId = hwid,
        machineName = Environment.MachineName
    };

    var response = await httpClient.PostAsJsonAsync(
        "https://your-server.com/api/v1/licenses/activate", payload);

    if (!response.IsSuccessStatusCode)
        throw new LicenseException(await response.Content.ReadAsStringAsync());

    var result = await response.Content.ReadFromJsonAsync<ApiResponse<ActivationResult>>();

    // Lưu token và public key
    SaveTokenSecurely(result.Data.SignedLicenseToken, hwid);
    SavePublicKey(result.Data.PublicKey);

    return result.Data;
}

public record ActivationResult(string SignedLicenseToken, string PublicKey);
```

### 3. Verify license Offline (ECDSA P-256)

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public static LicensePayload VerifyLicenseOffline(string token, string publicKeyBase64)
{
    var parts = token.Split('.');
    if (parts.Length != 2)
        throw new LicenseException("Invalid token format");

    var dataBytes = Base64UrlDecode(parts[0]);
    var signatureBytes = Base64UrlDecode(parts[1]);

    // Import public key
    using var ecdsa = ECDsa.Create();
    ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKeyBase64), out _);

    // Verify signature
    bool isValid = ecdsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256);
    if (!isValid)
        throw new LicenseException("Invalid signature - license tampered!");

    // Parse payload
    var payload = JsonSerializer.Deserialize<LicensePayload>(dataBytes);

    // Check expiry
    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    if (payload.Exp < now)
        throw new LicenseException("License expired");

    // Check hardware
    var currentHwid = GenerateHardwareId();
    if (payload.Hwid != currentHwid)
        throw new LicenseException("License not activated on this device");

    return payload;
}

private static byte[] Base64UrlDecode(string input)
{
    var s = input.Replace('-', '+').Replace('_', '/');
    switch (s.Length % 4) {
        case 2: s += "=="; break;
        case 3: s += "="; break;
    }
    return Convert.FromBase64String(s);
}
```

### 4. Heartbeat định kỳ

```csharp
// Chạy heartbeat mỗi 24h (dùng Timer hoặc BackgroundService)
public async Task Heartbeat(string licenseKey)
{
    var hwid = GenerateHardwareId();
    var payload = new { licenseKey, hardwareId = hwid };

    try {
        var response = await httpClient.PostAsJsonAsync(
            "https://your-server.com/api/v1/licenses/heartbeat", payload);

        var result = await response.Content
            .ReadFromJsonAsync<ApiResponse<HeartbeatResponse>>();

        if (result.Data.Status == "Revoked" || result.Data.Status == "Suspended")
        {
            LockApplication(result.Data.Status);
            return;
        }

        // Cập nhật token mới nếu có
        if (!string.IsNullOrEmpty(result.Data.SignedLicenseToken))
            SaveTokenSecurely(result.Data.SignedLicenseToken, hwid);

        LastHeartbeatTime = DateTime.UtcNow; // Ghi lại thời điểm heartbeat
    }
    catch (HttpRequestException)
    {
        // Không có mạng - kiểm tra grace period
        var daysSinceLastHeartbeat = (DateTime.UtcNow - LastHeartbeatTime).TotalDays;
        if (daysSinceLastHeartbeat > 30)
            RestrictFeatures(); // Giới hạn tính năng sau 30 ngày offline
    }
}
```

### 5. Lưu trữ token an toàn

```csharp
// Encrypt token bằng HWID-derived key (AES-256)
public static void SaveTokenSecurely(string token, string hwid)
{
    var key = SHA256.HashData(Encoding.UTF8.GetBytes(hwid + "license-salt"));
    using var aes = Aes.Create();
    aes.Key = key;
    aes.GenerateIV();

    using var encryptor = aes.CreateEncryptor();
    var tokenBytes = Encoding.UTF8.GetBytes(token);
    var encrypted = encryptor.TransformFinalBlock(tokenBytes, 0, tokenBytes.Length);

    var combined = new byte[aes.IV.Length + encrypted.Length];
    Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
    Buffer.BlockCopy(encrypted, 0, combined, aes.IV.Length, encrypted.Length);

    File.WriteAllBytes(GetTokenFilePath(), combined);
}

public static string LoadTokenSecurely(string hwid)
{
    var combined = File.ReadAllBytes(GetTokenFilePath());
    var key = SHA256.HashData(Encoding.UTF8.GetBytes(hwid + "license-salt"));

    using var aes = Aes.Create();
    aes.Key = key;
    aes.IV = combined[..16];

    using var decryptor = aes.CreateDecryptor();
    var decrypted = decryptor.TransformFinalBlock(combined, 16, combined.Length - 16);
    return Encoding.UTF8.GetString(decrypted);
}
```

### 6. Bảng mã lỗi và cách xử lý

| HTTP Status | Error | Mô tả | Xử lý |
|-------------|-------|--------|--------|
| 404 | `LICENSE_NOT_FOUND` | Key không tồn tại | Yêu cầu nhập lại key |
| 400 | `LICENSE_EXPIRED` | License đã hết hạn | Yêu cầu gia hạn trên web |
| 400 | `LICENSE_REVOKED` | License bị thu hồi | Thông báo + khóa phần mềm |
| 400 | `LICENSE_SUSPENDED` | License bị tạm dừng | "Liên hệ admin" |
| 400 | `USER_LOCKED` | Tài khoản bị khóa | "Liên hệ admin" |
| 400 | `MAX_ACTIVATIONS` | Vượt giới hạn thiết bị | Yêu cầu hủy kích hoạt thiết bị khác |
| 400 | `INVALID_HARDWARE` | Hardware ID không khớp | Kích hoạt lại trên thiết bị này |
| 400 | `KEY_NOT_REDEEMED` | Key chưa được nhập (Pending) | Nhập key trên web trước |
| 500 | Server Error | Lỗi hệ thống | Retry sau 5-30 giây |

### 7. Luồng khởi động phần mềm (khuyến nghị)

```
┌─────────────────────────────────────┐
│         Khởi động phần mềm         │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│  Có token trong local storage?      │
│  ┌─ KHÔNG → Hiển thị form nhập key │
│  └─ CÓ ↓                           │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│  Verify offline (ECDSA + Exp + Hwid)│
│  ┌─ FAIL → Xóa token, nhập lại key│
│  └─ OK ↓                           │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│  Có kết nối internet?               │
│  ┌─ KHÔNG → Check grace period      │
│  │   ≤ 30 ngày → MỞ KHÓA đầy đủ   │
│  │   > 30 ngày → GIỚI HẠN tính năng│
│  └─ CÓ ↓                           │
└──────────────┬──────────────────────┘
               ▼
┌─────────────────────────────────────┐
│  Gọi Heartbeat                      │
│  → Cập nhật token nếu có           │
│  → Kiểm tra trạng thái mới nhất    │
│  → MỞ KHÓA phần mềm               │
└─────────────────────────────────────┘
```
