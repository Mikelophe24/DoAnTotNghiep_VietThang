# Tài liệu đồ án – Website quản lý cửa hàng thời trang Việt Thắng

| File | Nội dung | Dùng cho chương |
|---|---|---|
| `01-de-cuong.md` | Đề cương: lý do, mục tiêu, phạm vi, phương pháp, công nghệ, kế hoạch 15 tuần | Đề cương nộp GVHD, Chương 1 |
| `02-phan-tich-yeu-cau.md` | Khảo sát website hiện tại, tác nhân, 37 chức năng, phi chức năng, use case, đặc tả, activity, sequence | Chương 3 |
| `03-thiet-ke-csdl.md` | ERD 27 bảng, mô tả bảng, bảng mã enum, quy tắc tính giá, index | Chương 4 |
| `04-schema.sql` | Script SQL Server tạo CSDL + dữ liệu khởi tạo | Phụ lục |
| `04b-migration-script.sql` | Script sinh tự động từ EF Core Migration (bản đang chạy thực tế, 34 bảng gồm 7 bảng Identity) | Phụ lục |
| `05-kien-truc-he-thong.md` | Kiến trúc, cấu trúc solution, phân quyền, máy trạng thái đơn, sitemap, báo cáo, bảo mật | Chương 4 |

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
| 6 | Nhà cung cấp, phiếu nhập, tồn kho, lịch sử kho | ⏳ Tiếp theo |
| 7–8 | Storefront: trang chủ, danh mục, chi tiết, giỏ hàng, đặt hàng | |
| 9–12 | Đơn hàng, khuyến mãi, người dùng, báo cáo | |

Đường dẫn chính: `/dang-nhap`, `/dang-ky`, `/Admin` (Dashboard), `/Admin/Products`, `/Admin/Products/Variants/{id}`, `/Admin/Categories`, `/Admin/Colors`, `/Admin/Sizes`.

## Quyết định đã chốt
- ASP.NET Core MVC (.NET 8 LTS) + EF Core + Identity + SQL Server.
- Một ứng dụng, Storefront + Area Admin.
- Thanh toán COD / chuyển khoản (không tích hợp cổng online).
- Có quản lý kho, nhập hàng, báo cáo biểu đồ (Chart.js).
