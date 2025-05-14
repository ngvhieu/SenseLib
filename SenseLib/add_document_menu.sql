-- Kiểm tra xem menu Document đã tồn tại chưa
IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE MenuName = N'Tài liệu' AND ControllerName = 'Document')
BEGIN
    -- Thêm menu Tài liệu vào AdminMenu
    INSERT INTO AdminMenu (MenuName, ItemLevel, IsActive, ParentLevel, ItemOrder, AreaName, ControllerName, ActionName, Icon)
    VALUES (N'Tài liệu', 1, 1, 0, 3, 'Admin', 'Document', 'Index', 'bi bi-journal-text');
END 