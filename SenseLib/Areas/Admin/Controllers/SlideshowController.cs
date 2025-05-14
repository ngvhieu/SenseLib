using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace SenseLib.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SlideshowController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public SlideshowController(DataContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Admin/Slideshow
        public async Task<IActionResult> Index()
        {
            return View(await _context.Slideshows.OrderBy(s => s.DisplayOrder).ToListAsync());
        }

        // GET: Admin/Slideshow/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slideshow = await _context.Slideshows
                .FirstOrDefaultAsync(m => m.SlideID == id);
            if (slideshow == null)
            {
                return NotFound();
            }

            return View(slideshow);
        }

        // GET: Admin/Slideshow/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Slideshow/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Slideshow slideshow, IFormFile imageFile)
        {
            try
            {
                // Kiểm tra trường bắt buộc
                if (string.IsNullOrEmpty(slideshow.Title))
                {
                    ViewBag.ErrorMessage = "Vui lòng nhập tiêu đề";
                    return View(slideshow);
                }

                // Xử lý upload hình ảnh
                if (imageFile == null || imageFile.Length == 0)
                {
                    ViewBag.ErrorMessage = "Vui lòng chọn ảnh cho slideshow";
                    return View(slideshow);
                }

                // Kiểm tra kích thước tối đa (5MB)
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ViewBag.ErrorMessage = "Kích thước ảnh không được vượt quá 5MB";
                    return View(slideshow);
                }
                
                // Kiểm tra định dạng file
                var extension = Path.GetExtension(imageFile.FileName);
                var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!validExtensions.Contains(extension.ToLower()))
                {
                    ViewBag.ErrorMessage = "Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .gif";
                    return View(slideshow);
                }
                
                string wwwRootPath = _hostEnvironment.WebRootPath;
                string uploadsFolder = Path.Combine(wwwRootPath, "uploads", "slideshow");
                
                // Tạo thư mục nếu không tồn tại
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                // Tạo tên file duy nhất để tránh trùng lặp
                string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, fileName);
                
                // Lưu file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }
                
                // Cập nhật thông tin slideshow
                slideshow.ImagePath = "/uploads/slideshow/" + fileName;
                slideshow.CreatedDate = DateTime.Now;
                if (slideshow.DisplayOrder <= 0)
                {
                    slideshow.DisplayOrder = 1;
                }
                
                // Lưu vào database
                _context.Add(slideshow);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Đã thêm slideshow mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Lỗi: {ex.Message}";
                return View(slideshow);
            }
        }

        // GET: Admin/Slideshow/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slideshow = await _context.Slideshows.FindAsync(id);
            if (slideshow == null)
            {
                return NotFound();
            }
            return View(slideshow);
        }

        // POST: Admin/Slideshow/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SlideID,Title,Description,ImagePath,Link,DisplayOrder,IsActive,CreatedDate")] Slideshow slideshow, IFormFile imageFile)
        {
            if (id != slideshow.SlideID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload hình ảnh mới nếu có
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Kiểm tra kích thước tối đa (5MB)
                        if (imageFile.Length > 5 * 1024 * 1024)
                        {
                            ViewBag.ErrorMessage = "Kích thước ảnh không được vượt quá 5MB";
                            return View(slideshow);
                        }
                        
                        // Kiểm tra định dạng file
                        var extension = Path.GetExtension(imageFile.FileName);
                        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        if (!validExtensions.Contains(extension.ToLower()))
                        {
                            ViewBag.ErrorMessage = "Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .gif";
                            return View(slideshow);
                        }
                    
                        // Xóa hình ảnh cũ nếu tồn tại
                        if (!string.IsNullOrEmpty(slideshow.ImagePath))
                        {
                            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, slideshow.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldImagePath);
                                }
                                catch (Exception ex)
                                {
                                    // Log lỗi nhưng vẫn tiếp tục
                                    Console.WriteLine($"Lỗi khi xóa ảnh cũ: {ex.Message}");
                                }
                            }
                        }

                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string uploadsFolder = Path.Combine(wwwRootPath, "uploads", "slideshow");
                        
                        // Tạo thư mục nếu không tồn tại
                        if (!Directory.Exists(uploadsFolder))
                        {
                            try
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }
                            catch (Exception ex)
                            {
                                ViewBag.ErrorMessage = $"Không thể tạo thư mục lưu trữ: {ex.Message}";
                                return View(slideshow);
                            }
                        }
                        
                        // Tạo tên file duy nhất để tránh trùng lặp
                        string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                        string filePath = Path.Combine(uploadsFolder, fileName);
                        
                        try
                        {
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(fileStream);
                            }
                            
                            slideshow.ImagePath = "/uploads/slideshow/" + fileName;
                        }
                        catch (Exception ex)
                        {
                            ViewBag.ErrorMessage = $"Lỗi khi lưu ảnh: {ex.Message}";
                            return View(slideshow);
                        }
                    }

                    _context.Update(slideshow);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Đã cập nhật slideshow thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SlideshowExists(slideshow.SlideID))
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
            return View(slideshow);
        }

        // GET: Admin/Slideshow/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var slideshow = await _context.Slideshows
                .FirstOrDefaultAsync(m => m.SlideID == id);
            if (slideshow == null)
            {
                return NotFound();
            }

            return View(slideshow);
        }

        // POST: Admin/Slideshow/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var slideshow = await _context.Slideshows.FindAsync(id);
            if (slideshow == null)
            {
                return NotFound();
            }
            
            // Xóa hình ảnh nếu tồn tại
            if (!string.IsNullOrEmpty(slideshow.ImagePath))
            {
                var imagePath = Path.Combine(_hostEnvironment.WebRootPath, slideshow.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    try
                    {
                        System.IO.File.Delete(imagePath);
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nhưng vẫn tiếp tục
                        Console.WriteLine($"Lỗi khi xóa ảnh: {ex.Message}");
                    }
                }
            }
            
            _context.Slideshows.Remove(slideshow);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Đã xóa slideshow thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool SlideshowExists(int id)
        {
            return _context.Slideshows.Any(e => e.SlideID == id);
        }
    }
} 