-- Thêm menu FileManager vào AdminMenu
INSERT INTO AdminMenus (MenuName, ControllerName, ActionName, Icon, ItemOrder, ItemLevel, ParentID, IsActive)
VALUES ('Quản lý File', 'FileManager', 'Index', 'bi bi-folder', 6, 0, NULL, 1); 