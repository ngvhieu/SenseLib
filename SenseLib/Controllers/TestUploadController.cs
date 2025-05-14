using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SenseLib.Controllers
{
    public class TestUploadController : Controller
    {
        private readonly IWebHostEnvironment _hostEnvironment;

        public TestUploadController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            // Check if the uploads directory exists
            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "documents");
            bool uploadsFolderExists = Directory.Exists(uploadsFolder);
            
            // Get web root path
            string webRootPath = _hostEnvironment.WebRootPath;
            bool webRootExists = Directory.Exists(webRootPath);
            
            ViewBag.WebRootPath = webRootPath;
            ViewBag.WebRootExists = webRootExists;
            ViewBag.UploadsFolder = uploadsFolder;
            ViewBag.UploadsFolderExists = uploadsFolderExists;
            
            // Try to create a test file to check write permissions
            string testFilePath = Path.Combine(uploadsFolder, "test_permission.txt");
            try
            {
                System.IO.File.WriteAllText(testFilePath, "Test write permission");
                ViewBag.WritePermissionMessage = "Có quyền ghi vào thư mục uploads/documents";
                ViewBag.HasWritePermission = true;
                
                // Clean up
                try
                {
                    System.IO.File.Delete(testFilePath);
                }
                catch {}
            }
            catch (Exception ex)
            {
                ViewBag.WritePermissionMessage = $"Không có quyền ghi vào thư mục: {ex.Message}";
                ViewBag.HasWritePermission = false;
            }
            
            ViewBag.Message = "Sử dụng form bên dưới để kiểm tra chức năng upload file";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            try
            {
                if (file != null && file.Length > 0)
                {
                    // Lấy đường dẫn đến thư mục uploads/documents
                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "documents");
                    
                    // Thông tin cấu trúc thư mục
                    string webRootPath = _hostEnvironment.WebRootPath;
                    bool webRootExists = Directory.Exists(webRootPath);
                    bool uploadsFolderExists = Directory.Exists(uploadsFolder);
                    
                    // Tạo thư mục nếu chưa tồn tại
                    if (!uploadsFolderExists)
                    {
                        try
                        {
                            Directory.CreateDirectory(uploadsFolder);
                            uploadsFolderExists = Directory.Exists(uploadsFolder);
                        }
                        catch (Exception ex)
                        {
                            return Content($"Lỗi khi tạo thư mục: {ex.Message}");
                        }
                    }
                    
                    // Tạo tên file duy nhất
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    // Lưu file
                    try
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        
                        // Kiểm tra file đã được tạo chưa
                        bool fileCreated = System.IO.File.Exists(filePath);
                        
                        return Content($"Upload thành công. Thông tin:<br>" +
                            $"File gốc: {file.FileName}<br>" +
                            $"Kích thước: {file.Length} bytes<br>" +
                            $"Content Type: {file.ContentType}<br>" +
                            $"WebRootPath: {webRootPath}<br>" +
                            $"WebRootPath tồn tại: {webRootExists}<br>" +
                            $"uploadsFolder: {uploadsFolder}<br>" +
                            $"uploadsFolder tồn tại: {uploadsFolderExists}<br>" +
                            $"Đường dẫn file: {filePath}<br>" +
                            $"File đã được tạo: {fileCreated}");
                    }
                    catch (Exception ex)
                    {
                        // Chi tiết lỗi
                        string errorDetails = $"Lỗi: {ex.Message}<br>";
                        
                        if (ex.InnerException != null)
                        {
                            errorDetails += $"InnerException: {ex.InnerException.Message}<br>";
                        }
                        
                        errorDetails += $"StackTrace: {ex.StackTrace}<br>";
                        
                        return Content($"Lỗi khi lưu file: {errorDetails} <br>Đường dẫn file: {filePath}");
                    }
                }
                
                return Content("Không có file nào được chọn");
            }
            catch (Exception ex)
            {
                return Content($"Lỗi: {ex.Message}");
            }
        }
    }
} 