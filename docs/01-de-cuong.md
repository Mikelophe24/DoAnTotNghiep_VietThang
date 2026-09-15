# ĐỀ CƯƠNG ĐỒ ÁN TỐT NGHIỆP

**Tên đề tài:** Xây dựng website quản lý cửa hàng thời trang Việt Thắng

**Công nghệ:** ASP.NET Core MVC (.NET 8), Entity Framework Core, ASP.NET Core Identity, SQL Server, Bootstrap 5, Chart.js

---

## 1. Lý do chọn đề tài

Cửa hàng Thời trang Việt Thắng (thương hiệu "Thời trang gia đình VT") là chuỗi cửa hàng chuyên kinh doanh đồ mặc nhà, đồ bộ, đồ ngủ cho nữ, trung niên, trẻ em và nam. Hiện cửa hàng đang bán hàng qua website thuê trên nền tảng Nhanh.vn (https://thoitrangvietthang.vn/). Việc phụ thuộc nền tảng thuê ngoài có một số hạn chế:

- Chi phí thuê hàng tháng, khó tùy biến giao diện và nghiệp vụ riêng.
- Dữ liệu sản phẩm, đơn hàng, khách hàng nằm trên hệ thống bên thứ ba.
- Quy trình nhập hàng, quản lý tồn kho theo từng biến thể màu/size và báo cáo doanh thu chưa gắn liền với kênh bán online.

Đề tài hướng tới xây dựng một hệ thống website riêng, gồm **trang bán hàng cho khách** và **trang quản trị nội bộ**, đáp ứng đầy đủ nghiệp vụ của cửa hàng: quản lý sản phẩm theo biến thể, bán hàng online, quản lý đơn hàng, nhập hàng từ nhà cung cấp, theo dõi tồn kho và thống kê báo cáo bằng biểu đồ.

## 2. Mục tiêu đề tài

### 2.1. Mục tiêu tổng quát
Xây dựng website hoàn chỉnh cho cửa hàng thời trang Việt Thắng, hỗ trợ khách hàng mua sắm trực tuyến và hỗ trợ chủ cửa hàng/nhân viên quản lý toàn bộ hoạt động kinh doanh.

### 2.2. Mục tiêu cụ thể
1. Khảo sát, phân tích nghiệp vụ và website hiện tại của cửa hàng.
2. Phân tích, thiết kế hệ thống: sơ đồ use case, sơ đồ hoạt động, sơ đồ tuần tự, thiết kế cơ sở dữ liệu.
3. Xây dựng website bán hàng: danh mục nhiều cấp, tìm kiếm/lọc, chi tiết sản phẩm theo màu và size, giỏ hàng, đặt hàng, theo dõi đơn, đánh giá sản phẩm, tin tức.
4. Xây dựng trang quản trị: quản lý sản phẩm và biến thể, danh mục, đơn hàng, khách hàng, nhân viên, khuyến mãi và mã giảm giá, nhà cung cấp, phiếu nhập hàng, tồn kho, tin tức, banner.
5. Xây dựng module thống kê báo cáo: doanh thu theo ngày/tháng/năm, sản phẩm bán chạy, tồn kho, đơn hàng theo trạng thái, hiển thị bằng biểu đồ.
6. Kiểm thử và triển khai hệ thống.

## 3. Đối tượng và phạm vi nghiên cứu

### 3.1. Đối tượng
- Quy trình bán hàng online và quản lý nội bộ của cửa hàng thời trang Việt Thắng.
- Công nghệ ASP.NET Core MVC, Entity Framework Core, SQL Server.

### 3.2. Phạm vi
**Trong phạm vi:**
- Website bán hàng (front-end khách hàng) và trang quản trị (Admin Area) trên cùng một ứng dụng ASP.NET Core MVC.
- Thanh toán: COD (thanh toán khi nhận hàng) và chuyển khoản ngân hàng (xác nhận thủ công).
- Quản lý kho: phiếu nhập hàng, nhà cung cấp, tồn kho theo biến thể, lịch sử xuất nhập.
- Báo cáo thống kê có biểu đồ.

**Ngoài phạm vi:**
- Tích hợp cổng thanh toán online (VNPay, MoMo), tích hợp đơn vị vận chuyển.
- Ứng dụng di động, bán hàng đa kênh (Shopee, Facebook).
- Hệ thống POS bán tại quầy nhiều chi nhánh.

## 4. Phương pháp thực hiện
1. **Khảo sát:** phân tích website hiện tại, danh mục sản phẩm, quy trình bán hàng; tham khảo các website thời trang tương tự.
2. **Phân tích và thiết kế:** mô hình hóa bằng UML (use case, activity, sequence, class), thiết kế CSDL quan hệ (ERD, chuẩn hóa 3NF).
3. **Xây dựng:** mô hình MVC, Repository/Service pattern, EF Core Code First với Migration, Identity phân quyền theo vai trò.
4. **Kiểm thử:** kiểm thử chức năng theo kịch bản (test case), kiểm thử giao diện trên nhiều kích thước màn hình.
5. **Triển khai:** IIS hoặc Azure App Service + SQL Server.

## 5. Công nghệ sử dụng

| Thành phần | Công nghệ | Ghi chú |
|---|---|---|
| Ngôn ngữ | C# 12 | |
| Framework | ASP.NET Core MVC 8.0 (LTS) | Razor Views, Areas |
| ORM | Entity Framework Core 8 | Code First, Migrations |
| Xác thực/phân quyền | ASP.NET Core Identity | Roles: Admin, Employee, Customer |
| CSDL | SQL Server 2019/2022 | |
| Giao diện | HTML5, CSS3, Bootstrap 5, jQuery | Responsive |
| Biểu đồ | Chart.js | Trang thống kê |
| Soạn thảo nội dung | TinyMCE / CKEditor | Mô tả sản phẩm, tin tức |
| Công cụ | Visual Studio 2022, SSMS, Git | |

## 6. Kết quả dự kiến
- Báo cáo đồ án đầy đủ (khảo sát, phân tích, thiết kế, cài đặt, kiểm thử).
- Mã nguồn website hoàn chỉnh chạy được, có dữ liệu mẫu thực tế theo cửa hàng Việt Thắng.
- Script CSDL SQL Server, tài liệu hướng dẫn cài đặt.
- Slide bảo vệ và demo trực tiếp.

## 7. Bố cục dự kiến của báo cáo

- **Chương 1. Tổng quan đề tài** – lý do, mục tiêu, phạm vi, phương pháp, công nghệ.
- **Chương 2. Cơ sở lý thuyết** – ASP.NET Core MVC, EF Core, Identity, SQL Server, mô hình MVC, RESTful, bảo mật web.
- **Chương 3. Khảo sát và phân tích hệ thống** – khảo sát cửa hàng và website hiện tại, tác nhân, yêu cầu chức năng/phi chức năng, sơ đồ use case, đặc tả use case, sơ đồ hoạt động, sơ đồ tuần tự.
- **Chương 4. Thiết kế hệ thống** – kiến trúc, thiết kế CSDL (ERD, mô tả bảng), thiết kế lớp, thiết kế giao diện.
- **Chương 5. Cài đặt và kiểm thử** – môi trường, cấu trúc mã nguồn, giao diện kết quả, kịch bản kiểm thử.
- **Kết luận và hướng phát triển.**
- **Tài liệu tham khảo, Phụ lục.**

## 8. Kế hoạch thực hiện (15 tuần)

| Tuần | Công việc | Sản phẩm |
|---|---|---|
| 1 | Khảo sát cửa hàng, website hiện tại; xác định yêu cầu | Đề cương, danh sách chức năng |
| 2 | Phân tích: tác nhân, use case, đặc tả use case | Sơ đồ use case, đặc tả |
| 3 | Thiết kế CSDL (ERD, mô tả bảng), sơ đồ hoạt động/tuần tự | ERD, script SQL |
| 4 | Khởi tạo dự án, cấu hình EF Core, Identity, seed dữ liệu | Solution chạy được, CSDL |
| 5 | Admin: danh mục, sản phẩm, biến thể, hình ảnh | Module sản phẩm |
| 6 | Admin: nhà cung cấp, phiếu nhập, tồn kho, lịch sử kho | Module kho |
| 7 | Khách: trang chủ, danh mục, tìm kiếm/lọc, chi tiết sản phẩm | Front-end sản phẩm |
| 8 | Khách: giỏ hàng, đặt hàng, mã giảm giá, tính phí ship | Luồng đặt hàng |
| 9 | Khách: tài khoản, lịch sử đơn, hủy đơn, đánh giá; Admin: xử lý đơn | Module đơn hàng |
| 10 | Admin: khuyến mãi, mã giảm giá, banner, tin tức, liên hệ | Module marketing/nội dung |
| 11 | Admin: khách hàng, nhân viên, phân quyền | Module người dùng |
| 12 | Thống kê báo cáo bằng biểu đồ, xuất Excel | Module báo cáo |
| 13 | Kiểm thử, sửa lỗi, tối ưu giao diện responsive | Test case, bản hoàn thiện |
| 14 | Viết báo cáo hoàn chỉnh | Báo cáo |
| 15 | Hoàn thiện slide, tập bảo vệ, nộp | Slide, mã nguồn |
