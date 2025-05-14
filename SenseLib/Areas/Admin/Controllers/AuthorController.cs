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
    public class AuthorController : Controller
    {
        private readonly DataContext _context;

        public AuthorController(DataContext context)
        {
            _context = context;
        }

        // GET: Admin/Author
        public async Task<IActionResult> Index()
        {
            var authors = await _context.Authors
                .Select(a => new
                {
                    Author = a,
                    DocumentCount = a.Documents.Count
                })
                .OrderBy(a => a.Author.AuthorName)
                .ToListAsync();
                
            ViewBag.TotalAuthors = authors.Count;
            
            // Chuyển đổi kết quả về danh sách tác giả và thêm thông tin số tài liệu
            var result = authors.Select(a => a.Author).ToList();
            ViewBag.DocumentCounts = authors.ToDictionary(a => a.Author.AuthorID, a => a.DocumentCount);
            
            return View(result);
        }

        // GET: Admin/Author/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors
                .Include(a => a.Documents)
                    .ThenInclude(d => d.Category)
                .FirstOrDefaultAsync(m => m.AuthorID == id);
                
            if (author == null)
            {
                return NotFound();
            }
            
            // Đảm bảo không bị lỗi null reference
            if (author.Documents == null)
            {
                author.Documents = new List<Document>();
            }

            return View(author);
        }

        // GET: Admin/Author/Create
        public IActionResult Create()
        {
            var author = new Author
            {
                Bio = string.Empty,
                Documents = new List<Document>()
            };
            return View(author);
        }

        // POST: Admin/Author/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AuthorName,Bio")] Author author)
        {
            try
            {
                // Nếu Bio là null thì gán thành chuỗi rỗng
                if (author.Bio == null)
                {
                    author.Bio = string.Empty;
                }
                
                // Đảm bảo Documents được khởi tạo
                if (author.Documents == null)
                {
                    author.Documents = new List<Document>();
                }

                if (ModelState.IsValid)
                {
                    _context.Add(author);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã thêm tác giả mới thành công!";
                    return RedirectToAction(nameof(Index));
                }
                
                // Log ra các lỗi modelstate để debug
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error: {error.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi lưu: " + ex.Message);
                // Log lỗi chi tiết
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.ToString()}");
            }
            return View(author);
        }

        // GET: Admin/Author/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors.FindAsync(id);
            if (author == null)
            {
                return NotFound();
            }
            
            // Đảm bảo không bị lỗi null reference
            if (author.Bio == null)
            {
                author.Bio = string.Empty;
            }
            
            if (author.Documents == null)
            {
                author.Documents = new List<Document>();
            }
            
            return View(author);
        }

        // POST: Admin/Author/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AuthorID,AuthorName,Bio")] Author author)
        {
            if (id != author.AuthorID)
            {
                return NotFound();
            }
            
            // Đảm bảo không bị null reference
            if (author.Bio == null)
            {
                author.Bio = string.Empty;
            }
            
            if (author.Documents == null)
            {
                author.Documents = new List<Document>();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(author);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Đã cập nhật thông tin tác giả thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!AuthorExists(author.AuthorID))
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
                ModelState.AddModelError(string.Empty, "Lỗi khi cập nhật tác giả: " + ex.Message);
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.ToString()}");
            }
            
            return View(author);
        }

        // GET: Admin/Author/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var author = await _context.Authors
                .FirstOrDefaultAsync(m => m.AuthorID == id);
                
            if (author == null)
            {
                return NotFound();
            }

            // Kiểm tra xem tác giả có tài liệu không
            bool hasDocuments = await _context.Documents
                .AnyAsync(d => d.AuthorID == id);
                
            ViewBag.HasDocuments = hasDocuments;
            if (hasDocuments)
            {
                ViewBag.DocumentCount = await _context.Documents
                    .CountAsync(d => d.AuthorID == id);
            }

            return View(author);
        }

        // POST: Admin/Author/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Kiểm tra xem tác giả có tài liệu không
            bool hasDocuments = await _context.Documents
                .AnyAsync(d => d.AuthorID == id);
                
            if (hasDocuments)
            {
                TempData["ErrorMessage"] = "Không thể xóa tác giả đang có tài liệu. Vui lòng chuyển tài liệu sang tác giả khác trước!";
                return RedirectToAction(nameof(Delete), new { id });
            }
            
            var author = await _context.Authors.FindAsync(id);
            if (author != null)
            {
                _context.Authors.Remove(author);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa tác giả thành công!";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool AuthorExists(int id)
        {
            return _context.Authors.Any(e => e.AuthorID == id);
        }
    }
} 