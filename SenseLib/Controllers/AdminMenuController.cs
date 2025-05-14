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
    }
} 