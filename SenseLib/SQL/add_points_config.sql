-- Script thêm cấu hình điểm thưởng cho tài liệu được duyệt
-- Chạy script này để thiết lập cấu hình ban đầu

-- Kiểm tra xem cấu hình đã tồn tại chưa
IF NOT EXISTS (SELECT 1 FROM SystemConfigs WHERE ConfigKey = 'PointsForApprovedDocument')
BEGIN
    -- Thêm cấu hình mới với giá trị mặc định là 10 điểm
    INSERT INTO SystemConfigs (ConfigKey, ConfigValue, Description)
    VALUES ('PointsForApprovedDocument', '10', 'Số điểm thưởng cho người dùng khi tài liệu của họ được duyệt. Điểm thưởng sẽ được cộng vào ví người dùng.');
    
    PRINT 'Đã thêm cấu hình điểm thưởng cho tài liệu được duyệt';
END
ELSE
BEGIN
    -- Cập nhật mô tả nếu đã tồn tại
    UPDATE SystemConfigs 
    SET Description = 'Số điểm thưởng cho người dùng khi tài liệu của họ được duyệt. Điểm thưởng sẽ được cộng vào ví người dùng.'
    WHERE ConfigKey = 'PointsForApprovedDocument';
    
    PRINT 'Cấu hình điểm thưởng đã tồn tại, đã cập nhật mô tả';
END

-- Thêm cấu hình này vào danh sách cấu hình cần được bảo vệ (không cho phép xóa)
-- Kiểm tra xem khóa SystemConfigEssentials đã tồn tại chưa
IF EXISTS (SELECT 1 FROM SystemConfigs WHERE ConfigKey = 'SystemConfigEssentials')
BEGIN
    DECLARE @currentEssentials NVARCHAR(1000);
    SELECT @currentEssentials = ConfigValue FROM SystemConfigs WHERE ConfigKey = 'SystemConfigEssentials';
    
    -- Kiểm tra xem PointsForApprovedDocument đã có trong danh sách chưa
    IF CHARINDEX('PointsForApprovedDocument', @currentEssentials) = 0
    BEGIN
        -- Thêm vào danh sách
        UPDATE SystemConfigs
        SET ConfigValue = ConfigValue + ',PointsForApprovedDocument'
        WHERE ConfigKey = 'SystemConfigEssentials';
        
        PRINT 'Đã thêm PointsForApprovedDocument vào danh sách cấu hình cần bảo vệ';
    END
END 