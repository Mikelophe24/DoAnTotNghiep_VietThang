# Hướng dẫn cài đặt và chạy dự án trên máy mới

Tài liệu này dành cho người nhận mã nguồn (GVHD, bạn cùng nhóm, hội đồng) để chạy và phát triển tiếp dự án trên máy khác. Ứng dụng **tự tạo CSDL, tự migrate và tự seed dữ liệu** khi khởi động, nên không cần chép file database.

## 1. Phần mềm cần cài

| Phần mềm | Phiên bản | Kiểm tra | Tải về |
|---|---|---|---|
| .NET SDK | **8.0** (hoặc mới hơn kèm runtime 8) | `dotnet --list-sdks` | https://dotnet.microsoft.com/download/dotnet/8.0 |
| Node.js | **20 LTS** hoặc 22 LTS | `node --version` | https://nodejs.org |
| SQL Server LocalDB | 2019 / 2022 | `sqllocaldb info` | Có sẵn khi cài Visual Studio 2022 (workload *ASP.NET and web development*), hoặc bộ cài **SQL Server Express** → chọn *LocalDB* |
| Git | bất kỳ | `git --version` | https://git-scm.com |

Tùy chọn: Visual Studio 2022 **17.13 trở lên** (mở được file `VietThangFashion.slnx`) hoặc VS Code + extension C# Dev Kit; SSMS để xem CSDL.

Lưu ý file solution `.slnx` chỉ được .NET SDK 9.0.200+ hỗ trợ. Nếu máy chỉ có .NET 8 SDK, build và chạy trực tiếp từng project (`dotnet run` trong thư mục project) như hướng dẫn bên dưới, không cần mở solution.

## 2. Lấy mã nguồn và chuẩn bị

```powershell
git clone https://github.com/Mikelophe24/DoAnTotNghiep_VietThang.git D:\DoAnPhuc
cd D:\DoAnPhuc
powershell -ExecutionPolicy Bypass -File .\setup.ps1
```

`setup.ps1` kiểm tra .NET, Node, LocalDB; khởi động LocalDB; build backend; `npm install` cho Angular rồi in hướng dẫn chạy. Thêm `-RunTests` để chạy luôn 93 unit test, `-SkipNpm` nếu đã cài `node_modules`.

Nếu không muốn dùng script, làm tay:

```powershell
sqllocaldb start MSSQLLocalDB
cd src\VietThang.Web;   dotnet build
cd ..\vietthang-web;    npm install
```

## 3. Chạy

Mở **hai** cửa sổ terminal:

```powershell
# Terminal 1 – Backend (REST API + trang quản trị) tại http://localhost:5292
cd D:\DoAnPhuc\src\VietThang.Web
dotnet run -- --seed-demo     # LẦN ĐẦU: tạo CSDL VietThangFashion, seed dữ liệu gốc + 96 đơn hàng demo
dotnet run                    # các lần sau
```

```powershell
# Terminal 2 – Storefront Angular tại http://localhost:4200
cd D:\DoAnPhuc\src\vietthang-web
npm start
```

| Địa chỉ | Dành cho | Tài khoản |
|---|---|---|
| http://localhost:4200 | Khách hàng (Angular) | `khachhang@gmail.com / KhachHang@123` hoặc 15 khách demo `lan.nguyen@example.com … / KhachHang@123` |
| http://localhost:5292/Admin | Quản trị (Razor) | `admin@thoitrangvietthang.vn / Admin@123` (Admin), `nhanvien@thoitrangvietthang.vn / NhanVien@123` (Employee) |

Truy cập `http://localhost:5292/` sẽ tự chuyển sang `http://localhost:4200`.

## 4. Những thứ KHÔNG đi theo Git (cần biết)

| Hạng mục | Vị trí | Xử lý trên máy mới |
|---|---|---|
| Khóa ký JWT | `src/VietThang.Web/App_Data/jwt-key.txt` | Tự sinh lại khi chạy lần đầu. Token cũ hết hiệu lực, đăng nhập lại là xong. |
| Cấu hình SMTP (gửi email thật) | user-secrets | Không bắt buộc. Chưa cấu hình thì email (quên mật khẩu) ghi ra console. Muốn gửi thật: xem mục 6. |
| Ảnh tải lên qua Admin | `src/VietThang.Web/wwwroot/uploads/` | Chỉ cần chép khi khôi phục CSDL cũ (mục 5). CSDL mới tạo không tham chiếu các ảnh này. Cấu hình Cloudinary (mục 6b) để không còn phụ thuộc máy. |
| Khóa Cloudinary | user-secrets | Không bắt buộc. Xem mục 6b. |
| Database | LocalDB `VietThangFashion` | Tự tạo lại từ migration + seed. Muốn giữ dữ liệu nhập tay: mục 5. |
| `node_modules/`, `bin/`, `obj/`, `dist/` | | `setup.ps1` tạo lại. |

## 5. (Tùy chọn) Mang theo dữ liệu đã nhập tay

Chỉ cần khi trên máy cũ đã nhập sản phẩm/đơn hàng thật qua Admin và muốn giữ nguyên.

Máy cũ, trong SSMS kết nối `(localdb)\MSSQLLocalDB`: chuột phải database `VietThangFashion` → *Tasks* → *Back Up…* → file `.bak`. Chép kèm thư mục `wwwroot/uploads/`.

Máy mới: **chưa chạy `dotnet run`**, mở SSMS → *Restore Database…* từ file `.bak`, chép `uploads/` vào đúng vị trí, rồi chạy `dotnet run` (không dùng `--seed-demo`). Ứng dụng chỉ áp dụng migration còn thiếu, không ghi đè dữ liệu.

## 6. (Tùy chọn) Gửi email thật bằng Gmail

```powershell
cd src\VietThang.Web
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Smtp:User" "dia-chi@gmail.com"
dotnet user-secrets set "Smtp:Password" "xxxx xxxx xxxx xxxx"   # App password 16 ký tự
dotnet user-secrets set "Smtp:FromEmail" "dia-chi@gmail.com"
```

## 6b. (Khuyên dùng) Lưu ảnh tải lên trên Cloudinary

Mặc định ảnh tải qua Admin lưu vào `wwwroot/uploads` trên đĩa máy đang chạy, nên **không đi theo Git** và mỗi máy một bộ ảnh khác nhau. Khi cấu hình Cloudinary, ảnh được tải lên đám mây và URL lưu trong CSDL là tuyệt đối (`https://res.cloudinary.com/...`), máy nào mở cũng thấy, deploy lại không mất.

Lấy ba giá trị trong Cloudinary Dashboard (mục *API Keys*), rồi lưu bằng user-secrets trên **mỗi máy** chạy backend:

```powershell
cd src\VietThang.Web
dotnet user-secrets set "Cloudinary:CloudName" "ten-cloud"
dotnet user-secrets set "Cloudinary:ApiKey" "123456789012345"
dotnet user-secrets set "Cloudinary:ApiSecret" "xxxxxxxxxxxxxxxxxxxxxxx"
```

Khởi động lại `dotnet run`. Kiểm tra: Admin → Sản phẩm → Sửa → Thêm ảnh; ảnh mới hiện ra với URL bắt đầu bằng `https://res.cloudinary.com/`. Ảnh nằm trong thư mục `vietthang/products/{id}`, `vietthang/categories`, `vietthang/banners`, `vietthang/posts` trên Cloudinary (đổi tên gốc bằng `Cloudinary:Folder`).

Thiếu bất kỳ khóa nào trong ba khóa trên thì ứng dụng tự quay về lưu trên đĩa, không báo lỗi. Ảnh cũ dạng `/uploads/...` vẫn hiển thị bình thường; muốn chuyển lên Cloudinary thì xóa và tải lại qua Admin. Gói miễn phí của Cloudinary là 25 GB lưu trữ và băng thông mỗi tháng.

## 7. Dùng SQL Server khác thay LocalDB

Sửa `src/VietThang.Web/appsettings.json`:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=VietThangFashion;Trusted_Connection=True;TrustServerCertificate=True"
```

Hoặc dùng tài khoản SQL: `Server=localhost;Database=VietThangFashion;User Id=sa;Password=...;TrustServerCertificate=True`. Chạy lại `dotnet run`, CSDL sẽ được tạo trên server đó.

## 8. Lỗi thường gặp

| Triệu chứng | Nguyên nhân / cách xử lý |
|---|---|
| `Không thể migrate/seed CSDL` khi `dotnet run` | LocalDB chưa chạy: `sqllocaldb start MSSQLLocalDB`. Hoặc chuỗi kết nối sai (mục 7). |
| `A network-related or instance-specific error` | Chưa cài LocalDB / SQL Server. Kiểm tra `sqllocaldb info`. |
| `npm start` báo lỗi phiên bản Node | Angular 18 cần Node ≥ 18.19. Cài Node 20 LTS. |
| Angular chạy nhưng không có dữ liệu, lỗi 504 trong console | Backend chưa chạy ở cổng 5292 (proxy `proxy.conf.json` trỏ sang đó). Mở terminal 1 trước. |
| Đăng nhập khách báo lỗi 401 liên tục | Khóa JWT mới sinh khác máy cũ. Xóa `localStorage` của trình duyệt hoặc đăng xuất rồi đăng nhập lại. |
| `dotnet build VietThangFashion.slnx` báo không hiểu định dạng | SDK < 9.0.200 không hỗ trợ `.slnx`. Build từng csproj: `dotnet build src\VietThang.Web\VietThang.Web.csproj`. |
| Cổng 5292 hoặc 4200 bị chiếm | Đổi cổng trong `Properties/launchSettings.json` (và `proxy.conf.json`, `Cors:Origins`, `Storefront:Url` tương ứng). |
| Migration pending sau khi pull code mới | Chỉ cần `dotnet run`, ứng dụng tự áp dụng migration. |

## 9. Chạy unit test

```powershell
dotnet test tests\VietThang.Tests
```

93 test xUnit dùng EF Core InMemory, không cần SQL Server.
