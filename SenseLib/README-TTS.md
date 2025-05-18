# Hướng dẫn tích hợp Text-to-Speech (TTS) vào SenseLib

## Tổng quan

SenseLib hỗ trợ tích hợp Google Cloud Text-to-Speech API để chuyển đổi nội dung tài liệu PDF thành giọng nói. Chức năng này được cung cấp qua 3 cách:

1. **Trang chuyên dụng** - `/text-to-speech`: Người dùng có thể tải lên file PDF và chuyển đổi
2. **Tích hợp vào trang chi tiết tài liệu**: Thêm nút "Nghe đọc tài liệu" cho mỗi tài liệu
3. **Tích hợp vào PDF viewer**: Khi người dùng xem PDF trực tiếp, họ có thể sử dụng tính năng TTS

## Yêu cầu

- Thiết lập Google Cloud credentials đúng cách (xem file README-GOOGLE-CREDENTIALS.md)
- Các file tài liệu phải là định dạng PDF có nội dung văn bản (không phải scan dạng ảnh)

## API Endpoints

### 1. Tải lên PDF và chuyển đổi

```
POST /api/TextToSpeech/pdf-to-speech
```

Parameters:
- `pdfFile` (FormFile): File PDF cần chuyển đổi
- `languageCode` (string, optional): Mã ngôn ngữ (mặc định: vi-VN)
- `voiceName` (string, optional): Mã giọng đọc (mặc định: vi-VN-Standard-A)

### 2. Chuyển đổi tài liệu theo ID

```
GET /api/TextToSpeech/document/{id}
```

Parameters:
- `id` (int): ID của tài liệu trong cơ sở dữ liệu
- `languageCode` (string, query, optional): Mã ngôn ngữ (mặc định: vi-VN)
- `voiceName` (string, query, optional): Mã giọng đọc (mặc định: vi-VN-Standard-A)

## Tích hợp vào trang web khác

### 1. Thêm script vào trang

```html
<script src="/js/document-tts.js"></script>
```

### 2. Khởi tạo component

```html
<div id="ttsContainer"></div>

<script>
    document.addEventListener('DOMContentLoaded', function() {
        const ttsContainer = document.getElementById('ttsContainer');
        new DocumentTTS(documentId, ttsContainer, {
            languageCode: 'vi-VN',
            voiceName: 'vi-VN-Standard-A',
            buttonClass: 'btn btn-primary',
            buttonText: 'Đọc tài liệu'
        });
    });
</script>
```

### Tùy chọn

DocumentTTS component hỗ trợ các tùy chọn sau:

| Tùy chọn | Mô tả | Giá trị mặc định |
|----------|-------|------------------|
| languageCode | Mã ngôn ngữ | vi-VN |
| voiceName | Mã giọng đọc | vi-VN-Standard-A |
| buttonClass | Class CSS cho nút | btn btn-primary |
| iconClass | Class CSS cho icon | fas fa-volume-up |
| buttonText | Nội dung nút | Đọc tài liệu |
| loadingText | Nội dung khi đang xử lý | Đang xử lý... |
| errorText | Nội dung lỗi | Đã xảy ra lỗi |

## Ngôn ngữ và giọng đọc được hỗ trợ

| Ngôn ngữ | Mã | Giọng đọc |
|----------|-----|-----------|
| Tiếng Việt | vi-VN | vi-VN-Standard-A (Nữ)<br>vi-VN-Standard-B (Nam)<br>vi-VN-Standard-C (Nữ)<br>vi-VN-Standard-D (Nam) |
| Tiếng Anh (Mỹ) | en-US | en-US-Standard-A (Nam)<br>en-US-Standard-B (Nam)<br>en-US-Standard-C (Nữ)<br>en-US-Standard-D (Nam)<br>en-US-Standard-E (Nữ)<br>en-US-Standard-F (Nữ)<br>en-US-Standard-G (Nữ)<br>en-US-Standard-H (Nữ)<br>en-US-Standard-I (Nam)<br>en-US-Standard-J (Nam) |
| Tiếng Anh (Anh) | en-GB | en-GB-Standard-A (Nữ)<br>en-GB-Standard-B (Nam)<br>en-GB-Standard-C (Nữ)<br>en-GB-Standard-D (Nam)<br>en-GB-Standard-F (Nữ) |

Xem thêm danh sách đầy đủ các ngôn ngữ và giọng đọc được hỗ trợ tại [Google Cloud Text-to-Speech documentation](https://cloud.google.com/text-to-speech/docs/voices). 