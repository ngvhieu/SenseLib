using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;

namespace SenseLib.Controllers
{
    [Authorize] // Yêu cầu đăng nhập
    public class UploadController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public UploadController(DataContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Upload
        public async Task<IActionResult> Index()
        {
            // Lấy ID người dùng hiện tại
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Lấy danh sách tài liệu của người dùng
            var userDocuments = await _context.Documents
                .Where(d => d.UserID == userId)
                .Include(d => d.Category)
                .OrderByDescending(d => d.UploadDate)
                .ToListAsync();
                
            return View(userDocuments);
        }

        // GET: Upload/Create
        public async Task<IActionResult> Create()
        {
            ViewData["CategoryID"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "CategoryName");
            ViewData["AuthorID"] = new SelectList(await _context.Authors.ToListAsync(), "AuthorID", "AuthorName");
            ViewData["PublisherID"] = new SelectList(await _context.Publishers.ToListAsync(), "PublisherID", "PublisherName");
            return View();
        }

        // POST: Upload/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Document document, IFormFile file, IFormFile imageFile, bool isPaid, decimal? price, bool setAsAuthor = false, string newAuthorName = null)
        {
            try
            {
                // Kiểm tra thông tin file
                if (file == null)
                {
                    ModelState.AddModelError("", "Vui lòng chọn file tài liệu để tải lên");
                    PrepareViewData();
                    return View(document);
                }
                
                // Kiểm tra thư mục uploads
                string uploadsPath = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
                string documentsPath = Path.Combine(uploadsPath, "documents");
                
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }
                
                if (!Directory.Exists(documentsPath))
                {
                    Directory.CreateDirectory(documentsPath);
                }
                
                // Kiểm tra quyền ghi
                try
                {
                    string testFilePath = Path.Combine(documentsPath, "test_permission.txt");
                    System.IO.File.WriteAllText(testFilePath, "Test write permission");
                    System.IO.File.Delete(testFilePath);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Lỗi quyền ghi vào thư mục: {ex.Message}");
                    PrepareViewData();
                    return View(document);
                }
                
                // Lấy ID người dùng hiện tại
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                // Gán ID người dùng cho tài liệu
                document.UserID = userId;
                
                // Đặt trạng thái là Pending (chờ duyệt)
                document.Status = "Pending";
                
                // Thiết lập tài liệu có phí hay miễn phí
                document.IsPaid = isPaid;
                
                // Đặt giá nếu là tài liệu có phí
                if (isPaid && price.HasValue)
                {
                    document.Price = price.Value;
                }
                else
                {
                    document.Price = 0;  // Giá mặc định là 0 cho tài liệu miễn phí
                }
                
                // Đặt ngày tải lên
                document.UploadDate = DateTime.Now;
                
                // Khởi tạo các collection
                document.Comments = new List<Comment>();
                document.Ratings = new List<Rating>();
                document.Downloads = new List<Download>();
                document.Favorites = new List<Favorite>();
                
                // Xử lý tác giả mới nếu có
                if (setAsAuthor && !string.IsNullOrEmpty(newAuthorName))
                {
                    // Tạo mới tác giả
                    var newAuthor = new Author
                    {
                        AuthorName = newAuthorName,
                        Bio = "Tác giả được tạo bởi người dùng"
                    };
                    
                    _context.Authors.Add(newAuthor);
                    await _context.SaveChangesAsync();
                    
                    document.AuthorID = newAuthor.AuthorID;
                }
                
                // Xóa các lỗi validation cho các navigation properties
                ModelState.Remove("User");
                ModelState.Remove("Author");
                ModelState.Remove("Category");
                ModelState.Remove("Publisher");
                ModelState.Remove("Statistics");
                ModelState.Remove("FilePath");
                ModelState.Remove("ImagePath");
                
                // Xử lý upload file tài liệu
                try
                {
                    // Kiểm tra kích thước file (giới hạn 50MB)
                    if (file.Length > 50 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "Kích thước file không được vượt quá 50MB");
                        PrepareViewData();
                        return View(document);
                    }
                    
                    // Kiểm tra định dạng file
                    string extension = Path.GetExtension(file.FileName).ToLower();
                    string[] allowedExtensions = { 
                        ".pdf", ".doc", ".docx", 
                        ".xls", ".xlsx", ".xlsm", 
                        ".ppt", ".pptx", ".pptm", 
                        ".txt", ".rtf", ".csv", 
                        ".odt", ".ods", ".odp" 
                    };
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("", "Chỉ chấp nhận các định dạng file: PDF, Word, Excel, PowerPoint, Text và định dạng tương tự");
                        PrepareViewData();
                        return View(document);
                    }
                    
                    // Tạo tên file duy nhất
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                    string filePath = Path.Combine(documentsPath, uniqueFileName);
                    
                    // Lưu file vào server
                    using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    
                    // Kiểm tra xem file có thực sự được tạo không
                    if (System.IO.File.Exists(filePath))
                    {
                        document.FilePath = "/uploads/documents/" + uniqueFileName;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Lỗi khi lưu file: File không được tạo sau khi upload");
                        PrepareViewData();
                        return View(document);
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Lỗi khi tải lên file: {ex.Message}");
                    PrepareViewData();
                    return View(document);
                }
                
                // Xử lý upload ảnh bìa nếu có
                if (imageFile != null && imageFile.Length > 0)
                {
                    try
                    {
                        // Kiểm tra kích thước ảnh (giới hạn 5MB)
                        if (imageFile.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("", "Kích thước ảnh bìa không được vượt quá 5MB");
                            PrepareViewData();
                            return View(document);
                        }
                        
                        // Kiểm tra định dạng ảnh
                        string imageExtension = Path.GetExtension(imageFile.FileName).ToLower();
                        string[] allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        if (!allowedImageExtensions.Contains(imageExtension))
                        {
                            ModelState.AddModelError("", "Chỉ chấp nhận định dạng ảnh: JPG, PNG, GIF, WEBP");
                            PrepareViewData();
                            return View(document);
                        }
                        
                        // Tạo thư mục nếu chưa tồn tại
                        string imagesPath = Path.Combine(uploadsPath, "images");
                        if (!Directory.Exists(imagesPath))
                        {
                            Directory.CreateDirectory(imagesPath);
                        }
                        
                        // Tạo tên file duy nhất
                        string uniqueImageName = Guid.NewGuid().ToString() + imageExtension;
                        string imagePath = Path.Combine(imagesPath, uniqueImageName);
                        
                        // Lưu file vào server
                        using (var fileStream = new FileStream(imagePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
                        
                        document.ImagePath = "/uploads/images/" + uniqueImageName;
                    }
                    catch (Exception ex)
                    {
                        // Ghi log lỗi nhưng không dừng quy trình
                        document.ImagePath = "/img/document-placeholder.jpg";
                    }
                }
                else
                {
                    // Đặt ảnh bìa mặc định nếu không có ảnh được tải lên
                    document.ImagePath = "/img/document-placeholder.jpg";
                }
                
                // Lưu tài liệu vào database
                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Add(document);
                        var result = await _context.SaveChangesAsync();
                        
                        if (result > 0)
                        {
                            TempData["SuccessMessage"] = "Tài liệu đã được tải lên và đang chờ quản trị viên duyệt!";
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            ModelState.AddModelError("", "Lưu dữ liệu không thành công");
                            PrepareViewData();
                            return View(document);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", $"Lỗi khi lưu vào database: {ex.Message}");
                        PrepareViewData();
                        return View(document);
                    }
                }
                else
                {
                    // Nếu ModelState không hợp lệ, thử gán các giá trị cần thiết và kiểm tra lại
                    if (!string.IsNullOrEmpty(document.FilePath) && !string.IsNullOrEmpty(document.ImagePath))
                    {
                        // Loại bỏ các lỗi validation không cần thiết một lần nữa
                        ModelState.Remove("User");
                        ModelState.Remove("Author");
                        ModelState.Remove("Category");
                        ModelState.Remove("Publisher");
                        ModelState.Remove("Statistics");
                        ModelState.Remove("FilePath");
                        ModelState.Remove("ImagePath");
                        
                        // Thử lưu lại
                        try {
                            _context.Add(document);
                            var result = await _context.SaveChangesAsync();
                            
                            if (result > 0)
                            {
                                TempData["SuccessMessage"] = "Tài liệu đã được tải lên và đang chờ quản trị viên duyệt!";
                                return RedirectToAction(nameof(Index));
                            }
                        }
                        catch (Exception ex) {
                            ModelState.AddModelError("", $"Lỗi khi lưu vào database: {ex.Message}");
                        }
                    }
                }
                
                PrepareViewData();
                return View(document);
                
                void PrepareViewData()
                {
                    ViewData["CategoryID"] = new SelectList(_context.Categories, "CategoryID", "CategoryName", document.CategoryID);
                    ViewData["AuthorID"] = new SelectList(_context.Authors, "AuthorID", "AuthorName", document.AuthorID);
                    ViewData["PublisherID"] = new SelectList(_context.Publishers, "PublisherID", "PublisherName", document.PublisherID);
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                string errorMessage = $"Lỗi không xác định: {ex.Message}";
                
                ModelState.AddModelError("", errorMessage);
                PrepareViewData();
                return View(document);
                
                void PrepareViewData()
                {
                    ViewData["CategoryID"] = new SelectList(_context.Categories, "CategoryID", "CategoryName", document.CategoryID);
                    ViewData["AuthorID"] = new SelectList(_context.Authors, "AuthorID", "AuthorName", document.AuthorID);
                    ViewData["PublisherID"] = new SelectList(_context.Publishers, "PublisherID", "PublisherName", document.PublisherID);
                }
            }
        }
        
        // GET: Upload/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            // Lấy ID người dùng hiện tại
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Lấy thông tin tài liệu
            var document = await _context.Documents.FindAsync(id);
            
            if (document == null)
            {
                return NotFound();
            }
            
            // Kiểm tra quyền chỉnh sửa
            if (document.UserID != userId)
            {
                return Forbid();
            }
            
            ViewData["CategoryID"] = new SelectList(_context.Categories, "CategoryID", "CategoryName", document.CategoryID);
            ViewData["AuthorID"] = new SelectList(_context.Authors, "AuthorID", "AuthorName", document.AuthorID);
            ViewData["PublisherID"] = new SelectList(_context.Publishers, "PublisherID", "PublisherName", document.PublisherID);
            
            return View(document);
        }
        
        // POST: Upload/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Document document, IFormFile file, IFormFile imageFile, bool isPaid, decimal? price, bool setAsAuthor = false, string newAuthorName = null)
        {
            if (id != document.DocumentID)
            {
                return NotFound();
            }
            
            // Lấy ID người dùng hiện tại
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Lấy thông tin tài liệu từ database
            var existingDocument = await _context.Documents.FindAsync(id);
            
            if (existingDocument == null)
            {
                return NotFound();
            }
            
            // Kiểm tra quyền chỉnh sửa
            if (existingDocument.UserID != userId)
            {
                return Forbid();
            }
            
            // Cập nhật thông tin tài liệu
            existingDocument.Title = document.Title;
            existingDocument.Description = document.Description;
            existingDocument.CategoryID = document.CategoryID;
            
            // Nếu người dùng muốn đặt mình làm tác giả
            if (setAsAuthor)
            {
                // Lấy thông tin người dùng
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    // Tìm xem đã có tác giả với tên người dùng chưa
                    var existingAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.AuthorName == user.FullName);
                    if (existingAuthor != null)
                    {
                        existingDocument.AuthorID = existingAuthor.AuthorID;
                    }
                    else
                    {
                        // Tạo mới tác giả
                        var newAuthor = new Author
                        {
                            AuthorName = user.FullName,
                            Bio = "Người dùng tải lên tài liệu"
                        };
                        
                        _context.Authors.Add(newAuthor);
                        await _context.SaveChangesAsync();
                        
                        existingDocument.AuthorID = newAuthor.AuthorID;
                    }
                }
            }
            // Nếu người dùng nhập tên tác giả mới
            else if (!string.IsNullOrEmpty(newAuthorName) && document.AuthorID == null)
            {
                // Tìm xem có tác giả với tên này chưa
                var existingAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.AuthorName == newAuthorName);
                if (existingAuthor != null)
                {
                    existingDocument.AuthorID = existingAuthor.AuthorID;
                }
                else
                {
                    // Tạo mới tác giả
                    var newAuthor = new Author
                    {
                        AuthorName = newAuthorName,
                        Bio = "Tác giả được tạo từ quy trình tải lên tài liệu"
                    };
                    
                    _context.Authors.Add(newAuthor);
                    await _context.SaveChangesAsync();
                    
                    existingDocument.AuthorID = newAuthor.AuthorID;
                }
            }
            else
            {
                existingDocument.AuthorID = document.AuthorID;
            }
            
            existingDocument.PublisherID = document.PublisherID;
            
            // Cập nhật thông tin về giá
            existingDocument.IsPaid = isPaid;
            existingDocument.Price = isPaid && price.HasValue ? price.Value : 0;
            
            // Đặt lại trạng thái là Pending (chờ duyệt)
            existingDocument.Status = "Pending";
            
            // Xử lý upload file tài liệu mới nếu có
            if (file != null && file.Length > 0)
            {
                try
                {
                    // Tạo thư mục nếu chưa tồn tại
                    string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "documents");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    
                    // Kiểm tra kích thước file (giới hạn 50MB)
                    if (file.Length > 50 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "Kích thước file không được vượt quá 50MB");
                        PrepareViewData();
                        return View(existingDocument);
                    }
                    
                    // Kiểm tra định dạng file
                    string extension = Path.GetExtension(file.FileName).ToLower();
                    string[] allowedExtensions = { 
                        ".pdf", ".doc", ".docx", 
                        ".xls", ".xlsx", ".xlsm", 
                        ".ppt", ".pptx", ".pptm", 
                        ".txt", ".rtf", ".csv", 
                        ".odt", ".ods", ".odp" 
                    };
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("", "Chỉ chấp nhận các định dạng file: PDF, Word, Excel, PowerPoint, Text và định dạng tương tự");
                        PrepareViewData();
                        return View(existingDocument);
                    }
                    
                    // Xóa file cũ nếu có
                    if (!string.IsNullOrEmpty(existingDocument.FilePath))
                    {
                        string oldFilePath = Path.Combine(_hostEnvironment.WebRootPath, 
                            existingDocument.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            catch (Exception ex)
                            {
                                // Ghi log lỗi nhưng không dừng quy trình
                                Console.WriteLine($"Lỗi khi xóa file cũ: {ex.Message}");
                            }
                        }
                    }
                    
                    // Tạo tên file duy nhất
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    // Lưu file vào server
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    
                    // Cập nhật đường dẫn file mới
                    existingDocument.FilePath = "/uploads/documents/" + uniqueFileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Lỗi khi tải lên file: {ex.Message}");
                    PrepareViewData();
                    return View(existingDocument);
                }
            }
            
            // Xử lý upload ảnh bìa mới nếu có
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    // Kiểm tra kích thước ảnh (giới hạn 5MB)
                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "Kích thước ảnh bìa không được vượt quá 5MB");
                        PrepareViewData();
                        return View(existingDocument);
                    }
                    
                    // Kiểm tra định dạng ảnh
                    string extension = Path.GetExtension(imageFile.FileName).ToLower();
                    string[] allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                    if (!allowedImageExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("", "Chỉ chấp nhận định dạng ảnh: JPG, PNG, GIF, WEBP");
                        PrepareViewData();
                        return View(existingDocument);
                    }
                    
                    // Xóa ảnh cũ nếu không phải ảnh mặc định
                    if (!string.IsNullOrEmpty(existingDocument.ImagePath) && 
                        !existingDocument.ImagePath.Contains("document-placeholder.jpg") &&
                        existingDocument.ImagePath.StartsWith("/uploads/"))
                    {
                        string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, 
                            existingDocument.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                            catch (Exception ex)
                            {
                                // Ghi log lỗi nhưng không dừng quy trình
                                Console.WriteLine($"Lỗi khi xóa ảnh cũ: {ex.Message}");
                            }
                        }
                    }
                    
                    // Tạo thư mục nếu chưa tồn tại
                    string imagesFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "images");
                    if (!Directory.Exists(imagesFolder))
                    {
                        Directory.CreateDirectory(imagesFolder);
                    }
                    
                    // Tạo tên file duy nhất
                    string uniqueImageName = Guid.NewGuid().ToString() + extension;
                    string imagePath = Path.Combine(imagesFolder, uniqueImageName);
                    
                    // Lưu file vào server
                    using (var fileStream = new FileStream(imagePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    
                    // Cập nhật đường dẫn ảnh mới
                    existingDocument.ImagePath = "/uploads/images/" + uniqueImageName;
                }
                catch (Exception ex)
                {
                    // Ghi log lỗi nhưng vẫn giữ ảnh cũ
                    Console.WriteLine($"Lỗi khi tải lên ảnh mới: {ex.Message}");
                    ModelState.AddModelError("", $"Lỗi khi tải lên ảnh mới: {ex.Message}");
                    // Không thay đổi ảnh cũ
                }
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(existingDocument);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tài liệu đã được cập nhật và đang chờ quản trị viên duyệt lại!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DocumentExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            PrepareViewData();
            return View(existingDocument);
            
            void PrepareViewData()
            {
                ViewData["CategoryID"] = new SelectList(_context.Categories, "CategoryID", "CategoryName", document.CategoryID);
                ViewData["AuthorID"] = new SelectList(_context.Authors, "AuthorID", "AuthorName", document.AuthorID);
                ViewData["PublisherID"] = new SelectList(_context.Publishers, "PublisherID", "PublisherName", document.PublisherID);
            }
        }
        
        // GET: Upload/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            // Lấy ID người dùng hiện tại
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Lấy thông tin tài liệu
            var document = await _context.Documents
                .Include(d => d.Category)
                .Include(d => d.Author)
                .Include(d => d.Publisher)
                .FirstOrDefaultAsync(d => d.DocumentID == id);
                
            if (document == null)
            {
                return NotFound();
            }
            
            // Kiểm tra quyền xóa
            if (document.UserID != userId)
            {
                return Forbid();
            }
            
            // Kiểm tra trạng thái
            if (document.Status == "Approved")
            {
                TempData["ErrorMessage"] = "Không thể xóa tài liệu đã được duyệt!";
                return RedirectToAction(nameof(Index));
            }
            
            return View(document);
        }
        
        // POST: Upload/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Lấy ID người dùng hiện tại
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Lấy thông tin tài liệu
            var document = await _context.Documents.FindAsync(id);
            
            if (document == null)
            {
                return NotFound();
            }
            
            // Kiểm tra quyền xóa
            if (document.UserID != userId)
            {
                return Forbid();
            }
            
            // Kiểm tra trạng thái
            if (document.Status == "Approved")
            {
                TempData["ErrorMessage"] = "Không thể xóa tài liệu đã được duyệt!";
                return RedirectToAction(nameof(Index));
            }
            
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Tài liệu đã được xóa thành công!";
            return RedirectToAction(nameof(Index));
        }
        
        private bool DocumentExists(int id)
        {
            return _context.Documents.Any(e => e.DocumentID == id);
        }

        // API để tạo mới tác giả
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAuthor(string authorName, string authorBio = "")
        {
            if (string.IsNullOrEmpty(authorName))
            {
                return Json(new { success = false, message = "Tên tác giả không được để trống" });
            }
            
            // Kiểm tra xem tác giả đã tồn tại chưa
            var existingAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.AuthorName == authorName);
            if (existingAuthor != null)
            {
                return Json(new { success = true, author = existingAuthor, message = "Tác giả đã tồn tại" });
            }
            
            // Tạo mới tác giả
            var newAuthor = new Author
            {
                AuthorName = authorName,
                Bio = authorBio ?? "Tác giả được tạo từ quy trình tải lên tài liệu"
            };
            
            _context.Authors.Add(newAuthor);
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, author = newAuthor, message = "Tác giả đã được tạo thành công" });
        }
        
        // API để tạo mới nhà xuất bản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePublisher(string publisherName, string address = "", string phone = "")
        {
            if (string.IsNullOrEmpty(publisherName))
            {
                return Json(new { success = false, message = "Tên nhà xuất bản không được để trống" });
            }
            
            // Kiểm tra xem nhà xuất bản đã tồn tại chưa
            var existingPublisher = await _context.Publishers.FirstOrDefaultAsync(p => p.PublisherName == publisherName);
            if (existingPublisher != null)
            {
                return Json(new { success = true, publisher = existingPublisher, message = "Nhà xuất bản đã tồn tại" });
            }
            
            // Tạo mới nhà xuất bản
            var newPublisher = new Publisher
            {
                PublisherName = publisherName,
                Address = address ?? "",
                Phone = phone ?? ""
            };
            
            _context.Publishers.Add(newPublisher);
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, publisher = newPublisher, message = "Nhà xuất bản đã được tạo thành công" });
        }
        
        // API để tạo mới danh mục
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string categoryName, string description = "")
        {
            if (string.IsNullOrEmpty(categoryName))
            {
                return Json(new { success = false, message = "Tên danh mục không được để trống" });
            }
            
            // Kiểm tra xem danh mục đã tồn tại chưa
            var existingCategory = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName == categoryName);
            if (existingCategory != null)
            {
                return Json(new { success = true, category = existingCategory, message = "Danh mục đã tồn tại" });
            }
            
            // Tạo mới danh mục
            var newCategory = new Category
            {
                CategoryName = categoryName,
                Description = description ?? "",
                Status = "Active"
            };
            
            _context.Categories.Add(newCategory);
            await _context.SaveChangesAsync();
            
            return Json(new { success = true, category = newCategory, message = "Danh mục đã được tạo thành công" });
        }
        
        // API để lấy thông tin người dùng hiện tại
        [HttpGet]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }
            
            return Json(new { success = true, user = new { fullName = user.FullName, username = user.Username, email = user.Email } });
        }
    }
} 