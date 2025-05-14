# SenseLib - Hệ thống quản lý thư viện số

SenseLib là một hệ thống quản lý thư viện số hiện đại, cho phép người dùng truy cập, tìm kiếm và mua tài liệu trực tuyến. Hệ thống hỗ trợ các tài liệu miễn phí và trả phí, với đầy đủ tính năng quản lý dành cho quản trị viên.

## Tính năng chính

### Dành cho người dùng
- Đăng ký và đăng nhập tài khoản
- Tìm kiếm và lọc tài liệu theo nhiều tiêu chí
- Xem trước và tải tài liệu miễn phí
- Mua và truy cập tài liệu trả phí
- Quản lý thông tin cá nhân và lịch sử giao dịch
- Bảo mật tài khoản với giới hạn đăng nhập sai

### Dành cho quản trị viên
- Quản lý người dùng (thêm, sửa, xóa, khóa/mở khóa)
- Quản lý tài liệu (thêm, sửa, xóa, phân loại)
- Quản lý danh mục, tác giả, nhà xuất bản
- Theo dõi và quản lý giao dịch thanh toán
- Báo cáo doanh thu và thống kê (theo ngày, danh mục)
- Cấu hình hệ thống (số lần đăng nhập sai tối đa, thời gian khóa tài khoản, v.v.)

## Yêu cầu hệ thống

- .NET 6.0 trở lên
- SQL Server 2019 trở lên
- VS Code hoặc Visual Studio 2022

## Cài đặt

1. Clone repository về máy:
   ```
   git clone https://github.com/yourusername/SenseLib.git
   cd SenseLib
   ```

2. Khôi phục các gói NuGet:
   ```
   dotnet restore
   ```

3. Cập nhật chuỗi kết nối trong appsettings.json
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=yourserver;Database=SenseLib;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```

4. Tạo cơ sở dữ liệu và áp dụng migration:
   ```
   dotnet ef database update
   ```

5. Chạy ứng dụng:
   ```
   dotnet run
   ```

## Cấu trúc dự án

```
SenseLib/
├── Areas/
│   └── Admin/                    # Phần quản trị
│       ├── Controllers/          # Điều khiển cho khu vực Admin
│       └── Views/                # Giao diện cho khu vực Admin
├── Controllers/                  # Điều khiển chính
├── Models/                       # Mô hình dữ liệu
├── Views/                        # Giao diện người dùng
├── wwwroot/                      # Tài nguyên tĩnh (CSS, JS, hình ảnh)
└── Data/                         # Lớp truy cập dữ liệu
```

## Tính năng chi tiết

### Quản lý người dùng
- Hệ thống phân quyền (User, Admin)
- Giới hạn đăng nhập sai và khóa tài khoản tự động
- Quản lý thông tin cá nhân và hình đại diện

### Quản lý tài liệu
- Hỗ trợ nhiều định dạng tài liệu (PDF, DOC, DOCX, v.v.)
- Phân loại theo danh mục, tác giả, nhà xuất bản
- Hỗ trợ tài liệu miễn phí và trả phí
- Tìm kiếm nâng cao với nhiều tiêu chí

### Thanh toán
- Ghi nhận và theo dõi giao dịch
- Quản lý trạng thái giao dịch (Đang xử lý, Hoàn thành, Thất bại)
- Báo cáo doanh thu theo thời gian và danh mục

### Cấu hình hệ thống
- Cấu hình số lần đăng nhập sai tối đa
- Cấu hình thời gian khóa tài khoản
- Cấu hình số lượng tài liệu hiển thị trên trang chủ

## Tài liệu liên quan

- [Tài liệu API](docs/api.md)
- [Hướng dẫn triển khai](docs/deployment.md)
- [Hướng dẫn sử dụng cho quản trị viên](docs/admin-guide.md)
- [Hướng dẫn sử dụng cho người dùng](docs/user-guide.md)

## Đóng góp

Chúng tôi rất hoan nghênh mọi đóng góp cho dự án SenseLib. Vui lòng tạo pull request hoặc báo cáo lỗi qua mục Issues trên GitHub.

## Giấy phép

SenseLib được phát hành dưới giấy phép MIT. Xem file [LICENSE](LICENSE) để biết thêm chi tiết.

## Liên hệ

- Email: your@email.com
- Website: https://senselib.com
- GitHub: https://github.com/yourusername/SenseLib 