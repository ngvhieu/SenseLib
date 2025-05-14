using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SenseLib.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class FileManagerController : Controller
    {
        private readonly IWebHostEnvironment _hostEnvironment;

        public FileManagerController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Browser()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile upload, string CKEditorFuncNum, string CKEditor, string langCode)
        {
            if (upload == null || upload.Length == 0)
            {
                return Json(new { error = new { message = "Không có file nào được chọn" } });
            }

            try
            {
                // Đảm bảo file có định dạng hợp lệ
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt" };
                var fileExtension = Path.GetExtension(upload.FileName).ToLowerInvariant();

                if (!Array.Exists(allowedExtensions, e => e == fileExtension))
                {
                    return Json(new { error = new { message = "Định dạng file không được hỗ trợ" } });
                }

                // Đảm bảo kích thước file hợp lệ (20MB)
                if (upload.Length > 20 * 1024 * 1024)
                {
                    return Json(new { error = new { message = "Kích thước file không được vượt quá 20MB" } });
                }

                // Tạo thư mục lưu trữ nếu chưa tồn tại
                string uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                // Tạo tên file duy nhất
                string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(upload.FileName);
                string filePath = Path.Combine(uploadDir, fileName);
                
                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await upload.CopyToAsync(stream);
                }

                // Tạo URL của file
                var url = $"/uploads/{fileName}";

                // Nếu là yêu cầu từ CKEditor
                if (!string.IsNullOrEmpty(CKEditorFuncNum))
                {
                    return Content($"<script>window.parent.CKEDITOR.tools.callFunction({CKEditorFuncNum}, '{url}', '');</script>", "text/html");
                }

                // Trả về đường dẫn file cho trình soạn thảo Summernote
                return Json(new { url });
            }
            catch (Exception ex)
            {
                return Json(new { error = new { message = ex.Message } });
            }
        }

        [HttpPost]
        public IActionResult ListFiles()
        {
            try
            {
                string uploadDir = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadDir))
                {
                    return Json(new List<object>());
                }

                var files = Directory.GetFiles(uploadDir);
                var fileList = new List<object>();

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    var url = $"/uploads/{fileInfo.Name}";
                    
                    fileList.Add(new
                    {
                        name = fileInfo.Name,
                        size = fileInfo.Length,
                        url = url,
                        thumbnailUrl = GetThumbnailUrl(url, fileInfo.Extension),
                        deleteUrl = Url.Action("DeleteFile", new { fileName = fileInfo.Name }),
                        deleteType = "POST"
                    });
                }

                return Json(fileList);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteFile(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return Json(new { success = false, message = "Tên file không hợp lệ" });
                }

                string filePath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", fileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return Json(new { success = false, message = "File không tồn tại" });
                }

                System.IO.File.Delete(filePath);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GetThumbnailUrl(string url, string extension)
        {
            // Nếu là hình ảnh thì dùng chính nó làm thumbnail
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
            if (Array.Exists(imageExtensions, e => e.Equals(extension, StringComparison.OrdinalIgnoreCase)))
            {
                return url;
            }

            // Với các loại file khác, sử dụng icon mặc định
            return "/img/file-icons/file.png";
        }
    }
} 