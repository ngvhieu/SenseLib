using System;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SenseLib.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SystemCheckController : Controller
    {
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<SystemCheckController> _logger;

        public SystemCheckController(IWebHostEnvironment hostEnvironment, ILogger<SystemCheckController> logger)
        {
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = new SystemStatusViewModel();
            
            // Kiểm tra WebRootPath
            model.WebRootPath = _hostEnvironment.WebRootPath;
            model.WebRootExists = !string.IsNullOrEmpty(_hostEnvironment.WebRootPath) && Directory.Exists(_hostEnvironment.WebRootPath);
            
            // Kiểm tra cấu trúc thư mục uploads
            string uploadsPath = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
            model.UploadsPath = uploadsPath;
            model.UploadsExists = Directory.Exists(uploadsPath);
            
            // Kiểm tra cấu trúc thư mục uploads/documents
            string documentsPath = Path.Combine(uploadsPath, "documents");
            model.DocumentsPath = documentsPath;
            model.DocumentsExists = Directory.Exists(documentsPath);
            
            // Kiểm tra quyền ghi
            model.HasWritePermission = CheckWritePermission(documentsPath);
            
            return View(model);
        }
        
        [HttpPost]
        public IActionResult FixDirectories()
        {
            try
            {
                if (string.IsNullOrEmpty(_hostEnvironment.WebRootPath) || !Directory.Exists(_hostEnvironment.WebRootPath))
                {
                    return Json(new { success = false, message = "WebRootPath không tồn tại hoặc không có quyền truy cập." });
                }
                
                // Tạo thư mục uploads nếu chưa tồn tại
                string uploadsPath = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                    _logger.LogInformation("Đã tạo thư mục uploads");
                }
                
                // Tạo thư mục uploads/documents nếu chưa tồn tại
                string documentsPath = Path.Combine(uploadsPath, "documents");
                if (!Directory.Exists(documentsPath))
                {
                    Directory.CreateDirectory(documentsPath);
                    _logger.LogInformation("Đã tạo thư mục uploads/documents");
                }
                
                // Kiểm tra quyền ghi
                bool hasWritePermission = CheckWritePermission(documentsPath);
                
                return Json(new { 
                    success = true,
                    hasWritePermission = hasWritePermission,
                    message = hasWritePermission 
                        ? "Đã tạo thư mục uploads/documents và có quyền ghi." 
                        : "Đã tạo thư mục uploads/documents nhưng không có quyền ghi."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thư mục uploads/documents");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
        
        private bool CheckWritePermission(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return false;
            }
            
            try
            {
                string testFilePath = Path.Combine(path, "write_permission_test.txt");
                System.IO.File.WriteAllText(testFilePath, "Test write permission");
                System.IO.File.Delete(testFilePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không có quyền ghi vào thư mục {Path}", path);
                return false;
            }
        }
    }
    
    public class SystemStatusViewModel
    {
        public string WebRootPath { get; set; }
        public bool WebRootExists { get; set; }
        public string UploadsPath { get; set; } 
        public bool UploadsExists { get; set; }
        public string DocumentsPath { get; set; }
        public bool DocumentsExists { get; set; }
        public bool HasWritePermission { get; set; }
    }
} 