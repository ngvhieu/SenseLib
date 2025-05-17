-- Cập nhật cấu hình để hỗ trợ nhiều định dạng tệp tin

-- Thêm cấu hình mới cho các định dạng tệp được hỗ trợ
IF NOT EXISTS (SELECT * FROM SystemConfigs WHERE ConfigKey = 'SupportedFileFormats')
BEGIN
    INSERT INTO SystemConfigs (ConfigKey, ConfigValue, Description)
    VALUES ('SupportedFileFormats', '.pdf,.doc,.docx,.xls,.xlsx,.xlsm,.ppt,.pptx,.pptm,.txt,.rtf,.csv,.odt,.ods,.odp,.md,.html,.htm,.xml,.json,.log,.zip,.rar,.7z,.mp3,.mp4,.avi,.png,.jpg,.jpeg,.gif,.bmp,.svg', 
    'Các định dạng tệp được hỗ trợ trên hệ thống')
END
ELSE
BEGIN
    UPDATE SystemConfigs 
    SET ConfigValue = '.pdf,.doc,.docx,.xls,.xlsx,.xlsm,.ppt,.pptx,.pptm,.txt,.rtf,.csv,.odt,.ods,.odp,.md,.html,.htm,.xml,.json,.log,.zip,.rar,.7z,.mp3,.mp4,.avi,.png,.jpg,.jpeg,.gif,.bmp,.svg',
        Description = 'Các định dạng tệp được hỗ trợ trên hệ thống'
    WHERE ConfigKey = 'SupportedFileFormats'
END

-- Thêm cấu hình cho giới hạn kích thước tệp
IF NOT EXISTS (SELECT * FROM SystemConfigs WHERE ConfigKey = 'MaxFileSize')
BEGIN
    INSERT INTO SystemConfigs (ConfigKey, ConfigValue, Description)
    VALUES ('MaxFileSize', '52428800', 'Kích thước tệp tối đa cho phép tải lên (byte - 50MB)')
END

-- Thêm cấu hình cho giới hạn kích thước ảnh bìa
IF NOT EXISTS (SELECT * FROM SystemConfigs WHERE ConfigKey = 'MaxImageSize')
BEGIN
    INSERT INTO SystemConfigs (ConfigKey, ConfigValue, Description)
    VALUES ('MaxImageSize', '5242880', 'Kích thước ảnh bìa tối đa cho phép tải lên (byte - 5MB)')
END

-- Thêm cấu hình các định dạng tệp được chuyển đổi sang PDF
IF NOT EXISTS (SELECT * FROM SystemConfigs WHERE ConfigKey = 'ConvertibleToPdfFormats')
BEGIN
    INSERT INTO SystemConfigs (ConfigKey, ConfigValue, Description)
    VALUES ('ConvertibleToPdfFormats', '.doc,.docx,.rtf,.odt,.ppt,.pptx,.odp,.xls,.xlsx,.csv,.ods,.txt,.md,.html,.htm,.xml,.json,.log,.png,.jpg,.jpeg,.gif,.bmp,.svg', 
    'Các định dạng tệp có thể chuyển đổi sang PDF')
END
ELSE
BEGIN
    UPDATE SystemConfigs 
    SET ConfigValue = '.doc,.docx,.rtf,.odt,.ppt,.pptx,.odp,.xls,.xlsx,.csv,.ods,.txt,.md,.html,.htm,.xml,.json,.log,.png,.jpg,.jpeg,.gif,.bmp,.svg',
        Description = 'Các định dạng tệp có thể chuyển đổi sang PDF'
    WHERE ConfigKey = 'ConvertibleToPdfFormats'
END 