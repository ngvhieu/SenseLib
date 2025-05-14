-- Thêm menu quản lý tài khoản vào menu admin
INSERT INTO SenseLib.dbo.AdminMenu (MenuName, ControllerName, ActionName, ParentLevel, ItemOrder, Icon, AreaName, IdName) 
VALUES (N'Quản lý tài khoản', 'User', 'Index', 0, 50, 'bi bi-people', 'Admin', 'user-nav'); 