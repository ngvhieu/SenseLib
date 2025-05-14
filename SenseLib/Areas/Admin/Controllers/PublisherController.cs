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
    public class PublisherController : Controller
    {
        private readonly DataContext _context;

        public PublisherController(DataContext context)
        {
            _context = context;
        }

        // GET: Admin/Publisher
        public async Task<IActionResult> Index()
        {
            var publishers = await _context.Publishers
                .Select(p => new
                {
                    Publisher = p,
                    DocumentCount = p.Documents.Count
                })
                .OrderBy(p => p.Publisher.PublisherName)
                .ToListAsync();
                
            ViewBag.TotalPublishers = publishers.Count;
            
            // Chuyển đổi kết quả về danh sách nhà xuất bản và thêm thông tin số tài liệu
            var result = publishers.Select(p => p.Publisher).ToList();
            ViewBag.DocumentCounts = publishers.ToDictionary(p => p.Publisher.PublisherID, p => p.DocumentCount);
            
            return View(result);
        }

        // GET: Admin/Publisher/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publisher = await _context.Publishers
                .Include(p => p.Documents)
                    .ThenInclude(d => d.Category)
                .Include(p => p.Documents)
                    .ThenInclude(d => d.Author)
                .FirstOrDefaultAsync(m => m.PublisherID == id);
                
            if (publisher == null)
            {
                return NotFound();
            }
            
            // Đảm bảo không bị lỗi null reference
            if (publisher.Documents == null)
            {
                publisher.Documents = new List<Document>();
            }

            return View(publisher);
        }

        // GET: Admin/Publisher/Create
        public IActionResult Create()
        {
            var publisher = new Publisher
            {
                Address = string.Empty,
                Phone = string.Empty,
                Documents = new List<Document>()
            };
            return View(publisher);
        }

        // POST: Admin/Publisher/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PublisherName,Address,Phone")] Publisher publisher)
        {
            try
            {
                // Đảm bảo các trường không null
                if (publisher.Address == null)
                {
                    publisher.Address = string.Empty;
                }
                
                if (publisher.Phone == null)
                {
                    publisher.Phone = string.Empty;
                }
                
                // Đảm bảo Documents được khởi tạo
                if (publisher.Documents == null)
                {
                    publisher.Documents = new List<Document>();
                }

                if (ModelState.IsValid)
                {
                    _context.Add(publisher);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã thêm nhà xuất bản mới thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi lưu: " + ex.Message);
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.ToString()}");
            }
            return View(publisher);
        }

        // GET: Admin/Publisher/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher == null)
            {
                return NotFound();
            }
            
            // Đảm bảo không bị lỗi null reference
            if (publisher.Address == null)
            {
                publisher.Address = string.Empty;
            }
            
            if (publisher.Phone == null)
            {
                publisher.Phone = string.Empty;
            }
            
            if (publisher.Documents == null)
            {
                publisher.Documents = new List<Document>();
            }
            
            return View(publisher);
        }

        // POST: Admin/Publisher/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PublisherID,PublisherName,Address,Phone")] Publisher publisher)
        {
            if (id != publisher.PublisherID)
            {
                return NotFound();
            }
            
            // Đảm bảo không bị null reference
            if (publisher.Address == null)
            {
                publisher.Address = string.Empty;
            }
            
            if (publisher.Phone == null)
            {
                publisher.Phone = string.Empty;
            }
            
            if (publisher.Documents == null)
            {
                publisher.Documents = new List<Document>();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(publisher);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Đã cập nhật thông tin nhà xuất bản thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!PublisherExists(publisher.PublisherID))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi cập nhật nhà xuất bản: " + ex.Message);
                System.Diagnostics.Debug.WriteLine($"Exception: {ex.ToString()}");
            }
            
            return View(publisher);
        }

        // GET: Admin/Publisher/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var publisher = await _context.Publishers
                .FirstOrDefaultAsync(m => m.PublisherID == id);
                
            if (publisher == null)
            {
                return NotFound();
            }

            // Kiểm tra xem nhà xuất bản có tài liệu không
            bool hasDocuments = await _context.Documents
                .AnyAsync(d => d.PublisherID == id);
                
            ViewBag.HasDocuments = hasDocuments;
            if (hasDocuments)
            {
                ViewBag.DocumentCount = await _context.Documents
                    .CountAsync(d => d.PublisherID == id);
            }

            return View(publisher);
        }

        // POST: Admin/Publisher/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Kiểm tra xem nhà xuất bản có tài liệu không
            bool hasDocuments = await _context.Documents
                .AnyAsync(d => d.PublisherID == id);
                
            if (hasDocuments)
            {
                TempData["ErrorMessage"] = "Không thể xóa nhà xuất bản đang có tài liệu. Vui lòng chuyển tài liệu sang nhà xuất bản khác trước!";
                return RedirectToAction(nameof(Delete), new { id });
            }
            
            var publisher = await _context.Publishers.FindAsync(id);
            if (publisher != null)
            {
                _context.Publishers.Remove(publisher);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa nhà xuất bản thành công!";
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool PublisherExists(int id)
        {
            return _context.Publishers.Any(e => e.PublisherID == id);
        }
    }
} 