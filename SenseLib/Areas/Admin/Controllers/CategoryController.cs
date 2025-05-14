using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace SenseLib.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly DataContext _context;

        public CategoryController(DataContext context)
        {
            _context = context;
        }

        // GET: Admin/Category
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Select(c => new
                {
                    Category = c,
                    DocumentCount = c.Documents.Count
                })
                .OrderBy(c => c.Category.CategoryName)
                .ToListAsync();
                
            ViewBag.TotalCategories = categories.Count;
            ViewBag.ActiveCategories = categories.Count(c => c.Category.Status == "Active");
            
            return View(categories.Select(c => c.Category).ToList());
        }

        // GET: Admin/Category/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .Include(c => c.Documents)
                    .ThenInclude(d => d.Author)
                .FirstOrDefaultAsync(m => m.CategoryID == id);
                
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Admin/Category/Create
        public IActionResult Create()
        {
            var category = new Category
            {
                Description = string.Empty,
                Status = "Active", // Đặt giá trị mặc định là Active
                Documents = new List<Document>()
            };
            return View(category);
        }

        // POST: Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryName,Description,Status")] Category category)
        {
            try
            {
                // Nếu Description là null thì gán thành chuỗi rỗng
                if (category.Description == null)
                {
                    category.Description = string.Empty;
                }
                
                // Đảm bảo Documents được khởi tạo
                if (category.Documents == null)
                {
                    category.Documents = new List<Document>();
                }

                // Log trạng thái hiện tại của model để debug
                System.Diagnostics.Debug.WriteLine($"CategoryName: {category.CategoryName}");
                System.Diagnostics.Debug.WriteLine($"Description: {category.Description}");
                System.Diagnostics.Debug.WriteLine($"Status: {category.Status}");
                System.Diagnostics.Debug.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

                if (ModelState.IsValid)
                {
                    _context.Add(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã thêm danh mục mới thành công!";
                    return RedirectToAction(nameof(Index));
                }

                // Log ra các lỗi modelstate để debug
                foreach (var key in ModelState.Keys)
                {
                    var modelState = ModelState[key];
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Key: {key}, Error: {error.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi lưu danh mục: " + ex.Message);
                // Log lỗi chi tiết
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.ToString()}");
            }
            
            return View(category);
        }

        // GET: Admin/Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            
            // Đảm bảo không bị lỗi null reference
            if (category.Documents == null)
            {
                category.Documents = new List<Document>();
            }
            
            return View(category);
        }

        // POST: Admin/Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryID,CategoryName,Description,Status")] Category category)
        {
            if (id != category.CategoryID)
            {
                return NotFound();
            }
            
            // Đảm bảo không bị null reference
            if (category.Description == null)
            {
                category.Description = string.Empty;
            }
            
            if (category.Documents == null)
            {
                category.Documents = new List<Document>();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(category);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Đã cập nhật danh mục thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!CategoryExists(category.CategoryID))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                
                // Log ra các lỗi modelstate để debug
                foreach (var key in ModelState.Keys)
                {
                    var modelState = ModelState[key];
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Key: {key}, Error: {error.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi cập nhật danh mục: " + ex.Message);
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.ToString()}");
            }
            
            return View(category);
        }

        // GET: Admin/Category/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.CategoryID == id);
                
            if (category == null)
            {
                return NotFound();
            }

            // Kiểm tra xem danh mục có đang được sử dụng không
            bool isInUse = await _context.Documents
                .AnyAsync(d => d.CategoryID == id);
                
            ViewBag.IsInUse = isInUse;
            if (isInUse)
            {
                ViewBag.DocumentCount = await _context.Documents
                    .CountAsync(d => d.CategoryID == id);
            }

            return View(category);
        }

        // POST: Admin/Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Kiểm tra xem danh mục có đang được sử dụng không
            bool isInUse = await _context.Documents
                .AnyAsync(d => d.CategoryID == id);
                
            if (isInUse)
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục đang được sử dụng. Vui lòng chuyển tài liệu sang danh mục khác trước!";
                return RedirectToAction(nameof(Delete), new { id });
            }
            
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa danh mục thành công!";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryID == id);
        }
    }
} 