-- Thêm cấu hình điểm thưởng khi tài liệu được duyệt
-- Kiểm tra nếu cấu hình đã tồn tại
IF NOT EXISTS (SELECT 1 FROM SystemConfigs WHERE ConfigKey = 'PointsForApprovedDocument')
BEGIN
    -- Thêm cấu hình mới với giá trị mặc định là 10 điểm
    INSERT INTO SystemConfigs (ConfigKey, ConfigValue, Description)
    VALUES ('PointsForApprovedDocument', '10', 'Số điểm thưởng cho người dùng khi tài liệu được duyệt');
    
    PRINT 'Đã thêm cấu hình điểm thưởng cho tài liệu được duyệt';
END
ELSE
BEGIN
    -- Nếu đã tồn tại thì bỏ qua
    PRINT 'Cấu hình điểm thưởng cho tài liệu được duyệt đã tồn tại';
END 