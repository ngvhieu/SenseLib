using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SenseLib.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminMenuController : Controller
    {
        private readonly DataContext _context;

        public AdminMenuController(DataContext context)
        {
            _context = context;
        }

        // GET: /AdminMenu/AddDocumentMenuItem
        public async Task<IActionResult> AddDocumentMenuItem()
        {
            // Kiểm tra xem menu item Document đã tồn tại chưa
            bool documentMenuExists = await _context.AdminMenus
                .AnyAsync(m => m.ControllerName == "Document" && m.MenuName == "Tài liệu" && m.AreaName == "Admin");

            if (!documentMenuExists)
            {
                // Xác định vị trí để thêm menu mới (sau menu thứ 2)
                int itemOrder = 3;
                
                // Tạo menu item Document
                var documentMenu = new AdminMenu
                {
                    MenuName = "Tài liệu",
                    ItemLevel = 1,
                    IsActive = true,
                    ParentLevel = 0, // Menu cấp cao nhất
                    ItemOrder = itemOrder,
                    AreaName = "Admin",
                    ControllerName = "Document",
                    ActionName = "Index",
                    Icon = "bi bi-journal-text"
                };

                // Thêm vào database
                _context.AdminMenus.Add(documentMenu);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm menu Tài liệu vào thanh điều hướng.";
            }
            else
            {
                TempData["InfoMessage"] = "Menu Tài liệu đã tồn tại.";
            }

            // Chuyển hướng đến trang quản trị
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }

        // GET: /AdminMenu/AddDocumentStatisticsMenuItem
        public async Task<IActionResult> AddDocumentStatisticsMenuItem()
        {
            // Tìm menu Document
            var documentMenu = await _context.AdminMenus
                .FirstOrDefaultAsync(m => m.ControllerName == "Document" && m.MenuName == "Tài liệu" && m.AreaName == "Admin");

            if (documentMenu == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy menu Tài liệu. Vui lòng thêm menu Tài liệu trước.";
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            // Kiểm tra xem menu item Statistics đã tồn tại chưa
            bool statisticsMenuExists = await _context.AdminMenus
                .AnyAsync(m => m.ControllerName == "Document" && m.ActionName == "Statistics" && m.AreaName == "Admin");

            if (!statisticsMenuExists)
            {
                // Tạo menu item Statistics
                var statisticsMenu = new AdminMenu
                {
                    MenuName = "Thống kê",
                    ItemLevel = 2,
                    IsActive = true,
                    ParentLevel = documentMenu.MenuID, // Là menu con của Document
                    ItemOrder = 2,
                    AreaName = "Admin",
                    ControllerName = "Document",
                    ActionName = "Statistics",
                    Icon = "bi bi-bar-chart"
                };

                // Thêm vào database
                _context.AdminMenus.Add(statisticsMenu);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm menu con Thống kê vào menu Tài liệu.";
            }
            else
            {
                TempData["InfoMessage"] = "Menu con Thống kê đã tồn tại.";
            }

            // Chuyển hướng đến trang quản trị
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }
        
        // GET: /AdminMenu/AddCategoryMenuItem
        public async Task<IActionResult> AddCategoryMenuItem()
        {
            // Kiểm tra xem menu quản lý danh mục đã tồn tại chưa
            bool categoryMenuExists = await _context.AdminMenus
                .AnyAsync(m => m.ControllerName == "Category" && m.AreaName == "Admin");

            if (!categoryMenuExists)
            {
                // Xác định vị trí để thêm menu mới (sau menu Document)
                int itemOrder = 4;
                
                // Tạo menu item Category
                var categoryMenu = new AdminMenu
                {
                    MenuName = "Quản lý danh mục",
                    ItemLevel = 1,
                    IsActive = true,
                    ParentLevel = 0, // Menu cấp cao nhất
                    ItemOrder = itemOrder,
                    AreaName = "Admin",
                    ControllerName = "Category",
                    ActionName = "Index",
                    Icon = "bi bi-folder"
                };

                // Thêm vào database
                _context.AdminMenus.Add(categoryMenu);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm menu Quản lý danh mục vào thanh điều hướng.";
            }
            else
            {
                TempData["InfoMessage"] = "Menu Quản lý danh mục đã tồn tại.";
            }

            // Chuyển hướng đến trang quản trị
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }
        
        // GET: /AdminMenu/AddAuthorMenuItem
        public async Task<IActionResult> AddAuthorMenuItem()
        {
            // Kiểm tra xem menu quản lý tác giả đã tồn tại chưa
            bool authorMenuExists = await _context.AdminMenus
                .AnyAsync(m => m.ControllerName == "Author" && m.AreaName == "Admin");

            if (!authorMenuExists)
            {
                // Xác định vị trí để thêm menu mới (sau menu Category)
                int itemOrder = 5;
                
                // Tạo menu item Author
                var authorMenu = new AdminMenu
                {
                    MenuName = "Quản lý tác giả",
                    ItemLevel = 1,
                    IsActive = true,
                    ParentLevel = 0, // Menu cấp cao nhất
                    ItemOrder = itemOrder,
                    AreaName = "Admin",
                    ControllerName = "Author",
                    ActionName = "Index",
                    Icon = "bi bi-people"
                };

                // Thêm vào database
                _context.AdminMenus.Add(authorMenu);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm menu Quản lý tác giả vào thanh điều hướng.";
            }
            else
            {
                TempData["InfoMessage"] = "Menu Quản lý tác giả đã tồn tại.";
            }

            // Chuyển hướng đến trang quản trị
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }
        
        // GET: /AdminMenu/AddUserActivityStatisticsMenuItem
        public async Task<IActionResult> AddUserActivityStatisticsMenuItem()
        {
            // Kiểm tra xem menu thống kê chính đã tồn tại chưa
            var statisticsMainMenu = await _context.AdminMenus
                .FirstOrDefaultAsync(m => m.ControllerName == "Statistics" && m.AreaName == "Admin" && m.ParentLevel == 0);

            if (statisticsMainMenu == null)
            {
                // Tạo menu thống kê chính
                statisticsMainMenu = new AdminMenu
                {
                    MenuName = "Thống kê",
                    ItemLevel = 1,
                    IsActive = true,
                    ParentLevel = 0, // Menu cấp cao nhất
                    ItemOrder = 7, // Đặt sau các menu khác
                    AreaName = "Admin",
                    ControllerName = "Statistics",
                    ActionName = "Index",
                    Icon = "bi bi-bar-chart-line",
                    IdName = "statistics-nav"
                };

                // Thêm vào database
                _context.AdminMenus.Add(statisticsMainMenu);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Đã thêm menu Thống kê vào thanh điều hướng.";
            }

            // Kiểm tra xem menu hoạt động người dùng đã tồn tại chưa
            bool userActivityMenuExists = await _context.AdminMenus
                .AnyAsync(m => m.ControllerName == "Statistics" && m.ActionName == "UserActivity" && m.AreaName == "Admin");

            if (!userActivityMenuExists)
            {
                // Tạo menu con thống kê hoạt động người dùng
                var userActivityMenu = new AdminMenu
                {
                    MenuName = "Hoạt động người dùng",
                    ItemLevel = 2,
                    IsActive = true,
                    ParentLevel = statisticsMainMenu.MenuID, // Là menu con của Thống kê
                    ItemOrder = 1, // Đặt đầu tiên trong danh sách con
                    AreaName = "Admin",
                    ControllerName = "Statistics",
                    ActionName = "UserActivity",
                    Icon = "bi bi-people-fill"
                };

                // Thêm vào database
                _context.AdminMenus.Add(userActivityMenu);
                
                // Thêm các menu con khác nếu chưa có
                if (!await _context.AdminMenus.AnyAsync(m => m.ControllerName == "Statistics" && m.ActionName == "Downloads" && m.AreaName == "Admin"))
                {
                    var downloadsMenu = new AdminMenu
                    {
                        MenuName = "Thống kê tải xuống",
                        ItemLevel = 2,
                        IsActive = true,
                        ParentLevel = statisticsMainMenu.MenuID,
                        ItemOrder = 2,
                        AreaName = "Admin",
                        ControllerName = "Statistics",
                        ActionName = "Downloads",
                        Icon = "bi bi-download"
                    };
                    _context.AdminMenus.Add(downloadsMenu);
                }
                
                if (!await _context.AdminMenus.AnyAsync(m => m.ControllerName == "Statistics" && m.ActionName == "Comments" && m.AreaName == "Admin"))
                {
                    var commentsMenu = new AdminMenu
                    {
                        MenuName = "Thống kê bình luận",
                        ItemLevel = 2,
                        IsActive = true,
                        ParentLevel = statisticsMainMenu.MenuID,
                        ItemOrder = 3,
                        AreaName = "Admin",
                        ControllerName = "Statistics",
                        ActionName = "Comments",
                        Icon = "bi bi-chat-dots"
                    };
                    _context.AdminMenus.Add(commentsMenu);
                }
                
                if (!await _context.AdminMenus.AnyAsync(m => m.ControllerName == "Statistics" && m.ActionName == "Ratings" && m.AreaName == "Admin"))
                {
                    var ratingsMenu = new AdminMenu
                    {
                        MenuName = "Thống kê đánh giá",
                        ItemLevel = 2,
                        IsActive = true,
                        ParentLevel = statisticsMainMenu.MenuID,
                        ItemOrder = 4,
                        AreaName = "Admin",
                        ControllerName = "Statistics",
                        ActionName = "Ratings",
                        Icon = "bi bi-star"
                    };
                    _context.AdminMenus.Add(ratingsMenu);
                }
                
                if (!await _context.AdminMenus.AnyAsync(m => m.ControllerName == "Statistics" && m.ActionName == "Favorites" && m.AreaName == "Admin"))
                {
                    var favoritesMenu = new AdminMenu
                    {
                        MenuName = "Thống kê yêu thích",
                        ItemLevel = 2,
                        IsActive = true,
                        ParentLevel = statisticsMainMenu.MenuID,
                        ItemOrder = 5,
                        AreaName = "Admin",
                        ControllerName = "Statistics",
                        ActionName = "Favorites",
                        Icon = "bi bi-heart"
                    };
                    _context.AdminMenus.Add(favoritesMenu);
                }
                
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm menu Thống kê hoạt động người dùng vào thanh điều hướng.";
            }
            else
            {
                TempData["InfoMessage"] = "Menu Thống kê hoạt động người dùng đã tồn tại.";
            }

            // Chuyển hướng đến trang quản trị
            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }
    }
} 