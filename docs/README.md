# Tài liệu đồ án – Website quản lý cửa hàng thời trang Việt Thắng

| File | Nội dung | Dùng cho chương |
|---|---|---|
| `01-de-cuong.md` | Đề cương: lý do, mục tiêu, phạm vi, phương pháp, công nghệ, kế hoạch 15 tuần | Đề cương nộp GVHD, Chương 1 |
| `02-phan-tich-yeu-cau.md` | Khảo sát website hiện tại, tác nhân, 37 chức năng, phi chức năng, use case, đặc tả, activity, sequence | Chương 3 |
| `03-thiet-ke-csdl.md` | ERD 27 bảng, mô tả bảng, bảng mã enum, quy tắc tính giá, index | Chương 4 |
| `04-schema.sql` | Script SQL Server tạo CSDL + dữ liệu khởi tạo | Phụ lục |
| `04b-migration-script.sql` | Script sinh tự động từ EF Core Migration (bản đang chạy thực tế, 34 bảng gồm 7 bảng Identity) | Phụ lục |
| `05-kien-truc-he-thong.md` | Kiến trúc, cấu trúc solution, phân quyền, máy trạng thái đơn, sitemap, báo cáo, bảo mật | Chương 4 |
| `06-huong-dan-cai-dat.md` | Cài đặt trên máy mới: phần mềm cần có, `setup.ps1`, chạy, những gì không theo git, backup/restore, lỗi thường gặp | Phụ lục, bàn giao |

## Giao diện khách hàng bằng Angular (từ 15/09/2026)

Quyết định: **Angular 18 cho storefront (khách hàng), Admin giữ Razor**. Backend bổ sung **REST API + JWT** trong cùng project `VietThang.Web`:

| Thành phần | Vị trí | Ghi chú |
|---|---|---|
| REST API | `src/VietThang.Web/Controllers/Api/*ApiController.cs` | `/api/catalog`, `/api/auth`, `/api/cart`, `/api/checkout`, `/api/account`, `/api/content`; `[ApiController]`, `[IgnoreAntiforgeryToken]`, JSON camelCase |
| JWT | `Services/IJwtTokenService.cs`, cấu hình `Jwt` trong `appsettings.json` | Scheme `Bearer` chỉ dùng cho API; Admin Razor vẫn dùng cookie Identity |
| CORS | `Cors:Origins` trong `appsettings.json` | Mặc định `http://localhost:4200` |
| Angular app | `src/vietthang-web/` | Standalone components, signals, lazy routes, Reactive Forms, HttpClient + interceptor JWT, guard `/tai-khoan` |
| Proxy dev | `src/vietthang-web/proxy.conf.json` | Chuyển `/api`, `/images`, `/uploads` sang `http://localhost:5292` |

Giỏ hàng khách vãng lai lưu `localStorage`, tính giá qua `POST /api/cart/quote`; khi đăng nhập, giỏ được gộp lên CSDL qua `POST /api/cart/merge`. Đặt hàng: `POST /api/checkout` (khách vãng lai gửi `items`, khách đăng nhập dùng giỏ CSDL).

Chạy song song hai server:

```bash
# Terminal 1 – backend (API + Admin Razor) tại http://localhost:5292
cd src/VietThang.Web && dotnet run

# Terminal 2 – Angular storefront tại http://localhost:4200
cd src/vietthang-web && npm start
```

Từ 03/10/2026 storefront Razor cũ đã được gỡ: backend chỉ còn REST API (`/api/*`) và Admin (`/Admin`); truy cập `http://localhost:5292/` sẽ chuyển sang địa chỉ Angular cấu hình ở `Storefront:Url`.

## Hoàn thiện giai đoạn 1 (03/10/2026)

| Hạng mục | Cách làm | Vị trí |
|---|---|---|
| Quên mật khẩu | `POST /api/auth/forgot-password` gửi email chứa liên kết `/reset-password?email=&token=` (token Identity, 24 giờ); `POST /api/auth/reset-password` đặt mật khẩu mới. Luôn trả cùng một thông báo để không lộ email đã đăng ký | `Controllers/Api/AuthApiController.cs`, Angular `pages/auth/forgot-password`, `reset-password` |
| Email thật | `SmtpEmailSender` (MailKit) dùng khi có `Smtp:Host`; chưa cấu hình thì `LogEmailSender` ghi nội dung email ra console | `Services/IAppEmailSender.cs` |
| Tìm kiếm không dấu | Cột `Products.SearchText` lưu tên + mã + chất liệu dạng không dấu, chữ thường; từ khóa cũng được chuẩn hóa rồi so khớp từng từ ("bo lanh" khớp "Bộ lanh nữ") | `SlugHelper.ToSearchText`, `CatalogService.ApplyFilter` |
| Giá vốn snapshot | Cột `OrderDetails.UnitCost` lưu giá vốn lúc đặt hàng; báo cáo lợi nhuận gộp = Σ(UnitPrice − UnitCost) × SL | `OrderService`, `ReportsController` |
| Chống trùng mã đơn | `sp_getapplock` (khóa độc quyền mức transaction) trước khi sinh mã `VT + yyMMdd + STT`; đã thử 6 đơn đồng thời | `OrderService.GenerateOrderCodeAsync` |
| Khóa JWT ngoài mã nguồn | Ưu tiên cấu hình `Jwt:Key` (biến môi trường / user-secrets); nếu không có thì tự sinh `App_Data/jwt-key.txt` (đã `.gitignore`) | `Services/JwtKeyProvider.cs` |
| Chọn tỉnh / quận / phường | Ba ô chọn liên cấp dùng file tĩnh `public/data/vn-address.json` (63 tỉnh, 696 quận/huyện, 10.051 phường/xã; nguồn provinces.open-api.vn, cấu trúc 3 cấp trước 01/07/2025) | Angular `shared/address-select.component.ts`, `core/address.service.ts` |

Cấu hình gửi email thật bằng Gmail (mật khẩu là *App password* 16 ký tự, không phải mật khẩu đăng nhập; lưu bằng user-secrets nên không vào git):

```bash
cd src/VietThang.Web
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Smtp:User" "dia-chi@gmail.com"
dotnet user-secrets set "Smtp:Password" "xxxx xxxx xxxx xxxx"
```

Hạn chế đã biết: danh mục địa giới là bộ 3 cấp cũ; sau sắp xếp đơn vị hành chính 01/07/2025 (34 tỉnh, bỏ cấp huyện) có thể thay file `vn-address.json` bằng bộ 2 cấp mới và cho phép bỏ trống quận/huyện.

## Dữ liệu demo (03/10/2026)

```bash
cd src/VietThang.Web
dotnet run -- --seed-demo
```

`Data/DemoDataSeeder.cs` chạy một lần (đánh dấu bằng dòng `Settings.DemoDataSeededAt`), sinh theo hạt giống cố định và tính ngày lùi từ hôm chạy:

| Dữ liệu | Số lượng | Ghi chú |
|---|---|---|
| Sản phẩm bổ sung | 10 (tổng 20, 281 biến thể) | Có ảnh theo màu, phủ đủ các danh mục con |
| Khách hàng | 15 | `lan.nguyen@example.com` … mật khẩu `KhachHang@123`, có địa chỉ mặc định |
| Phiếu nhập | 4 | Ngày −86, −62, −38, −14; cộng tồn và ghi nhật ký kho |
| Đơn hàng | 96 | Trải 90 ngày, tăng dần về gần hiện tại; đủ 5 trạng thái, COD và chuyển khoản, có đơn dùng mã giảm giá, đơn hủy đã hoàn tồn |
| Đánh giá | 43 | Của khách có đơn hoàn thành; một số còn chờ duyệt |
| Yêu thích, liên hệ | 30, 4 | |

Mô phỏng theo dòng thời gian nên tồn kho, `StockAfter` trong nhật ký kho, số đã bán và lượt dùng mã đều nhất quán.

## Kiểm thử đơn vị (03/10/2026)

```bash
dotnet test tests/VietThang.Tests
```

Project `tests/VietThang.Tests` (xUnit, EF Core InMemory), **93 test, đạt 100%**:

| Lớp test | Nội dung |
|---|---|
| `PricingServiceTests` | Giảm %, giảm số tiền, chọn mức giảm lớn nhất, không âm, chỉ lấy khuyến mãi đang hiệu lực |
| `PromotionServiceTests` | Mã không tồn tại, đã tắt, chưa đến ngày, hết hạn, hết lượt, chưa đạt đơn tối thiểu, mức giảm tối đa, không phân biệt hoa thường |
| `OrderStateMachineTests` | 5 chuyển trạng thái hợp lệ, 11 chuyển không hợp lệ, trạng thái cuối, tên hiển thị |
| `CartSummaryTests` | Phí ship, ngưỡng freeship (đúng bằng ngưỡng), đổi cấu hình, mã giảm giá, giỏ rỗng, kiểm tra còn hàng |
| `InventoryServiceTests` | Điều chỉnh kiểm kê tăng/giảm/không đổi/số âm; duyệt phiếu nhập cộng tồn, cập nhật giá vốn, chặn duyệt lại |
| `SlugHelperTests` | Slug, mã SKU, chuỗi tìm kiếm không dấu, chữ đ |

## Ảnh sản phẩm mẫu

`tools/gen-images.mjs` (chạy `node tools/gen-images.mjs` ở thư mục gốc) sinh 31 ảnh SVG minh họa theo mã sản phẩm + màu (`/images/products/{MÃ}-{MÀU}.svg`) và 4 ảnh nhóm danh mục. `SeedData.UpgradeImagesAsync` chạy mỗi lần khởi động: sản phẩm chỉ có ảnh placeholder sẽ được gắn ảnh theo từng màu (gallery đổi ảnh khi chọn màu), sản phẩm đã có ảnh thật tải lên qua Admin được giữ nguyên. Ảnh thật của cửa hàng thêm qua Admin → Sản phẩm → Sửa → Thêm ảnh (chọn màu tương ứng).

## Chạy dự án

Máy mới: clone về rồi chạy `powershell -ExecutionPolicy Bypass -File .\setup.ps1` (kiểm tra .NET/Node/LocalDB, build, `npm install`). Chi tiết và lỗi thường gặp: `06-huong-dan-cai-dat.md`.

```powershell
cd src\VietThang.Web
dotnet run
```

Ứng dụng tự chạy Migration và seed dữ liệu khi khởi động. CSDL: `(localdb)\MSSQLLocalDB`, database `VietThangFashion` (đổi trong `appsettings.json` nếu dùng SQL Server khác). Tạo migration mới: `dotnet ef migrations add <Ten>` trong thư mục `src\VietThang.Web`.

Tài khoản mẫu: `admin@thoitrangvietthang.vn / Admin@123`, `nhanvien@thoitrangvietthang.vn / NhanVien@123`, `khachhang@gmail.com / KhachHang@123`.

Sơ đồ viết bằng Mermaid: xem trực tiếp trên GitHub/VS Code (extension *Markdown Preview Mermaid Support*), hoặc dán vào https://mermaid.live để xuất PNG đưa vào Word.

## Tiến độ

| Tuần | Nội dung | Trạng thái |
|---|---|---|
| 1–3 | Đề cương, phân tích, thiết kế CSDL | ✅ Xong (14/09/2026) |
| 4 | Khởi tạo solution, EF Core, Identity, Migration, seed | ✅ Xong (14/09/2026) |
| 5 | Đăng nhập/đăng ký, layout Admin, Danh mục, Màu, Size, Sản phẩm + ảnh + biến thể | ✅ Xong (14/09/2026) |
| 7 | Storefront: trang chủ, menu danh mục, danh mục + lọc (giá, màu, size, chất liệu, sắp xếp), tìm kiếm, hàng mới, sale, chi tiết sản phẩm chọn màu/size, tin tức, tuyển dụng, cửa hàng, liên hệ | ✅ Xong (15/09/2026) |
| 8 | Giỏ hàng (session cho khách vãng lai, CSDL cho khách đăng nhập, gộp khi đăng nhập), mã giảm giá, phí ship/freeship, đặt hàng COD/chuyển khoản trong transaction (trừ tồn, nhật ký kho, lượt dùng mã), trang cảm ơn, tra cứu đơn theo mã + SĐT | ✅ Xong (15/09/2026) |
| – | Trang đăng nhập riêng cho quản trị `/Admin/login` (từ chối tài khoản khách), tự chuyển hướng khi vào `/Admin/*` chưa đăng nhập | ✅ Xong (15/09/2026) |
| 6 | Nhà cung cấp (CRUD), phiếu nhập (nháp → thêm dòng theo biến thể → duyệt cộng tồn + cập nhật giá vốn), tồn kho theo biến thể (lọc, cảnh báo sắp hết, giá trị tồn), điều chỉnh kiểm kê, lịch sử xuất nhập | ✅ Xong (15/09/2026) |
| 9 | Admin xử lý đơn: danh sách theo trạng thái, chi tiết, chuyển trạng thái theo máy trạng thái (hủy → hoàn tồn, hoàn thành → đã thanh toán), xác nhận chuyển khoản, in phiếu giao; Dashboard có đơn gần đây + sắp hết hàng. Khách: hồ sơ, đổi mật khẩu, sổ địa chỉ, lịch sử đơn, hủy đơn chờ xác nhận, đánh giá sản phẩm đã mua (chờ duyệt), yêu thích | ✅ Xong (15/09/2026) |
| 10 | Admin: khuyến mãi theo sản phẩm (chọn nhiều SP có lọc), mã giảm giá, duyệt/ẩn đánh giá, tin tức & tuyển dụng (TinyMCE, ảnh), banner, liên hệ (đánh dấu đã xử lý), hệ thống cửa hàng | ✅ Xong (15/09/2026) |
| 11 | Admin: khách hàng (tìm, chi tiết, số đơn, chi tiêu, khóa/mở), nhân viên & phân quyền (tạo tài khoản Admin/Employee, đổi vai trò, đặt lại mật khẩu, khóa; chống tự khóa), cấu hình hệ thống (tên, hotline, phí ship, freeship, ngưỡng tồn, tài khoản ngân hàng) | ✅ Xong (15/09/2026) |
| 12 | Báo cáo (Chart.js): doanh thu theo ngày/tháng, đơn theo trạng thái, top 10 bán chạy, doanh thu theo nhóm hàng, lợi nhuận gộp theo tháng, khách mới; thẻ tổng hợp; xuất Excel đơn hàng + chi tiết (ClosedXML) | ✅ Xong (15/09/2026) |
| GĐ1 | Gỡ storefront Razor, quên mật khẩu + SMTP, tìm kiếm không dấu, giá vốn snapshot, chống trùng mã đơn, khóa JWT ngoài mã nguồn, chọn tỉnh/quận/phường | ✅ Xong (03/10/2026) |
| 13a | Dữ liệu demo (`--seed-demo`), 93 unit test xUnit | ✅ Xong (03/10/2026) |
| GĐ3 | Lưu ảnh tải lên trên Cloudinary (`CloudinaryFileStorageService`, bật qua user-secrets, tự quay về lưu đĩa khi chưa cấu hình); `setup.ps1` + `06-huong-dan-cai-dat.md` để chuyển dự án sang máy khác; 104 unit test | ✅ Xong (17/10/2026) |
| 13b | Bảng test case chức năng, rà responsive Angular, ảnh sản phẩm thật | ⏳ Tiếp theo |
| 14–15 | Viết báo cáo, slide, bảo vệ | |

Đường dẫn storefront (Angular, cổng 4200, đổi sang tiếng Anh từ 03/10/2026): `/`, `/category/{slug}`, `/search?q=`, `/new-arrivals`, `/sale`, `/product/{slug}`, `/cart`, `/checkout`, `/checkout/success/{code}`, `/order-lookup`, `/news`, `/news/{slug}`, `/careers`, `/stores`, `/contact`, `/login`, `/register`, `/forgot-password`, `/reset-password`.
Đường dẫn tài khoản khách: `/account`, `/account/addresses`, `/account/orders`, `/account/orders/{code}`, `/account/wishlist`.
Quản trị: đăng nhập `/Admin/login`, đăng xuất `/Admin/logout`, từ chối quyền `/Admin/access-denied`. Slug của danh mục, sản phẩm, bài viết vẫn là tiếng Việt không dấu vì lấy từ dữ liệu.
Đường dẫn quản trị: `/Admin/login`, `/Admin` (Dashboard), `/Admin/Products`, `/Admin/Products/Variants/{id}`, `/Admin/Categories`, `/Admin/Colors`, `/Admin/Sizes`, `/Admin/Orders`, `/Admin/Orders/Details/{id}`, `/Admin/Orders/Print/{id}`, `/Admin/Suppliers`, `/Admin/GoodsReceipts`, `/Admin/GoodsReceipts/Edit/{id}`, `/Admin/Inventory`, `/Admin/Inventory/History`, `/Admin/Promotions`, `/Admin/Coupons`, `/Admin/Reviews`, `/Admin/Posts`, `/Admin/Banners`, `/Admin/Contacts`, `/Admin/Stores`, `/Admin/Customers`, `/Admin/Employees`, `/Admin/Settings`, `/Admin/Reports`, `/Admin/Reports/Export?from=&to=`.

Phân quyền Admin: Employee dùng được Sản phẩm, Đơn hàng, Kho, Đánh giá, Liên hệ, xem Khách hàng. Chỉ Admin: Danh mục, Màu/Size, Khuyến mãi, Mã giảm giá, Tin tức, Banner, Cửa hàng, Nhân viên, Báo cáo, Cấu hình, khóa khách hàng.

Ghi chú báo cáo: doanh thu tính theo đơn **Hoàn thành** tại mốc `CompletedAt`; lợi nhuận gộp = Σ(UnitPrice − CostPrice hiện tại của biến thể) × SL (giá vốn không lưu snapshot theo đơn, nêu trong phần hạn chế của báo cáo).

Dịch vụ nghiệp vụ (`Services/`): `PricingService` (giá khuyến mãi), `PromotionService` (mã giảm giá), `CartService`, `OrderService` (đặt hàng trong transaction), `CatalogService` (lọc/sắp xếp/thẻ sản phẩm), `SettingService` (cấu hình có cache), `IFileStorageService` (`LocalFileStorageService` lưu `wwwroot/uploads`, `CloudinaryFileStorageService` khi có khóa Cloudinary), `IAppEmailSender` (`SmtpEmailSender` khi có `Smtp:Host`, ngược lại `LogEmailSender`).

## Quyết định đã chốt
- ASP.NET Core MVC (.NET 8 LTS) + EF Core + Identity + SQL Server.
- Một ứng dụng, Storefront + Area Admin.
- Thanh toán COD / chuyển khoản (không tích hợp cổng online).
- Có quản lý kho, nhập hàng, báo cáo biểu đồ (Chart.js).
