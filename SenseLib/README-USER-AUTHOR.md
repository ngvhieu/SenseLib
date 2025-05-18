# Tính năng "Đặt tôi làm tác giả"

## Mô tả
Tính năng này cho phép người dùng đặt chính họ làm tác giả khi tải lên hoặc chỉnh sửa tài liệu trong hệ thống SenseLib.

## Chức năng chính
1. Người dùng có thể chọn option "Đặt tôi làm tác giả" khi tạo mới tài liệu
2. Khi chọn option này, hệ thống sẽ tự động:
   - Vô hiệu hóa dropdown chọn tác giả
   - Lấy tên đầy đủ của người dùng hiện tại 
   - Tạo mới tác giả (nếu chưa tồn tại) hoặc sử dụng tác giả có sẵn nếu tên đã tồn tại
   - Gán tác giả này cho tài liệu

## Triển khai
Tính năng đã được cập nhật trong các file sau:

1. `Controllers/UploadController.cs`
   - Cập nhật phương thức `Create` để xử lý khi người dùng chọn làm tác giả
   - Cập nhật phương thức `Edit` để xử lý khi chỉnh sửa tài liệu

2. `Views/Upload/Create.cshtml`
   - Thêm checkbox "Đặt tôi làm tác giả"
   - Thêm JavaScript để vô hiệu hóa dropdown tác giả và hiển thị thông tin người dùng

3. `Views/Upload/Edit.cshtml`
   - Thêm checkbox "Đặt tôi làm tác giả"
   - Thêm JavaScript để vô hiệu hóa dropdown tác giả và hiển thị thông tin người dùng

## Quy trình
1. Khi người dùng tạo mới tài liệu, có thể chọn checkbox "Đặt tôi làm tác giả"
2. Hệ thống sẽ lấy tên người dùng hiện tại, kiểm tra xem có tác giả nào có tên này không
3. Nếu đã tồn tại tác giả, hệ thống sẽ sử dụng tác giả đó
4. Nếu chưa tồn tại, hệ thống sẽ tạo mới tác giả với tên người dùng và gán cho tài liệu
5. Quy trình tương tự áp dụng khi người dùng chỉnh sửa tài liệu

## Lưu ý
- Tài liệu sau khi tạo hoặc chỉnh sửa vẫn cần được admin phê duyệt trước khi xuất bản
- Nếu người dùng thay đổi tác giả khi chỉnh sửa tài liệu đã được phê duyệt, tài liệu sẽ cần phê duyệt lại 