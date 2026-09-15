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
| 7 | Storefront: trang chủ, menu danh mục, danh mục + lọc (giá, màu, size, chất liệu, sắp xếp), tìm kiếm, hàng mới, sale, chi tiết sản phẩm chọn màu/size, tin tức, tuyển dụng, cửa hàng, liên hệ | ✅ Xong (15/09/2026) |
| 8 | Giỏ hàng (session cho khách vãng lai, CSDL cho khách đăng nhập, gộp khi đăng nhập), mã giảm giá, phí ship/freeship, đặt hàng COD/chuyển khoản trong transaction (trừ tồn, nhật ký kho, lượt dùng mã), trang cảm ơn, tra cứu đơn theo mã + SĐT | ✅ Xong (15/09/2026) |
| – | Trang đăng nhập riêng cho quản trị `/Admin/dang-nhap` (từ chối tài khoản khách), tự chuyển hướng khi vào `/Admin/*` chưa đăng nhập | ✅ Xong (15/09/2026) |
| 6 | Nhà cung cấp (CRUD), phiếu nhập (nháp → thêm dòng theo biến thể → duyệt cộng tồn + cập nhật giá vốn), tồn kho theo biến thể (lọc, cảnh báo sắp hết, giá trị tồn), điều chỉnh kiểm kê, lịch sử xuất nhập | ✅ Xong (15/09/2026) |
| 9 | Admin xử lý đơn: danh sách theo trạng thái, chi tiết, chuyển trạng thái theo máy trạng thái (hủy → hoàn tồn, hoàn thành → đã thanh toán), xác nhận chuyển khoản, in phiếu giao; Dashboard có đơn gần đây + sắp hết hàng. Khách: hồ sơ, đổi mật khẩu, sổ địa chỉ, lịch sử đơn, hủy đơn chờ xác nhận, đánh giá sản phẩm đã mua (chờ duyệt), yêu thích | ✅ Xong (15/09/2026) |
| 10 | Admin: khuyến mãi, mã giảm giá, duyệt đánh giá, tin tức, banner, liên hệ, cửa hàng | ⏳ Tiếp theo |
| 11 | Admin: khách hàng, nhân viên, phân quyền, cấu hình | |
| 12 | Báo cáo thống kê biểu đồ, xuất Excel | |

Đường dẫn storefront: `/`, `/danh-muc/{slug}`, `/tim-kiem?q=`, `/hang-moi-ve`, `/sale`, `/san-pham/{slug}`, `/gio-hang`, `/thanh-toan`, `/tra-cuu-don-hang`, `/tin-tuc`, `/tuyen-dung`, `/he-thong-cua-hang`, `/lien-he`, `/dang-nhap`, `/dang-ky`.
Đường dẫn tài khoản khách: `/tai-khoan`, `/tai-khoan/doi-mat-khau`, `/tai-khoan/dia-chi`, `/tai-khoan/don-hang`, `/tai-khoan/don-hang/{code}`, `/tai-khoan/yeu-thich`.
Đường dẫn quản trị: `/Admin/dang-nhap`, `/Admin` (Dashboard), `/Admin/Products`, `/Admin/Products/Variants/{id}`, `/Admin/Categories`, `/Admin/Colors`, `/Admin/Sizes`, `/Admin/Orders`, `/Admin/Orders/Details/{id}`, `/Admin/Orders/Print/{id}`, `/Admin/Suppliers`, `/Admin/GoodsReceipts`, `/Admin/GoodsReceipts/Edit/{id}`, `/Admin/Inventory`, `/Admin/Inventory/History`.

Dịch vụ nghiệp vụ (`Services/`): `PricingService` (giá khuyến mãi), `PromotionService` (mã giảm giá), `CartService`, `OrderService` (đặt hàng trong transaction), `CatalogService` (lọc/sắp xếp/thẻ sản phẩm), `SettingService` (cấu hình có cache), `FileStorageService`, `LogEmailSender` (email ghi log, thay bằng SMTP ở tuần 13).

## Quyết định đã chốt
- ASP.NET Core MVC (.NET 8 LTS) + EF Core + Identity + SQL Server.
- Một ứng dụng, Storefront + Area Admin.
- Thanh toán COD / chuyển khoản (không tích hợp cổng online).
- Có quản lý kho, nhập hàng, báo cáo biểu đồ (Chart.js).
