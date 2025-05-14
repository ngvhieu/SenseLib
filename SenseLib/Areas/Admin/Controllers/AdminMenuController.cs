using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SenseLib.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminMenuController : Controller
    {
        private readonly DataContext _context;

        public AdminMenuController(DataContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminMenu
        public async Task<IActionResult> Index()
        {
            var menuItems = await _context.AdminMenus
                .OrderBy(m => m.ItemLevel)
                .ThenBy(m => m.ItemOrder)
                .ToListAsync();
                
            // Lấy danh sách các menu cha để hiển thị tên
            var parentMenuDict = menuItems
                .Where(m => m.ParentLevel == 0)
                .ToDictionary(m => m.MenuID, m => m.MenuName);
                
            // Thêm item "Không có" cho menu cha
            parentMenuDict[0] = "Không có";
                
            ViewBag.ParentMenus = parentMenuDict;
            ViewBag.TotalMenuItems = menuItems.Count;
            ViewBag.ActiveMenuItems = menuItems.Count(m => m.IsActive);
            
            return View(menuItems);
        }

        // GET: Admin/AdminMenu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _context.AdminMenus
                .FirstOrDefaultAsync(m => m.MenuID == id);
                
            if (menuItem == null)
            {
                return NotFound();
            }

            // Lấy thông tin menu cha nếu có
            if (menuItem.ParentLevel > 0)
            {
                var parentMenu = await _context.AdminMenus
                    .FirstOrDefaultAsync(m => m.MenuID == menuItem.ParentLevel);
                    
                if (parentMenu != null)
                {
                    ViewBag.ParentMenuName = parentMenu.MenuName;
                }
            }
            else
            {
                ViewBag.ParentMenuName = "Không có";
            }

            return View(menuItem);
        }

        // GET: Admin/AdminMenu/Create
        public async Task<IActionResult> Create()
        {
            // Lấy danh sách menu cha (parent level = 0)
            var parentMenus = await _context.AdminMenus
                .Where(m => m.ParentLevel == 0)
                .OrderBy(m => m.ItemOrder)
                .ToListAsync();
                
            ViewBag.ParentMenus = parentMenus;
            
            // Lấy ItemOrder mặc định
            int nextOrder = 1;
            var lastItem = await _context.AdminMenus
                .OrderByDescending(m => m.ItemOrder)
                .FirstOrDefaultAsync();
                
            if (lastItem != null)
            {
                nextOrder = lastItem.ItemOrder + 1;
            }
            
            var menuItem = new AdminMenu
            {
                IsActive = true,
                ItemLevel = 1,
                ParentLevel = 0,
                ItemOrder = nextOrder
            };
            
            return View(menuItem);
        }

        // POST: Admin/AdminMenu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MenuName,ItemLevel,IsActive,ParentLevel,ItemOrder,ItemTarget,AreaName,ControllerName,ActionName,Icon,IdName")] AdminMenu menuItem)
        {
            if (ModelState.IsValid)
            {
                _context.Add(menuItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã thêm menu mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            
            // Lấy danh sách menu cha (parent level = 0)
            var parentMenus = await _context.AdminMenus
                .Where(m => m.ParentLevel == 0)
                .OrderBy(m => m.ItemOrder)
                .ToListAsync();
                
            ViewBag.ParentMenus = parentMenus;
            
            return View(menuItem);
        }

        // GET: Admin/AdminMenu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _context.AdminMenus.FindAsync(id);
            if (menuItem == null)
            {
                return NotFound();
            }
            
            // Lấy danh sách menu cha (parent level = 0), ngoại trừ chính item này
            var parentMenus = await _context.AdminMenus
                .Where(m => m.ParentLevel == 0 && m.MenuID != id)
                .OrderBy(m => m.ItemOrder)
                .ToListAsync();
                
            ViewBag.ParentMenus = parentMenus;
            
            return View(menuItem);
        }

        // POST: Admin/AdminMenu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MenuID,MenuName,ItemLevel,IsActive,ParentLevel,ItemOrder,ItemTarget,AreaName,ControllerName,ActionName,Icon,IdName")] AdminMenu menuItem)
        {
            if (id != menuItem.MenuID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(menuItem);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật menu thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AdminMenuExists(menuItem.MenuID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            
            // Lấy danh sách menu cha (parent level = 0), ngoại trừ chính item này
            var parentMenus = await _context.AdminMenus
                .Where(m => m.ParentLevel == 0 && m.MenuID != id)
                .OrderBy(m => m.ItemOrder)
                .ToListAsync();
                
            ViewBag.ParentMenus = parentMenus;
            
            return View(menuItem);
        }

        // GET: Admin/AdminMenu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menuItem = await _context.AdminMenus
                .FirstOrDefaultAsync(m => m.MenuID == id);
                
            if (menuItem == null)
            {
                return NotFound();
            }

            // Kiểm tra xem có menu con không
            bool hasChildren = await _context.AdminMenus
                .AnyAsync(m => m.ParentLevel == id);
                
            if (hasChildren)
            {
                ViewBag.HasChildren = true;
                ViewBag.ChildCount = await _context.AdminMenus
                    .CountAsync(m => m.ParentLevel == id);
            }

            return View(menuItem);
        }

        // POST: Admin/AdminMenu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Kiểm tra xem có menu con không
            bool hasChildren = await _context.AdminMenus
                .AnyAsync(m => m.ParentLevel == id);
                
            if (hasChildren)
            {
                TempData["ErrorMessage"] = "Không thể xóa menu có chứa menu con. Vui lòng xóa menu con trước!";
                return RedirectToAction(nameof(Delete), new { id });
            }
            
            var menuItem = await _context.AdminMenus.FindAsync(id);
            if (menuItem != null)
            {
                _context.AdminMenus.Remove(menuItem);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa menu thành công!";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool AdminMenuExists(int id)
        {
            return _context.AdminMenus.Any(e => e.MenuID == id);
        }
    }
} 