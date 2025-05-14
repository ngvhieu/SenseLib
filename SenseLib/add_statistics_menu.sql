-- Thêm menu thống kê vào bảng AdminMenus

-- Thêm menu chính StatisticsID = 9
INSERT INTO AdminMenus (MenuName, ControllerName, ActionName, Icon, IdName, IsActive, ItemLevel, ItemOrder, ParentLevel, AreaName) 
VALUES (N'Thống kê', 'Statistics', 'Index', 'bi bi-graph-up', 'statistics-nav', 1, 1, 9, 0, 'Admin');

-- Thêm menu con Thống kê bình luận
INSERT INTO AdminMenus (MenuName, ControllerName, ActionName, Icon, IsActive, ItemLevel, ItemOrder, ParentLevel, AreaName) 
VALUES (N'Bình luận', 'Statistics', 'Comments', 'bi bi-chat-dots', 1, 2, 1, 9, 'Admin');

-- Thêm menu con Thống kê đánh giá
INSERT INTO AdminMenus (MenuName, ControllerName, ActionName, Icon, IsActive, ItemLevel, ItemOrder, ParentLevel, AreaName) 
VALUES (N'Đánh giá', 'Statistics', 'Ratings', 'bi bi-star', 1, 2, 2, 9, 'Admin');

-- Thêm menu con Thống kê yêu thích
INSERT INTO AdminMenus (MenuName, ControllerName, ActionName, Icon, IsActive, ItemLevel, ItemOrder, ParentLevel, AreaName) 
VALUES (N'Yêu thích', 'Statistics', 'Favorites', 'bi bi-heart', 1, 2, 3, 9, 'Admin');

-- Thêm menu con Thống kê tải xuống
INSERT INTO AdminMenus (MenuName, ControllerName, ActionName, Icon, IsActive, ItemLevel, ItemOrder, ParentLevel, AreaName) 
VALUES (N'Tải xuống', 'Statistics', 'Downloads', 'bi bi-download', 1, 2, 4, 9, 'Admin'); 