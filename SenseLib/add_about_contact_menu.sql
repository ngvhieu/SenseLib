-- Thêm menu About nếu chưa tồn tại
IF NOT EXISTS (SELECT 1 FROM Menu WHERE ControllerName = 'About' AND ActionName = 'Index' AND Position = 1)
BEGIN
    -- Xác định thứ tự lớn nhất hiện tại
    DECLARE @maxOrder INT = ISNULL((SELECT MAX(MenuOrder) FROM Menu WHERE Position = 1 AND ParentID = 0), 0)
    
    -- Thêm menu About
    INSERT INTO Menu (MenuName, IsActive, ControllerName, ActionName, Levels, ParentID, Link, MenuOrder, Position)
    VALUES (N'Giới thiệu', 1, 'About', 'Index', 1, 0, '/About', @maxOrder + 1, 1)
    
    PRINT 'Đã thêm menu Giới thiệu'
END
ELSE
BEGIN
    PRINT 'Menu Giới thiệu đã tồn tại'
END

-- Cập nhật menu About cũ nếu tồn tại
UPDATE Menu 
SET ControllerName = 'About', ActionName = 'Index', Link = '/About'
WHERE ControllerName = 'Home' AND ActionName = 'About'

-- Thêm menu Contact nếu chưa tồn tại
IF NOT EXISTS (SELECT 1 FROM Menu WHERE ControllerName = 'Contact' AND ActionName = 'Index' AND Position = 1)
BEGIN
    -- Xác định thứ tự lớn nhất hiện tại
    DECLARE @maxOrder2 INT = ISNULL((SELECT MAX(MenuOrder) FROM Menu WHERE Position = 1 AND ParentID = 0), 0)
    
    -- Thêm menu Contact
    INSERT INTO Menu (MenuName, IsActive, ControllerName, ActionName, Levels, ParentID, Link, MenuOrder, Position)
    VALUES (N'Liên hệ', 1, 'Contact', 'Index', 1, 0, '/Contact', @maxOrder2 + 1, 1)
    
    PRINT 'Đã thêm menu Liên hệ'
END
ELSE
BEGIN
    PRINT 'Menu Liên hệ đã tồn tại'
END

-- Cập nhật menu Contact cũ nếu tồn tại
UPDATE Menu 
SET ControllerName = 'Contact', ActionName = 'Index', Link = '/Contact'
WHERE ControllerName = 'Home' AND ActionName = 'Contact' 