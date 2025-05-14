-- Cập nhật ActionName cho menu Thống kê (ID 14)
UPDATE AdminMenus SET ActionName = 'Index' WHERE MenuID = 14;

-- Xóa dòng chứa toàn NULL
DELETE FROM AdminMenus WHERE MenuID IS NULL;

-- Thêm menu con "Tải xuống" nếu chưa tồn tại
IF NOT EXISTS (SELECT 1 FROM AdminMenus WHERE MenuName = N'Tải xuống' AND ParentLevel = 9)
BEGIN
    INSERT INTO AdminMenus (MenuName, ItemLevel, IsActive, ParentLevel, ItemOrder, AreaName, ControllerName, ActionName, Icon)
    VALUES (N'Tải xuống', 2, 1, 9, 4, 'Admin', 'Statistics', 'Downloads', 'bi bi-download');
END

-- Kiểm tra lại dữ liệu IdName của menu thống kê
UPDATE AdminMenus SET IdName = 'statistics-nav' WHERE MenuID = 14;

-- Đảm bảo menu con tham chiếu đúng đến menu cha
UPDATE AdminMenus 
SET ParentLevel = 14
WHERE ControllerName = 'Statistics' 
  AND MenuID != 14 
  AND ParentLevel = 9; 