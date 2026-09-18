# Tài liệu đồ án – Website quản lý cửa hàng thời trang Việt Thắng

| File | Nội dung | Dùng cho chương |
|---|---|---|
| `01-de-cuong.md` | Đề cương: lý do, mục tiêu, phạm vi, phương pháp, công nghệ, kế hoạch 15 tuần | Đề cương nộp GVHD, Chương 1 |
| `02-phan-tich-yeu-cau.md` | Khảo sát website hiện tại, tác nhân, 37 chức năng, phi chức năng, use case, đặc tả, activity, sequence | Chương 3 |
| `03-thiet-ke-csdl.md` | ERD 27 bảng, mô tả bảng, bảng mã enum, quy tắc tính giá, index | Chương 4 |
| `04-schema.sql` | Script SQL Server tạo CSDL + dữ liệu khởi tạo | Phụ lục |
| `04b-migration-script.sql` | Script sinh tự động từ EF Core Migration (bản đang chạy thực tế, 34 bảng gồm 7 bảng Identity) | Phụ lục |
| `05-kien-truc-he-thong.md` | Kiến trúc, cấu trúc solution, phân quyền, máy trạng thái đơn, sitemap, báo cáo, bảo mật | Chương 4 |

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

Storefront Razor cũ (`/`, `/danh-muc/...` trên cổng 5292) vẫn chạy và có thể dùng làm bản so sánh; Admin ở `http://localhost:5292/Admin`.

## Ảnh sản phẩm mẫu

`tools/gen-images.mjs` (chạy `node tools/gen-images.mjs` ở thư mục gốc) sinh 31 ảnh SVG minh họa theo mã sản phẩm + màu (`/images/products/{MÃ}-{MÀU}.svg`) và 4 ảnh nhóm danh mục. `SeedData.UpgradeImagesAsync` chạy mỗi lần khởi động: sản phẩm chỉ có ảnh placeholder sẽ được gắn ảnh theo từng màu (gallery đổi ảnh khi chọn màu), sản phẩm đã có ảnh thật tải lên qua Admin được giữ nguyên. Ảnh thật của cửa hàng thêm qua Admin → Sản phẩm → Sửa → Thêm ảnh (chọn màu tương ứng).

## Chạy dự án

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
| – | Trang đăng nhập riêng cho quản trị `/Admin/dang-nhap` (từ chối tài khoản khách), tự chuyển hướng khi vào `/Admin/*` chưa đăng nhập | ✅ Xong (15/09/2026) |
| 6 | Nhà cung cấp (CRUD), phiếu nhập (nháp → thêm dòng theo biến thể → duyệt cộng tồn + cập nhật giá vốn), tồn kho theo biến thể (lọc, cảnh báo sắp hết, giá trị tồn), điều chỉnh kiểm kê, lịch sử xuất nhập | ✅ Xong (15/09/2026) |
| 9 | Admin xử lý đơn: danh sách theo trạng thái, chi tiết, chuyển trạng thái theo máy trạng thái (hủy → hoàn tồn, hoàn thành → đã thanh toán), xác nhận chuyển khoản, in phiếu giao; Dashboard có đơn gần đây + sắp hết hàng. Khách: hồ sơ, đổi mật khẩu, sổ địa chỉ, lịch sử đơn, hủy đơn chờ xác nhận, đánh giá sản phẩm đã mua (chờ duyệt), yêu thích | ✅ Xong (15/09/2026) |
| 10 | Admin: khuyến mãi theo sản phẩm (chọn nhiều SP có lọc), mã giảm giá, duyệt/ẩn đánh giá, tin tức & tuyển dụng (TinyMCE, ảnh), banner, liên hệ (đánh dấu đã xử lý), hệ thống cửa hàng | ✅ Xong (15/09/2026) |
| 11 | Admin: khách hàng (tìm, chi tiết, số đơn, chi tiêu, khóa/mở), nhân viên & phân quyền (tạo tài khoản Admin/Employee, đổi vai trò, đặt lại mật khẩu, khóa; chống tự khóa), cấu hình hệ thống (tên, hotline, phí ship, freeship, ngưỡng tồn, tài khoản ngân hàng) | ✅ Xong (15/09/2026) |
| 12 | Báo cáo (Chart.js): doanh thu theo ngày/tháng, đơn theo trạng thái, top 10 bán chạy, doanh thu theo nhóm hàng, lợi nhuận gộp theo tháng, khách mới; thẻ tổng hợp; xuất Excel đơn hàng + chi tiết (ClosedXML) | ✅ Xong (15/09/2026) |
| 13 | Kiểm thử, sửa lỗi, responsive, email SMTP, test case | ⏳ Tiếp theo |
| 14–15 | Viết báo cáo, slide, bảo vệ | |

Đường dẫn storefront: `/`, `/danh-muc/{slug}`, `/tim-kiem?q=`, `/hang-moi-ve`, `/sale`, `/san-pham/{slug}`, `/gio-hang`, `/thanh-toan`, `/tra-cuu-don-hang`, `/tin-tuc`, `/tuyen-dung`, `/he-thong-cua-hang`, `/lien-he`, `/dang-nhap`, `/dang-ky`.
Đường dẫn tài khoản khách: `/tai-khoan`, `/tai-khoan/doi-mat-khau`, `/tai-khoan/dia-chi`, `/tai-khoan/don-hang`, `/tai-khoan/don-hang/{code}`, `/tai-khoan/yeu-thich`.
Đường dẫn quản trị: `/Admin/dang-nhap`, `/Admin` (Dashboard), `/Admin/Products`, `/Admin/Products/Variants/{id}`, `/Admin/Categories`, `/Admin/Colors`, `/Admin/Sizes`, `/Admin/Orders`, `/Admin/Orders/Details/{id}`, `/Admin/Orders/Print/{id}`, `/Admin/Suppliers`, `/Admin/GoodsReceipts`, `/Admin/GoodsReceipts/Edit/{id}`, `/Admin/Inventory`, `/Admin/Inventory/History`, `/Admin/Promotions`, `/Admin/Coupons`, `/Admin/Reviews`, `/Admin/Posts`, `/Admin/Banners`, `/Admin/Contacts`, `/Admin/Stores`, `/Admin/Customers`, `/Admin/Employees`, `/Admin/Settings`, `/Admin/Reports`, `/Admin/Reports/Export?from=&to=`.

Phân quyền Admin: Employee dùng được Sản phẩm, Đơn hàng, Kho, Đánh giá, Liên hệ, xem Khách hàng. Chỉ Admin: Danh mục, Màu/Size, Khuyến mãi, Mã giảm giá, Tin tức, Banner, Cửa hàng, Nhân viên, Báo cáo, Cấu hình, khóa khách hàng.

Ghi chú báo cáo: doanh thu tính theo đơn **Hoàn thành** tại mốc `CompletedAt`; lợi nhuận gộp = Σ(UnitPrice − CostPrice hiện tại của biến thể) × SL (giá vốn không lưu snapshot theo đơn, nêu trong phần hạn chế của báo cáo).

Dịch vụ nghiệp vụ (`Services/`): `PricingService` (giá khuyến mãi), `PromotionService` (mã giảm giá), `CartService`, `OrderService` (đặt hàng trong transaction), `CatalogService` (lọc/sắp xếp/thẻ sản phẩm), `SettingService` (cấu hình có cache), `FileStorageService`, `LogEmailSender` (email ghi log, thay bằng SMTP ở tuần 13).

## Quyết định đã chốt
- ASP.NET Core MVC (.NET 8 LTS) + EF Core + Identity + SQL Server.
- Một ứng dụng, Storefront + Area Admin.
- Thanh toán COD / chuyển khoản (không tích hợp cổng online).
- Có quản lý kho, nhập hàng, báo cáo biểu đồ (Chart.js).
