# Hướng dẫn thiết lập Google Cloud Credentials

## 1. Tạo và tải Service Account Key

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Tạo một dự án mới hoặc chọn dự án hiện có
3. Từ menu bên trái, chọn "APIs & Services" > "Enabled APIs & services"
4. Nhấn nút "+ ENABLE APIS AND SERVICES"
5. Tìm kiếm "Cloud Text-to-Speech API" và kích hoạt
6. Từ menu bên trái, chọn "IAM & Admin" > "Service accounts"
7. Nhấn "CREATE SERVICE ACCOUNT"
8. Nhập tên service account (ví dụ: "tts-service-account") và mô tả tuỳ chọn
9. Nhấn "CREATE AND CONTINUE"
10. Gán quyền "Cloud Text-to-Speech User" (Role > Cloud Text-to-Speech > Cloud Text-to-Speech User)
11. Nhấn "CONTINUE" và sau đó "DONE"
12. Tìm service account vừa tạo trong danh sách và nhấn vào nó
13. Trong tab "KEYS", nhấn "ADD KEY" > "Create new key"
14. Chọn định dạng "JSON" và nhấn "CREATE"
15. File credentials JSON sẽ được tải về máy tính của bạn

## 2. Cấu hình file credentials

1. Đổi tên file JSON vừa tải về thành `google-credentials.json`
2. Sao chép file vào thư mục gốc của dự án SenseLib

Ví dụ nội dung file `google-credentials.json`:
```json
{
  "type": "service_account",
  "project_id": "your-project-id",
  "private_key_id": "abc123...",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
  "client_email": "your-service-account@your-project.iam.gserviceaccount.com",
  "client_id": "123456789",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/your-service-account%40your-project.iam.gserviceaccount.com",
  "universe_domain": "googleapis.com"
}
```

## 3. Cấu hình trong ứng dụng

Ứng dụng SenseLib sẽ tự động đọc file credentials từ thư mục gốc của dự án và thiết lập biến môi trường `GOOGLE_APPLICATION_CREDENTIALS` khi khởi động.

Nếu bạn thấy thông báo lỗi:
```
CẢNH BÁO: File credentials Google không tìm thấy tại: ...
```

Hãy kiểm tra lại các bước trên để đảm bảo file credentials đã được đặt đúng vị trí.

## 4. Lưu ý bảo mật

- File credentials chứa thông tin nhạy cảm, không nên chia sẻ hoặc commit lên Git
- File `.gitignore` của dự án nên bao gồm `google-credentials.json`
- Trong môi trường sản xuất, nên sử dụng các giải pháp lưu trữ bí mật an toàn như Azure Key Vault, AWS Secrets Manager, hoặc Google Secret Manager 