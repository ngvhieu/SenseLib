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
using Microsoft.Extensions.Logging;

namespace SenseLib.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SlideshowController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<SlideshowController> _logger;

        public SlideshowController(DataContext context, IWebHostEnvironment hostEnvironment, ILogger<SlideshowController> logger)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
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
                // Kiểm tra tiêu đề không được để trống
                if (string.IsNullOrEmpty(slideshow.Title))
                {
                    ModelState.AddModelError("Title", "Tiêu đề không được để trống");
                    _logger.LogWarning("Tiêu đề trống khi tạo slideshow mới");
                    return View(slideshow);
                }

                // Kiểm tra độ dài tiêu đề
                if (slideshow.Title.Length > 255)
                {
                    ModelState.AddModelError("Title", "Tiêu đề không được vượt quá 255 ký tự");
                    _logger.LogWarning("Tiêu đề quá dài khi tạo slideshow mới");
                    return View(slideshow);
                }

                // Kiểm tra độ dài mô tả
                if (slideshow.Description != null && slideshow.Description.Length > 500)
                {
                    ModelState.AddModelError("Description", "Mô tả không được vượt quá 500 ký tự");
                    _logger.LogWarning("Mô tả quá dài khi tạo slideshow mới");
                    return View(slideshow);
                }

                // Kiểm tra độ dài link
                if (slideshow.Link != null && slideshow.Link.Length > 255)
                {
                    ModelState.AddModelError("Link", "Liên kết không được vượt quá 255 ký tự");
                    _logger.LogWarning("Liên kết quá dài khi tạo slideshow mới");
                    return View(slideshow);
                }

                // Xử lý upload hình ảnh
                if (imageFile == null || imageFile.Length == 0)
                {
                    ViewBag.ErrorMessage = "Vui lòng chọn ảnh cho slideshow";
                    _logger.LogWarning("Không có file ảnh khi tạo slideshow mới");
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
        public async Task<IActionResult> Edit(int id, Slideshow slideshow, IFormFile imageFile)
        {
            try
            {
                _logger.LogInformation($"[DEBUG] Edit POST nhận được: ID={id}, Title='{slideshow.Title}', IsActive={slideshow.IsActive}");
                
                if (id != slideshow.SlideID)
                {
                    return NotFound();
                }

                var dbSlideshow = await _context.Slideshows.FindAsync(id);
                if (dbSlideshow == null)
                {
                    return NotFound();
                }

                _logger.LogInformation($"[DEBUG] Trạng thái hiện tại trong DB: Title='{dbSlideshow.Title}', IsActive={dbSlideshow.IsActive}");

                // Cập nhật các trường cơ bản
                dbSlideshow.Title = slideshow.Title;
                dbSlideshow.Description = slideshow.Description;
                dbSlideshow.Link = slideshow.Link;
                dbSlideshow.DisplayOrder = slideshow.DisplayOrder > 0 ? slideshow.DisplayOrder : 1;
                dbSlideshow.IsActive = slideshow.IsActive;
                
                // Giữ lại ngày tạo
                dbSlideshow.CreatedDate = dbSlideshow.CreatedDate;

                // Xử lý hình ảnh mới nếu có
                if (imageFile != null && imageFile.Length > 0)
                {
                    var oldImagePath = dbSlideshow.ImagePath;
                    
                    // Lưu hình ảnh mới
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "slideshow");
                    
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    
                    // Cập nhật đường dẫn mới
                    dbSlideshow.ImagePath = "/uploads/slideshow/" + fileName;
                    
                    // Xóa hình ảnh cũ
                    try
                    {
                        var oldPath = Path.Combine(_hostEnvironment.WebRootPath, oldImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi xóa ảnh cũ: {ex.Message}");
                    }
                }

                _logger.LogInformation($"[DEBUG] Trước khi lưu: Title='{dbSlideshow.Title}', IsActive={dbSlideshow.IsActive}");
                
                // Lưu thay đổi
                _context.Update(dbSlideshow);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"[DEBUG] Đã lưu thành công");
                
                TempData["SuccessMessage"] = "Đã cập nhật slideshow thành công!";
                return RedirectToAction(nameof(Edit), new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Lỗi khi cập nhật slideshow: {ex}");
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                return View(slideshow);
            }
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