# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Bối cảnh

Đồ án tốt nghiệp "Website quản lý cửa hàng thời trang Việt Thắng". Người dùng là sinh viên, trao đổi bằng **tiếng Việt**; comment trong mã nguồn, thông báo UI và tài liệu cũng viết tiếng Việt. Kế hoạch 15 tuần, tiến độ và các quyết định đã chốt nằm trong `docs/README.md` – đọc file đó trước khi bắt đầu một tuần/hạng mục mới và cập nhật bảng "Tiến độ" khi hoàn thành.

## Lệnh thường dùng

```powershell
# Máy mới: kiểm tra .NET/Node/LocalDB, build, npm install (xem docs/06-huong-dan-cai-dat.md)
powershell -ExecutionPolicy Bypass -File .\setup.ps1 [-RunTests] [-SkipNpm]

# Backend (API + Admin Razor) tại http://localhost:5292 – tự migrate + seed khi khởi động
cd src\VietThang.Web
dotnet run
dotnet run -- --seed-demo          # nạp dữ liệu demo một lần (DemoDataSeeder, đánh dấu bằng Settings.DemoDataSeededAt)

# Angular storefront tại http://localhost:4200 (proxy /api, /images, /uploads sang 5292)
cd src\vietthang-web
npm install
npm start                          # ng serve
npm run build

# Build toàn solution (.slnx cần .NET SDK 9.0.200+; máy chỉ có SDK 8 thì build từng csproj)
dotnet build VietThangFashion.slnx
dotnet build src\VietThang.Web\VietThang.Web.csproj

# Unit test (xUnit + EF Core InMemory)
dotnet test tests/VietThang.Tests
dotnet test tests/VietThang.Tests --filter "FullyQualifiedName~PricingServiceTests"
dotnet test tests/VietThang.Tests --filter "Name~Calculate_GiamPhanTram"

# EF Core migration (chạy trong src\VietThang.Web)
dotnet ef migrations add <TenMigration>
dotnet ef migrations script -o ..\..\docs\04b-migration-script.sql   # cập nhật phụ lục SQL

# Sinh ảnh SVG minh họa sản phẩm theo mã + màu (chạy ở thư mục gốc)
node tools/gen-images.mjs

# Cấu hình SMTP thật (user-secrets, không vào git)
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com"
```

CSDL mặc định: SQL Server LocalDB `(localdb)\MSSQLLocalDB`, database `VietThangFashion`. Nếu `dotnet run` báo lỗi kết nối, thử `sqllocaldb start MSSQLLocalDB`.

Tài khoản seed: `admin@thoitrangvietthang.vn / Admin@123` (Admin), `nhanvien@thoitrangvietthang.vn / NhanVien@123` (Employee), `khachhang@gmail.com / KhachHang@123` (Customer).

## Kiến trúc

Hai ứng dụng chạy song song, **một backend .NET 8 duy nhất** `src/VietThang.Web`:

- **Angular 18 storefront** (`src/vietthang-web`) – toàn bộ giao diện khách hàng. Standalone components, signals, lazy `loadComponent`, Reactive Forms, Bootstrap 5. Gọi REST API bằng JWT.
- **REST API** (`Controllers/Api/*ApiController.cs`, route `/api/*`) – kế thừa `ApiControllerBase` (`[ApiController]`, `[IgnoreAntiforgeryToken]`). Endpoint cần đăng nhập dùng `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`; endpoint cho cả khách vãng lai gọi `TryGetJwtUserIdAsync()`. Lỗi validation trả 400 `{ message }`. DTO trong `ViewModels/Api/ApiDtos.cs`.
- **Admin Razor** (`Areas/Admin`, route `/Admin/*`) – kế thừa `AdminBaseController` (`[Authorize(Roles = "Admin,Employee")]`). Controller chỉ dành cho Admin gắn thêm `[Authorize(Roles = SeedData.RoleAdmin)]`. Mọi POST Razor phải có anti-forgery token (bật `AutoValidateAntiforgeryToken` toàn cục).
- Storefront Razor cũ đã bị gỡ (03/10/2026): `HomeController.Index` chỉ redirect sang `Storefront:Url`. Đừng tạo lại view/controller storefront phía server.

**Hai scheme xác thực song song**: cookie Identity (login tại `/Admin/login`) chỉ cho Admin Razor; JWT Bearer (phát hành tại `POST /api/auth/login`, 7 ngày) chỉ cho API. Khóa ký JWT lấy từ `Jwt:Key` hoặc tự sinh vào `App_Data/jwt-key.txt` (`JwtKeyProvider`). CORS whitelist `Cors:Origins`.

**Lớp Services** (`Services/`): interface và implementation nằm **cùng một file** `I<Ten>Service.cs`, đăng ký Scoped trong `Program.cs`, dùng chung cho cả API và Admin. Các nghiệp vụ chính:

- `PricingService` – giá sau khuyến mãi (chọn mức giảm lớn nhất đang hiệu lực).
- `PromotionService` – kiểm tra mã giảm giá.
- `CartService` – `BuildLinesAsync`/`BuildSummaryAsync` tính giá cho giỏ localStorage của khách vãng lai; `Add/Update/Remove` cho giỏ CSDL của khách đăng nhập; gộp giỏ khi đăng nhập qua `POST /api/cart/merge`.
- `OrderService` – đặt hàng trong transaction: trừ tồn, ghi `InventoryTransaction`, tăng lượt dùng mã, lưu snapshot `UnitCost`. Mã đơn `VT + yyMMdd + STT` sinh dưới `sp_getapplock` (phải gọi trong transaction).
- `InventoryService` – duyệt phiếu nhập (cộng tồn, cập nhật `CostPrice`), điều chỉnh kiểm kê; mọi biến động tồn đều ghi `InventoryTransaction` kèm `StockAfter`.
- `CatalogService` – lọc/sắp xếp/phân trang sản phẩm; tìm kiếm không dấu qua cột `Products.SearchText` (sinh bởi `SlugHelper.ToSearchText`, phải cập nhật khi đổi tên/mã/chất liệu).
- `SettingService` – đọc bảng `Settings` có cache 10 phút; gọi `Invalidate()` sau khi Admin lưu cấu hình. Khóa trong `SettingKeys`.
- `IAppEmailSender` – `SmtpEmailSender` (MailKit) khi có `Smtp:Host`, ngược lại `LogEmailSender` ghi log.
- `IFileStorageService` – `CloudinaryFileStorageService` khi có đủ `Cloudinary:CloudName/ApiKey/ApiSecret` (user-secrets), ngược lại `LocalFileStorageService` lưu `wwwroot/uploads`. URL ảnh lưu trong CSDL có thể là tương đối `/uploads/...` hoặc tuyệt đối `https://res.cloudinary.com/...`; Angular và Admin dùng nguyên URL, không nối thêm host.

**Máy trạng thái đơn hàng** `Helpers/OrderStateMachine.cs`: Pending → Confirmed → Shipping → Completed; Pending/Confirmed → Cancelled. Hủy phải hoàn tồn; Completed đặt `PaymentStatus = Paid` và `CompletedAt` (báo cáo doanh thu tính theo mốc này). Mọi chuyển trạng thái phải qua `CanTransition`.

**Dữ liệu**: `Data/ApplicationDbContext.cs` (IdentityDbContext + Fluent API: unique index, check constraint, delete behavior), enum kiểu `byte` trong `Models/Enums/Enums.cs`. `SeedData.SeedAsync` chạy mỗi lần khởi động (migrate, roles, tài khoản, danh mục, sản phẩm mẫu, nâng cấp ảnh theo màu) – viết idempotent. `DemoDataSeeder` chỉ chạy với cờ `--seed-demo`.

**Phân quyền Admin**: Employee dùng được Sản phẩm, Đơn hàng, Kho, Đánh giá, Liên hệ, xem Khách hàng. Chỉ Admin: Danh mục, Màu/Size, Khuyến mãi, Mã giảm giá, Tin tức, Banner, Cửa hàng, Nhân viên, Báo cáo, Cấu hình, khóa khách hàng.

**Angular**: `core/` chứa service (auth, cart, catalog, address), `auth.interceptor.ts` gắn token và bắt 401, `auth.guard.ts` bảo vệ `/account`; `shared/` là component dùng lại; `pages/` theo route trong `app.routes.ts`. Đường dẫn route tiếng Anh, slug dữ liệu tiếng Việt không dấu. Dữ liệu tỉnh/quận/phường tĩnh ở `public/data/vn-address.json`.

## Kiểm thử

`tests/VietThang.Tests` dùng `TestDb.Create()` tạo DbContext InMemory riêng cho mỗi test (đã tắt cảnh báo transaction) và `TestDb.Settings(db)` để lấy `ISettingService`. Test tập trung vào Services/Helpers, không test controller. Thêm test mới theo cùng mẫu khi sửa nghiệp vụ tính giá, mã giảm giá, trạng thái đơn, tồn kho.

## Quy ước

- Khi đổi entity: tạo migration, chạy `dotnet run` để áp dụng, và cân nhắc cập nhật `docs/03-thiet-ke-csdl.md` + `docs/04b-migration-script.sql`.
- Khi thêm endpoint API: thêm DTO vào `ApiDtos.cs`, cập nhật `core/models.ts` và service Angular tương ứng.
- Thư mục `App_Data/`, `wwwroot/uploads/` và `dist/`, `.angular/` không đưa lên git.
- Sơ đồ trong `docs/` viết bằng Mermaid.
