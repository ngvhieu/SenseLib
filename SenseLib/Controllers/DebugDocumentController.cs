using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;

namespace SenseLib.Controllers
{
    public class DebugDocumentController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public DebugDocumentController(DataContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Authors = await _context.Authors.ToListAsync();
            ViewBag.Publishers = await _context.Publishers.ToListAsync();
            
            return View();
        }
        
        public async Task<IActionResult> CreateTestDocument()
        {
            try
            {
                // Tạo file test trước
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "documents");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                string fileName = "test_document.txt";
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + fileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                
                // Tạo một file text đơn giản
                try 
                {
                    System.IO.File.WriteAllText(filePath, "This is a test document content.");
                } 
                catch (Exception ex)
                {
                    return Content($"Lỗi khi tạo file test: {ex.Message}");
                }
                
                // Tạo object Document
                var document = new Document
                {
                    Title = "Test Document " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = "Tài liệu test tạo từ DebugDocumentController",
                    // Lấy category, author, publisher đầu tiên nếu có
                    CategoryID = await _context.Categories.Select(c => c.CategoryID).FirstOrDefaultAsync(),
                    AuthorID = await _context.Authors.Select(a => a.AuthorID).FirstOrDefaultAsync(),
                    PublisherID = await _context.Publishers.Select(p => p.PublisherID).FirstOrDefaultAsync(),
                    UploadDate = DateTime.Now,
                    FilePath = "/uploads/documents/" + uniqueFileName,
                    Status = "Published"
                };
                
                // Xóa bỏ các ID null nếu không có dữ liệu
                if (document.CategoryID == 0) document.CategoryID = null;
                if (document.AuthorID == 0) document.AuthorID = null;
                if (document.PublisherID == 0) document.PublisherID = null;
                
                // Lưu vào database
                try
                {
                    _context.Documents.Add(document);
                    await _context.SaveChangesAsync();
                    
                    return Content($"Tạo tài liệu thành công!<br>" +
                        $"ID: {document.DocumentID}<br>" +
                        $"Title: {document.Title}<br>" +
                        $"FilePath: {document.FilePath}<br>" +
                        $"<a href='/Admin/Document'>Đi đến trang quản lý tài liệu</a>"
                    );
                }
                catch (Exception ex)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        try { System.IO.File.Delete(filePath); } catch { }
                    }
                    
                    string errorMessage = $"Lỗi khi lưu document: {ex.Message}";
                    if (ex.InnerException != null)
                    {
                        errorMessage += $"<br>Inner Exception: {ex.InnerException.Message}";
                    }
                    
                    return Content(errorMessage);
                }
            }
            catch (Exception ex)
            {
                return Content($"Lỗi: {ex.Message}");
            }
        }
        
        public async Task<IActionResult> CreateMissingData()
        {
            try
            {
                // Kiểm tra và tạo Category nếu chưa có
                if (!await _context.Categories.AnyAsync())
                {
                    var category = new Category
                    {
                        CategoryName = "Danh mục mặc định",
                        Description = "Danh mục được tạo tự động"
                    };
                    
                    _context.Categories.Add(category);
                    await _context.SaveChangesAsync();
                }
                
                // Kiểm tra và tạo Author nếu chưa có
                if (!await _context.Authors.AnyAsync())
                {
                    var author = new Author
                    {
                        AuthorName = "Tác giả mặc định",
                        Bio = "Tác giả được tạo tự động"
                    };
                    
                    _context.Authors.Add(author);
                    await _context.SaveChangesAsync();
                }
                
                // Kiểm tra và tạo Publisher nếu chưa có
                if (!await _context.Publishers.AnyAsync())
                {
                    var publisher = new Publisher
                    {
                        PublisherName = "Nhà xuất bản mặc định",
                        Address = "Địa chỉ mặc định",
                        Phone = "0123456789"
                    };
                    
                    _context.Publishers.Add(publisher);
                    await _context.SaveChangesAsync();
                }
                
                return Content("Đã tạo dữ liệu cần thiết (Danh mục, Tác giả, Nhà xuất bản)");
            }
            catch (Exception ex)
            {
                return Content($"Lỗi: {ex.Message}");
            }
        }
    }
} 