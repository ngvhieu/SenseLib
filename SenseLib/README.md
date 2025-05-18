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
   git clone https://github.com/hiu.ngv/SenseLib.git
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

## Hướng dẫn thanh toán VNPay

SenseLib hỗ trợ thanh toán trực tuyến qua cổng VNPay. Để sử dụng tính năng này, bạn cần cấu hình các thông số của VNPay trong file `appsettings.json`:

```json
"VNPay": {
  "TmnCode": "YOUR_TMN_CODE",
  "HashSecret": "YOUR_HASH_SECRET",
  "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
  "Command": "pay",
  "CurrCode": "VND",
  "Version": "2.1.0",
  "Locale": "vn"
}
```

### Các bước thanh toán

1. Người dùng chọn tài liệu muốn mua
2. Trang chi tiết tài liệu hiển thị nút "Thanh toán qua VNPay"
3. Người dùng click vào nút thanh toán và được chuyển đến trang xác nhận
4. Sau khi xác nhận, người dùng được chuyển đến cổng thanh toán VNPay
5. Sau khi thanh toán thành công, hệ thống cập nhật trạng thái giao dịch và cho phép truy cập tài liệu

### Môi trường test

Trong môi trường phát triển, bạn có thể sử dụng Sandbox của VNPay để kiểm thử tính năng thanh toán mà không cần thực hiện giao dịch thật.

## Chức năng Text-to-Speech (TTS)

SenseLib hỗ trợ chuyển đổi văn bản PDF thành giọng nói sử dụng Google Cloud Text-to-Speech API. Đây là hướng dẫn để thiết lập và sử dụng tính năng này:

### Thiết lập Google Cloud Text-to-Speech API

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Tạo một dự án mới hoặc chọn dự án hiện có
3. Kích hoạt API Text-to-Speech cho dự án
4. Tạo Service Account Key:
   - Đi đến "IAM & Admin" > "Service Accounts"
   - Tạo service account mới hoặc chọn service account hiện có
   - Tạo khóa mới (JSON) và tải về
   - Đặt file JSON đã tải về vào thư mục gốc của dự án và đổi tên thành `google-credentials.json`

5. Thiết lập biến môi trường:
   ```
   $env:GOOGLE_APPLICATION_CREDENTIALS="[PATH]\google-credentials.json"
   ```
   Thay `[PATH]` bằng đường dẫn đầy đủ đến file JSON.

### Sử dụng chức năng Text-to-Speech

1. Truy cập đường dẫn `/text-to-speech` trên ứng dụng
2. Tải lên file PDF cần chuyển đổi
3. Chọn ngôn ngữ và giọng đọc phù hợp
4. Nhấn "Chuyển đổi sang giọng nói"
5. Sau khi xử lý hoàn tất, bạn có thể nghe và tải xuống các file âm thanh đã được tạo

### Lưu ý khi sử dụng

- Giới hạn kích thước file: 50MB
- Google Cloud TTS API có giới hạn 5000 ký tự cho mỗi request, nên các văn bản dài sẽ được chia thành nhiều đoạn nhỏ
- Đảm bảo dự án Google Cloud của bạn đã thiết lập thanh toán để sử dụng API không bị giới hạn


