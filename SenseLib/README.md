# SenseLib - Hệ thống quản lý thư viện số thông minh

SenseLib là hệ thống quản lý thư viện số hiện đại, cho phép người dùng truy cập, tìm kiếm và mua tài liệu trực tuyến. Hệ thống hỗ trợ nhiều loại tài liệu (miễn phí và trả phí), tích hợp các công nghệ tiên tiến như chatbot, text-to-speech và thanh toán trực tuyến.

## Tính năng chính

### Dành cho người dùng
- **Quản lý tài khoản**: Đăng ký, đăng nhập, quản lý thông tin cá nhân và hình đại diện
- **Tìm kiếm tài liệu**: Tìm kiếm và lọc tài liệu theo nhiều tiêu chí (danh mục, tác giả, miễn phí/trả phí)
- **Xem và tải tài liệu**: Xem trước và tải tài liệu miễn phí
- **Mua tài liệu trả phí**: Thanh toán qua VNPay để mua quyền truy cập tài liệu
- **Ví điện tử**: Nạp tiền và sử dụng ví điện tử để mua tài liệu
- **Tương tác thông minh**: 
  - Chatbot hỏi đáp về nội dung tài liệu
  - Chuyển đổi văn bản thành giọng nói (Text-to-Speech)
- **Theo dõi hoạt động**: Lịch sử mua hàng, tải tài liệu và thanh toán

### Dành cho quản trị viên
- **Quản lý người dùng**: Thêm, sửa, xóa, khóa/mở khóa tài khoản
- **Quản lý tài liệu**: Thêm, sửa, xóa, phê duyệt tài liệu
- **Quản lý danh mục**: Danh mục, tác giả, nhà xuất bản
- **Quản lý menu và slideshow**: Tùy chỉnh menu và slideshow hiển thị trên trang chủ
- **Quản lý giao dịch**: Theo dõi và quản lý thanh toán, nạp tiền
- **Báo cáo thống kê**: Thống kê doanh thu, lượt tải, người dùng hoạt động
- **Cấu hình hệ thống**: Tùy chỉnh các thông số hệ thống

## Công nghệ sử dụng

- **Backend**: ASP.NET Core 8.0, Entity Framework Core
- **Frontend**: HTML, CSS, JavaScript, Bootstrap
- **Database**: SQL Server
- **Công nghệ AI**: 
  - Google Cloud Text-to-Speech API
  - AI Chatbot cho tương tác với tài liệu
- **Thanh toán**: Tích hợp cổng thanh toán VNPay

## Cài đặt

1. Clone repository:
   ```
   git clone https://github.com/[username]/SenseLib.git
   cd SenseLib
   ```

2. Khôi phục các gói NuGet:
   ```
   dotnet restore
   ```

3. Cập nhật chuỗi kết nối trong appsettings.json:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=yourserver;Database=SenseLib;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```

4. Tạo cơ sở dữ liệu sử dụng script SQL:
   ```sql
   -- Sử dụng script SQL trong thư mục SQL/ để tạo cơ sở dữ liệu
   ```

5. Chạy ứng dụng:
   ```
   dotnet run
   ```

## Cấu trúc dự án

```
SenseLib/
├── Areas/Admin/                 # Khu vực quản trị
├── Controllers/                 # Các controller chính
├── Models/                      # Mô hình dữ liệu
├── Services/                    # Các dịch vụ (VNPay, TTS, Chatbot)
├── Views/                       # Giao diện người dùng
├── wwwroot/                     # Tài nguyên tĩnh và uploads
└── Utilities/                   # Các tiện ích bổ sung
```

## Tính năng chi tiết

### Quản lý tài liệu
- Hỗ trợ nhiều định dạng tài liệu (PDF, DOC, DOCX, v.v.)
- Phân loại theo danh mục, tác giả, nhà xuất bản
- Hỗ trợ tài liệu miễn phí và trả phí
- Tìm kiếm nâng cao với nhiều tiêu chí
- Tính năng xem trước tài liệu

### Tương tác thông minh với tài liệu
- **Chatbot tương tác**: Cho phép người dùng đặt câu hỏi và nhận câu trả lời về nội dung tài liệu
- **Text-to-Speech**: Chuyển đổi nội dung tài liệu PDF thành âm thanh với nhiều giọng đọc và ngôn ngữ khác nhau
- **Tính năng ghi chú**: Người dùng có thể thêm ghi chú cá nhân cho tài liệu đã mua

### Hệ thống thanh toán
- **VNPay**: Thanh toán trực tuyến qua cổng VNPay
- **Ví điện tử**: Nạp tiền và sử dụng ví điện tử nội bộ
- **Quản lý giao dịch**: Theo dõi trạng thái giao dịch, lịch sử thanh toán
- **Báo cáo doanh thu**: Thống kê theo thời gian và danh mục

### Bảo mật và quyền truy cập
- Hệ thống phân quyền (User, Admin)
- Giới hạn đăng nhập sai và khóa tài khoản tự động
- Bảo vệ tài liệu có phí, chỉ người mua mới có quyền truy cập đầy đủ

### Giao diện người dùng
- Giao diện hiện đại, thân thiện với người dùng
- Thiết kế responsive, hỗ trợ đa thiết bị
- Menu tùy chỉnh và slideshow trên trang chủ

## Thiết lập Google Cloud Text-to-Speech API

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

## Cấu hình thanh toán VNPay

SenseLib hỗ trợ thanh toán trực tuyến qua cổng VNPay. Để sử dụng tính năng này, cần cấu hình trong file `appsettings.json`:

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

## Yêu cầu hệ thống

- .NET 8.0 trở lên
- SQL Server 2019 trở lên
- VS Code hoặc Visual Studio 2022


