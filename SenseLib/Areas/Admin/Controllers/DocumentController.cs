using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Collections.Generic;

namespace SenseLib.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DocumentController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public DocumentController(DataContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Admin/Document
        public async Task<IActionResult> Index(string searchString, string status, int page = 1, bool showAllDocuments = false)
        {
            int pageSize = 10;

            // Debug: Hiển thị tổng số tài liệu
            int totalDocCount = await _context.Documents.CountAsync();
            ViewBag.TotalDocumentsInDB = totalDocCount;
            ViewBag.ShowAllDocuments = showAllDocuments;

            var query = _context.Documents
                .Include(d => d.Author)
                .Include(d => d.Category)
                .Include(d => d.Publisher)
                .Include(d => d.Downloads)
                .Include(d => d.User)
                .AsQueryable();

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.Status == status);
                ViewBag.CurrentStatus = status;
            }
            // Không lọc theo trạng thái mặc định để hiển thị tất cả
            else
            {
                ViewBag.CurrentStatus = "All";
            }

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(d => 
                    d.Title.Contains(searchString) ||
                    (d.Description != null && d.Description.Contains(searchString)) ||
                    (d.Author != null && d.Author.AuthorName.Contains(searchString)) ||
                    (d.User != null && d.User.Username.Contains(searchString)));
                
                ViewBag.CurrentSearch = searchString;
            }

            // Debug: Hiển thị số lượng tài liệu từng trạng thái
            ViewBag.PendingCount = await _context.Documents.CountAsync(d => d.Status == "Pending");
            ViewBag.ApprovedCount = await _context.Documents.CountAsync(d => d.Status == "Approved");
            ViewBag.RejectedCount = await _context.Documents.CountAsync(d => d.Status == "Rejected");
            ViewBag.TotalCount = totalDocCount;
            
            // Sắp xếp và phân trang
            var documents = await query
                .OrderByDescending(d => d.UploadDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Thống kê lượt tải và lượt xem
            foreach (var document in documents)
            {
                document.Downloads = await _context.Downloads
                    .Where(d => d.DocumentID == document.DocumentID)
                    .ToListAsync();
            }

            // Phân trang
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            
            // Thêm danh sách các trạng thái để hiển thị filter
            ViewBag.StatusList = new List<string> { "Pending", "Approved", "Rejected" };

            return View(documents);
        }

        // GET: Admin/Document/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents
                .Include(d => d.Author)
                .Include(d => d.Category)
                .Include(d => d.Publisher)
                .Include(d => d.User)
                .Include(d => d.Comments)
                    .ThenInclude(c => c.User)
                .Include(d => d.Ratings)
                .FirstOrDefaultAsync(m => m.DocumentID == id);

            if (document == null)
            {
                return NotFound();
            }

            // Thống kê
            ViewBag.DownloadCount = await _context.Downloads.CountAsync(d => d.DocumentID == id);
            ViewBag.RatingCount = document.Ratings.Count;
            ViewBag.AverageRating = document.Ratings.Any() ? document.Ratings.Average(r => r.RatingValue) : 0;

            return View(document);
        }

        // GET: Admin/Document/Create
        public async Task<IActionResult> Create()
        {
            ViewData["AuthorID"] = new SelectList(await _context.Authors.ToListAsync(), "AuthorID", "AuthorName");
            ViewData["CategoryID"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "CategoryName");
            ViewData["PublisherID"] = new SelectList(await _context.Publishers.ToListAsync(), "PublisherID", "PublisherName");
            return View();
        }

        // POST: Admin/Document/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,CategoryID,PublisherID,AuthorID,Status")] Document document, IFormFile file, IFormFile imageFile, IFormCollection form)
        {
            // Xử lý Description từ form nếu không binding được qua model
            if (form.ContainsKey("Description"))
            {
                document.Description = form["Description"].ToString();
                Console.WriteLine($"Description từ form: {(form["Description"].ToString().Length > 50 ? form["Description"].ToString().Substring(0, 50) + "..." : form["Description"].ToString())}");
            }
            else
            {
                document.Description = "";
                Console.WriteLine("Không tìm thấy Description trong form");
            }
            
            // Khởi tạo các collection
            document.Comments = new List<Comment>();
            document.Ratings = new List<Rating>();
            document.Downloads = new List<Download>();
            document.Favorites = new List<Favorite>();
            
            // Loại bỏ các lỗi validation liên quan đến navigation properties và các trường sẽ được gán sau
            foreach (var key in ModelState.Keys.ToList())
            {
                if (key.Contains("Author") || key.Contains("Category") || key.Contains("Publisher") ||
                    key.Contains("Comments") || key.Contains("Ratings") || key.Contains("Downloads") ||
                    key.Contains("Favorites") || key.Contains("Statistics") ||
                    key.Contains("FilePath") || key.Contains("Description") || key.Contains("ImagePath") ||
                    key == "file" || key == "imageFile" || key == "User" || key.Contains("UserID"))
                {
                    ModelState.Remove(key);
                }
            }
            
            // Đảm bảo Description có giá trị
            if (string.IsNullOrEmpty(document.Description))
            {
                document.Description = "";
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload file
                    if (file != null && file.Length > 0)
                    {
                        // Kiểm tra webRootPath tồn tại
                        if (string.IsNullOrEmpty(_hostEnvironment.WebRootPath))
                        {
                            ModelState.AddModelError("", "WebRootPath không tồn tại hoặc không có quyền truy cập.");
                            goto PrepareViewData;
                        }

                        // Lấy đường dẫn đến thư mục uploads/documents
                        string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "documents");
                        
                        // Kiểm tra và tạo thư mục nếu chưa tồn tại
                        if (!Directory.Exists(Path.Combine(_hostEnvironment.WebRootPath, "uploads")))
                        {
                            try
                            {
                                Directory.CreateDirectory(Path.Combine(_hostEnvironment.WebRootPath, "uploads"));
                            } 
                            catch (Exception ex)
                            {
                                ModelState.AddModelError("", $"Không thể tạo thư mục uploads: {ex.Message}");
                                goto PrepareViewData;
                            }
                        }
                        
                        if (!Directory.Exists(uploadsFolder))
                        {
                            try
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }
                            catch (Exception ex)
                            {
                                ModelState.AddModelError("", $"Không thể tạo thư mục uploads/documents: {ex.Message}");
                                goto PrepareViewData;
                            }
                        }
                        
                        // Kiểm tra quyền ghi vào thư mục
                        try
                        {
                            string testFilePath = Path.Combine(uploadsFolder, "test_write_permission.txt");
                            System.IO.File.WriteAllText(testFilePath, "Test write permission");
                            System.IO.File.Delete(testFilePath);
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", $"Không có quyền ghi vào thư mục uploads/documents: {ex.Message}");
                            goto PrepareViewData;
                        }
                        
                        // Tạo tên file duy nhất
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        
                        // Lưu file vào server
                        try
                        {
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }
                        }
                        catch (Exception ex)
                        {
                            string errorMessage = ex.Message;
                            if (ex.InnerException != null)
                            {
                                errorMessage += $" Inner exception: {ex.InnerException.Message}";
                            }
                            
                            ModelState.AddModelError("", $"Lỗi khi lưu file: {errorMessage}");
                            goto PrepareViewData;
                        }
                        
                        // Kiểm tra xem file đã được tạo chưa
                        if (!System.IO.File.Exists(filePath))
                        {
                            ModelState.AddModelError("", "Không thể lưu file. File không được tạo sau khi upload.");
                            goto PrepareViewData;
                        }
                        
                        document.FilePath = "/uploads/documents/" + uniqueFileName;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Vui lòng chọn file tài liệu để tải lên.");
                        goto PrepareViewData;
                    }
                    
                    // Xử lý upload ảnh đại diện
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Tạo thư mục uploads/images nếu chưa tồn tại
                        string imagesFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "images");
                        if (!Directory.Exists(imagesFolder))
                        {
                            try
                            {
                                Directory.CreateDirectory(imagesFolder);
                            }
                            catch (Exception ex)
                            {
                                ModelState.AddModelError("", $"Không thể tạo thư mục uploads/images: {ex.Message}");
                                goto PrepareViewData;
                            }
                        }
                        
                        // Tạo tên file duy nhất cho ảnh
                        string uniqueImageName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                        string imagePath = Path.Combine(imagesFolder, uniqueImageName);
                        
                        // Lưu ảnh vào server
                        try
                        {
                            using (var fileStream = new FileStream(imagePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(fileStream);
                            }
                        }
                        catch (Exception ex)
                        {
                            string errorMessage = ex.Message;
                            if (ex.InnerException != null)
                            {
                                errorMessage += $" Inner exception: {ex.InnerException.Message}";
                            }
                            
                            ModelState.AddModelError("", $"Lỗi khi lưu ảnh: {errorMessage}");
                            // Không halt nếu chỉ lỗi ảnh, vẫn tiếp tục tạo tài liệu
                        }
                        
                        if (System.IO.File.Exists(imagePath))
                        {
                            document.ImagePath = "/uploads/images/" + uniqueImageName;
                        }
                    }
                    
                    document.UploadDate = DateTime.Now;
                    
                    // Đặt trạng thái là Published cho tài liệu do admin tạo
                    document.Status = "Published";
                    
                    _context.Add(document);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Tài liệu đã được tạo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    string errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" Inner exception: {ex.InnerException.Message}";
                    }
                    
                    ModelState.AddModelError("", $"Lỗi khi lưu tài liệu: {errorMessage}");
                }
            }
            else
            {
                // Thêm kiểm tra Model không hợp lệ
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        ModelState.AddModelError("", $"Lỗi Model: {error.ErrorMessage}");
                    }
                }
            }
            
        PrepareViewData:
            ViewData["AuthorID"] = new SelectList(await _context.Authors.ToListAsync(), "AuthorID", "AuthorName", document.AuthorID);
            ViewData["CategoryID"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "CategoryName", document.CategoryID);
            ViewData["PublisherID"] = new SelectList(await _context.Publishers.ToListAsync(), "PublisherID", "PublisherName", document.PublisherID);
            return View(document);
        }

        // GET: Admin/Document/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }
            
            ViewData["AuthorID"] = new SelectList(await _context.Authors.ToListAsync(), "AuthorID", "AuthorName", document.AuthorID);
            ViewData["CategoryID"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "CategoryName", document.CategoryID);
            ViewData["PublisherID"] = new SelectList(await _context.Publishers.ToListAsync(), "PublisherID", "PublisherName", document.PublisherID);
            return View(document);
        }

        // POST: Admin/Document/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DocumentID,Title,CategoryID,PublisherID,AuthorID,Status,FilePath,ImagePath,IsPaid,Price")] Document document, IFormFile file, IFormFile imageFile, IFormCollection form)
        {
            // Ghi log để debug
            Console.WriteLine($"=== BẮT ĐẦU EDIT - DocumentID: {id} ===");
            Console.WriteLine($"Title: {document.Title}");
            Console.WriteLine($"CategoryID: {document.CategoryID}");
            Console.WriteLine($"Status: {document.Status}");
            Console.WriteLine($"IsPaid: {document.IsPaid}, Price: {document.Price}");
            Console.WriteLine($"File: {(file != null ? $"{file.FileName}, {file.Length} bytes" : "Không có")}");
            Console.WriteLine($"ImageFile: {(imageFile != null ? $"{imageFile.FileName}, {imageFile.Length} bytes" : "Không có")}");
            Console.WriteLine($"Form keys: {string.Join(", ", form.Keys.ToArray())}");
            
            if (id != document.DocumentID)
            {
                Console.WriteLine("ID không khớp, trả về NotFound");
                return NotFound();
            }
            
            // Xử lý Description từ form nếu không binding được qua model
            if (form.ContainsKey("Description"))
            {
                document.Description = form["Description"].ToString();
                Console.WriteLine($"Description từ form: {(form["Description"].ToString().Length > 50 ? form["Description"].ToString().Substring(0, 50) + "..." : form["Description"].ToString())}");
            }
            else
            {
                document.Description = "";
                Console.WriteLine("Không tìm thấy Description trong form");
            }
            
            // Lấy document hiện tại từ DB để giữ thông tin chưa được bind
            var existingDocument = await _context.Documents.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DocumentID == id);
                
            if (existingDocument == null)
            {
                Console.WriteLine($"Không tìm thấy tài liệu với ID {id} trong cơ sở dữ liệu");
                return NotFound();
            }
                
            // Gán lại các trường không được bind từ form
            document.UploadDate = existingDocument.UploadDate;
            document.UserID = existingDocument.UserID;
            
            // Giữ lại giá trị hiện tại nếu không được gửi qua form
            if (document.IsPaid == false && existingDocument.IsPaid)
            {
                document.IsPaid = existingDocument.IsPaid;
                document.Price = existingDocument.Price;
                Console.WriteLine("Giữ lại IsPaid và Price từ dữ liệu hiện tại");
            }
            
            // Khởi tạo các collection
            document.Comments = new List<Comment>();
            document.Ratings = new List<Rating>();
            document.Downloads = new List<Download>();
            document.Favorites = new List<Favorite>();
            
            // Kiểm tra các lỗi validation input trước khi xử lý file
            if (string.IsNullOrEmpty(document.Title))
            {
                ModelState.AddModelError("Title", "Tiêu đề không được để trống");
                Console.WriteLine("Lỗi: Tiêu đề trống");
            }
            
            if (document.CategoryID == null || document.CategoryID <= 0)
            {
                ModelState.AddModelError("CategoryID", "Vui lòng chọn danh mục");
                Console.WriteLine("Lỗi: Danh mục không hợp lệ");
            }
            
            if (string.IsNullOrEmpty(document.Status))
            {
                ModelState.AddModelError("Status", "Vui lòng chọn trạng thái");
                Console.WriteLine("Lỗi: Trạng thái trống");
            }
            
            // Loại bỏ các lỗi validation liên quan đến navigation properties và các trường sẽ được gán sau
            foreach (var key in ModelState.Keys.ToList())
            {
                if (key.Contains("Author") || key.Contains("Category") || key.Contains("Publisher") ||
                    key.Contains("Comments") || key.Contains("Ratings") || key.Contains("Downloads") ||
                    key.Contains("Favorites") || key.Contains("Statistics") ||
                    key.Contains("FilePath") || key.Contains("Description") || key.Contains("ImagePath") ||
                    key == "file" || key == "imageFile" || key == "User" || key.Contains("UserID"))
                {
                    ModelState.Remove(key);
                }
            }
            
            // Đảm bảo Description có giá trị
            if (string.IsNullOrEmpty(document.Description))
            {
                document.Description = "";
                Console.WriteLine("Đã gán Description rỗng");
            }
            
            // Kiểm tra file nếu có
            if (file != null && file.Length > 0)
            {
                var fileName = Path.GetFileName(file.FileName);
                var fileExtension = Path.GetExtension(fileName).ToLower();
                var maxFileSize = 20 * 1024 * 1024; // 20MB
                
                Console.WriteLine($"Kiểm tra file: {fileName}, định dạng: {fileExtension}, kích thước: {file.Length} bytes");
                
                if (fileExtension != ".pdf" && fileExtension != ".doc" && fileExtension != ".docx")
                {
                    ModelState.AddModelError("", $"Định dạng file {fileExtension} không được hỗ trợ. Vui lòng chỉ sử dụng PDF, DOC, hoặc DOCX.");
                    Console.WriteLine($"Lỗi: Định dạng file {fileExtension} không được hỗ trợ");
                }
                
                if (file.Length > maxFileSize)
                {
                    ModelState.AddModelError("", $"Kích thước file ({file.Length / (1024 * 1024)}MB) vượt quá giới hạn cho phép (20MB).");
                    Console.WriteLine($"Lỗi: Kích thước file {file.Length / (1024 * 1024)}MB vượt quá giới hạn");
                }
            }
            
            // Kiểm tra file ảnh nếu có
            if (imageFile != null && imageFile.Length > 0)
            {
                var imageName = Path.GetFileName(imageFile.FileName);
                var imageExtension = Path.GetExtension(imageName).ToLower();
                var maxImageSize = 5 * 1024 * 1024; // 5MB
                
                Console.WriteLine($"Kiểm tra ảnh: {imageName}, định dạng: {imageExtension}, kích thước: {imageFile.Length} bytes");
                
                if (imageExtension != ".jpg" && imageExtension != ".jpeg" && imageExtension != ".png" && imageExtension != ".gif")
                {
                    ModelState.AddModelError("", $"Định dạng ảnh {imageExtension} không được hỗ trợ. Vui lòng chỉ sử dụng JPG, JPEG, PNG, hoặc GIF.");
                    Console.WriteLine($"Lỗi: Định dạng ảnh {imageExtension} không được hỗ trợ");
                }
                
                if (imageFile.Length > maxImageSize)
                {
                    ModelState.AddModelError("", $"Kích thước ảnh ({imageFile.Length / (1024 * 1024)}MB) vượt quá giới hạn cho phép (5MB).");
                    Console.WriteLine($"Lỗi: Kích thước ảnh {imageFile.Length / (1024 * 1024)}MB vượt quá giới hạn");
                }
            }
            
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            if (!ModelState.IsValid)
            {
                // Hiển thị tất cả lỗi
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        Console.WriteLine($"Lỗi {state.Key}: {error.ErrorMessage}");
                    }
                }
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    Console.WriteLine("ModelState hợp lệ, tiến hành cập nhật tài liệu");
                    // Xử lý upload file nếu có
                    if (file != null && file.Length > 0)
                    {
                        // Kiểm tra webRootPath tồn tại
                        if (string.IsNullOrEmpty(_hostEnvironment.WebRootPath))
                        {
                            ModelState.AddModelError("", "WebRootPath không tồn tại hoặc không có quyền truy cập.");
                            Console.WriteLine("Lỗi: WebRootPath không tồn tại");
                            goto PrepareViewData;
                        }

                        // Lấy đường dẫn đến thư mục uploads/documents
                        string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "documents");
                        Console.WriteLine($"Thư mục tải lên: {uploadsFolder}");
                        
                        // Kiểm tra và tạo thư mục nếu chưa tồn tại
                        if (!Directory.Exists(Path.Combine(_hostEnvironment.WebRootPath, "uploads")))
                        {
                            try
                            {
                                Directory.CreateDirectory(Path.Combine(_hostEnvironment.WebRootPath, "uploads"));
                                Console.WriteLine("Đã tạo thư mục uploads");
                            } 
                            catch (Exception ex)
                            {
                                ModelState.AddModelError("", $"Không thể tạo thư mục uploads: {ex.Message}");
                                Console.WriteLine($"Lỗi khi tạo thư mục uploads: {ex.Message}");
                                goto PrepareViewData;
                            }
                        }
                        
                        if (!Directory.Exists(uploadsFolder))
                        {
                            try
                            {
                                Directory.CreateDirectory(uploadsFolder);
                                Console.WriteLine("Đã tạo thư mục uploads/documents");
                            }
                            catch (Exception ex)
                            {
                                ModelState.AddModelError("", $"Không thể tạo thư mục uploads/documents: {ex.Message}");
                                Console.WriteLine($"Lỗi khi tạo thư mục uploads/documents: {ex.Message}");
                                goto PrepareViewData;
                            }
                        }
                        
                        // Kiểm tra quyền ghi vào thư mục
                        try
                        {
                            string testFilePath = Path.Combine(uploadsFolder, "test_write_permission.txt");
                            System.IO.File.WriteAllText(testFilePath, "Test write permission");
                            System.IO.File.Delete(testFilePath);
                            Console.WriteLine("Đã kiểm tra quyền ghi thành công");
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", $"Không có quyền ghi vào thư mục uploads/documents: {ex.Message}");
                            Console.WriteLine($"Lỗi quyền ghi: {ex.Message}");
                            goto PrepareViewData;
                        }
                        
                        // Xóa file cũ nếu có
                        if (!string.IsNullOrEmpty(existingDocument.FilePath))
                        {
                            string oldFilePath = Path.Combine(_hostEnvironment.WebRootPath, existingDocument.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            Console.WriteLine($"Đường dẫn file cũ: {oldFilePath}");
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldFilePath);
                                    Console.WriteLine("Đã xóa file cũ thành công");
                                }
                                catch (Exception ex)
                                {
                                    // Log lỗi nhưng vẫn tiếp tục
                                    Console.WriteLine($"Lỗi khi xóa file cũ: {ex.Message}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("File cũ không tồn tại trên hệ thống");
                            }
                        }
                        
                        // Tạo file mới
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        Console.WriteLine($"Tạo file mới: {filePath}");
                        
                        // Lưu file vào server
                        try 
                        {
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }
                            Console.WriteLine("Đã lưu file thành công");
                        } 
                        catch (Exception ex)
                        {
                            string errorMessage = ex.Message;
                            if (ex.InnerException != null)
                            {
                                errorMessage += $" Inner exception: {ex.InnerException.Message}";
                            }
                            
                            ModelState.AddModelError("", $"Lỗi khi lưu file: {errorMessage}");
                            Console.WriteLine($"Lỗi khi lưu file: {errorMessage}");
                            goto PrepareViewData;
                        }
                        
                        // Kiểm tra xem file đã được tạo chưa
                        if (!System.IO.File.Exists(filePath))
                        {
                            ModelState.AddModelError("", "Không thể lưu file. File không được tạo sau khi upload.");
                            Console.WriteLine("File không tồn tại sau khi tải lên");
                            goto PrepareViewData;
                        }
                        
                        document.FilePath = "/uploads/documents/" + uniqueFileName;
                        Console.WriteLine($"Cập nhật đường dẫn file: {document.FilePath}");
                    }
                    else
                    {
                        // Giữ nguyên đường dẫn file cũ
                        document.FilePath = existingDocument.FilePath;
                        Console.WriteLine($"Giữ đường dẫn file cũ: {document.FilePath}");
                    }
                    
                    // Xử lý upload ảnh đại diện nếu có
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Tạo thư mục uploads/images nếu chưa tồn tại
                        string imagesFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "images");
                        Console.WriteLine($"Thư mục ảnh: {imagesFolder}");
                        if (!Directory.Exists(imagesFolder))
                        {
                            try
                            {
                                Directory.CreateDirectory(imagesFolder);
                                Console.WriteLine("Đã tạo thư mục uploads/images");
                            }
                            catch (Exception ex)
                            {
                                ModelState.AddModelError("", $"Không thể tạo thư mục uploads/images: {ex.Message}");
                                Console.WriteLine($"Lỗi khi tạo thư mục uploads/images: {ex.Message}");
                                goto PrepareViewData;
                            }
                        }
                        
                        // Xóa ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(existingDocument.ImagePath))
                        {
                            string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, existingDocument.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            Console.WriteLine($"Đường dẫn ảnh cũ: {oldImagePath}");
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldImagePath);
                                    Console.WriteLine("Đã xóa ảnh cũ thành công");
                                }
                                catch (Exception ex)
                                {
                                    // Log lỗi nhưng vẫn tiếp tục
                                    Console.WriteLine($"Lỗi khi xóa ảnh cũ: {ex.Message}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Ảnh cũ không tồn tại trên hệ thống");
                            }
                        }
                        
                        // Tạo tên file duy nhất cho ảnh
                        string uniqueImageName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                        string imagePath = Path.Combine(imagesFolder, uniqueImageName);
                        Console.WriteLine($"Tạo ảnh mới: {imagePath}");
                        
                        // Lưu ảnh vào server
                        try
                        {
                            using (var fileStream = new FileStream(imagePath, FileMode.Create))
                            {
                                await imageFile.CopyToAsync(fileStream);
                            }
                            Console.WriteLine("Đã lưu ảnh thành công");
                        }
                        catch (Exception ex)
                        {
                            string errorMessage = ex.Message;
                            if (ex.InnerException != null)
                            {
                                errorMessage += $" Inner exception: {ex.InnerException.Message}";
                            }
                            
                            ModelState.AddModelError("", $"Lỗi khi lưu ảnh: {errorMessage}");
                            Console.WriteLine($"Lỗi khi lưu ảnh: {errorMessage}");
                            // Không halt nếu chỉ lỗi ảnh, vẫn tiếp tục cập nhật tài liệu
                        }
                        
                        if (System.IO.File.Exists(imagePath))
                        {
                            document.ImagePath = "/uploads/images/" + uniqueImageName;
                            Console.WriteLine($"Cập nhật đường dẫn ảnh: {document.ImagePath}");
                        }
                    }
                    else
                    {
                        // Giữ nguyên đường dẫn ảnh cũ
                        document.ImagePath = existingDocument.ImagePath;
                        Console.WriteLine($"Giữ đường dẫn ảnh cũ: {document.ImagePath}");
                    }
                    
                    Console.WriteLine("Thực hiện cập nhật vào database");
                    _context.Update(document);
                    await _context.SaveChangesAsync();
                    Console.WriteLine("Cập nhật thành công");
                    TempData["SuccessMessage"] = "Cập nhật tài liệu thành công!";
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine($"DbUpdateConcurrencyException: {ex.Message}");
                    if (!DocumentExists(document.DocumentID))
                    {
                        Console.WriteLine("Tài liệu không tồn tại");
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    string errorMessage = ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" Inner exception: {ex.InnerException.Message}";
                    }
                    
                    Console.WriteLine($"Lỗi ngoại lệ: {errorMessage}");
                    ModelState.AddModelError("", $"Lỗi khi cập nhật tài liệu: {errorMessage}");
                    TempData["ErrorMessage"] = $"Lỗi khi cập nhật tài liệu: {errorMessage}";
                }
            }
            
        PrepareViewData:
            Console.WriteLine("Chuẩn bị ViewData và trả về view");
            ViewData["AuthorID"] = new SelectList(await _context.Authors.ToListAsync(), "AuthorID", "AuthorName", document.AuthorID);
            ViewData["CategoryID"] = new SelectList(await _context.Categories.ToListAsync(), "CategoryID", "CategoryName", document.CategoryID);
            ViewData["PublisherID"] = new SelectList(await _context.Publishers.ToListAsync(), "PublisherID", "PublisherName", document.PublisherID);
            
            // Thêm các lỗi ModelState vào TempData để hiển thị
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                if (errors.Any())
                {
                    TempData["ErrorMessage"] = string.Join("<br>", errors);
                }
            }
            
            Console.WriteLine($"=== KẾT THÚC EDIT - DocumentID: {id} ===");
            return View(document);
        }

        // GET: Admin/Document/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents
                .Include(d => d.Author)
                .Include(d => d.Category)
                .Include(d => d.Publisher)
                .FirstOrDefaultAsync(m => m.DocumentID == id);
                
            if (document == null)
            {
                return NotFound();
            }

            return View(document);
        }

        // POST: Admin/Document/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            
            if (document != null)
            {
                // Xóa file nếu có
                if (!string.IsNullOrEmpty(document.FilePath))
                {
                    string filePath = Path.Combine(_hostEnvironment.WebRootPath, document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(filePath))
                    {
                        try
                        {
                            System.IO.File.Delete(filePath);
                        }
                        catch (Exception ex)
                        {
                            // Log lỗi nhưng vẫn tiếp tục
                            Console.WriteLine($"Lỗi khi xóa file: {ex.Message}");
                        }
                    }
                }
                
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // GET: Admin/Document/Statistics
        public async Task<IActionResult> Statistics()
        {
            // Thống kê tổng quan
            ViewBag.TotalDocuments = await _context.Documents.CountAsync();
            ViewBag.PublishedDocuments = await _context.Documents.CountAsync(d => d.Status == "Published");
            ViewBag.DraftDocuments = await _context.Documents.CountAsync(d => d.Status == "Draft");
            ViewBag.TotalDownloads = await _context.Downloads.CountAsync();
            
            // Top 10 tài liệu được tải nhiều nhất
            var topDownloadedDocs = await _context.Documents
                .Select(d => new
                {
                    Document = d,
                    DownloadCount = d.Downloads.Count
                })
                .OrderByDescending(d => d.DownloadCount)
                .Take(10)
                .ToListAsync();
                
            ViewBag.TopDownloadedDocuments = topDownloadedDocs;
            
            // Thống kê theo danh mục
            var categoriesStats = await _context.Categories
                .Select(c => new
                {
                    Category = c,
                    DocumentCount = c.Documents.Count,
                    DownloadCount = c.Documents.SelectMany(d => d.Downloads).Count()
                })
                .ToListAsync();
                
            ViewBag.CategoriesStats = categoriesStats;
            
            return View();
        }

        // GET: Admin/Document/CreateDebug
        public IActionResult CreateDebug()
        {
            return View("CreateDebug");
        }
        
        // POST: Admin/Document/CreateDebug
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDebug(IFormCollection form, IFormFile file)
        {
            string debug = "Thông tin form:\n";
            debug += $"FormKeys: {string.Join(", ", form.Keys)}\n";
            foreach (var key in form.Keys)
            {
                debug += $"{key}: {form[key]}\n";
            }
            
            debug += $"\nFile: {(file != null ? "Có file (" + file.Length + " bytes)" : "Không có file")}\n";
            
            // Kiểm tra thư mục uploads/documents
            debug += $"\nKiểm tra thư mục:\n";
            debug += $"WebRootPath: {_hostEnvironment.WebRootPath}\n";
            
            string uploadsPath = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
            string documentsPath = Path.Combine(uploadsPath, "documents");
            
            debug += $"uploads path: {uploadsPath}, exists: {Directory.Exists(uploadsPath)}\n";
            debug += $"documents path: {documentsPath}, exists: {Directory.Exists(documentsPath)}\n";
            
            // Kiểm tra quyền ghi thư mục
            try
            {
                string testPath = Path.Combine(documentsPath, "debug_test.txt");
                System.IO.File.WriteAllText(testPath, "Debug test");
                debug += $"Đã tạo file test: {testPath}\n";
                System.IO.File.Delete(testPath);
                debug += $"Đã xóa file test\n";
            }
            catch (Exception ex)
            {
                debug += $"Lỗi khi thử tạo file test: {ex.Message}\n";
            }
            
            // Hiển thị thông tin tất cả tài liệu trong database
            debug += "\n------ THÔNG TIN TÀI LIỆU TRONG DATABASE ------\n";
            debug += $"Tổng số tài liệu: {await _context.Documents.CountAsync()}\n";
            debug += $"Số tài liệu đang chờ duyệt: {await _context.Documents.CountAsync(d => d.Status == "Pending")}\n";
            debug += $"Số tài liệu đã duyệt: {await _context.Documents.CountAsync(d => d.Status == "Approved")}\n";
            debug += $"Số tài liệu bị từ chối: {await _context.Documents.CountAsync(d => d.Status == "Rejected")}\n\n";
            
            debug += "Chi tiết 10 tài liệu mới nhất:\n";
            
            var latestDocs = await _context.Documents
                .OrderByDescending(d => d.UploadDate)
                .Take(10)
                .ToListAsync();
                
            foreach (var doc in latestDocs)
            {
                debug += $"- ID: {doc.DocumentID}, Tiêu đề: {doc.Title}, Trạng thái: {doc.Status}, Upload: {doc.UploadDate}\n";
                debug += $"  FilePath: {doc.FilePath}\n";
                debug += $"  UserID: {doc.UserID}, CategoryID: {doc.CategoryID}, Giá: {(doc.IsPaid ? doc.Price.ToString() : "Miễn phí")}\n\n";
            }
            
            // Nếu có file upload trong form này, thử lưu nó
            if (file != null && file.Length > 0)
            {
                try
                {
                    // Tạo tên file duy nhất
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                    string filePath = Path.Combine(documentsPath, uniqueFileName);
                    
                    // Lưu file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    
                    debug += "\n------ KẾT QUẢ UPLOAD TEST ------\n";
                    debug += $"Upload thành công: {filePath}\n";
                    debug += $"File URL: /uploads/documents/{uniqueFileName}\n";
                }
                catch (Exception ex)
                {
                    debug += "\n------ LỖI UPLOAD TEST ------\n";
                    debug += $"Lỗi: {ex.Message}\n";
                    if (ex.InnerException != null)
                    {
                        debug += $"Inner Exception: {ex.InnerException.Message}\n";
                    }
                }
            }
            
            // Hiển thị thông tin debug
            ViewBag.DebugInfo = debug;
            return View();
        }

        // GET: Admin/Document/Approve/5
        public async Task<IActionResult> Approve(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents
                .Include(d => d.Author)
                .Include(d => d.Category)
                .Include(d => d.Publisher)
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.DocumentID == id);

            if (document == null)
            {
                return NotFound();
            }

            return View(document);
        }
        
        // POST: Admin/Document/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            
            if (document == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài liệu với ID này!";
                return RedirectToAction(nameof(Index));
            }
            
            try
            {
                // Kiểm tra trạng thái hiện tại
                string currentStatus = document.Status;
                
                // Chỉ có thể duyệt tài liệu đang ở trạng thái Pending
                if (currentStatus != "Pending")
                {
                    TempData["ErrorMessage"] = $"Không thể duyệt tài liệu - Trạng thái hiện tại: {currentStatus}. Chỉ có thể duyệt tài liệu đang ở trạng thái chờ xét duyệt!";
                    return RedirectToAction(nameof(Index));
                }
                
                // Kiểm tra file có tồn tại không
                if (string.IsNullOrEmpty(document.FilePath))
                {
                    TempData["ErrorMessage"] = "Tài liệu không có đường dẫn file hợp lệ!";
                    return RedirectToAction(nameof(Index));
                }
                
                // Kiểm tra đường dẫn tệp vật lý
                string physicalPath = Path.Combine(_hostEnvironment.WebRootPath, document.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (!System.IO.File.Exists(physicalPath))
                {
                    TempData["WarningMessage"] = $"Cảnh báo: File vật lý không tồn tại tại đường dẫn {physicalPath}, nhưng vẫn tiếp tục phê duyệt tài liệu trong database.";
                }
                
                // Cập nhật trạng thái tài liệu - vẫn giữ "Approved" để đồng nhất với logic người dùng tạo
                document.Status = "Approved";
                _context.Update(document);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Tài liệu đã được duyệt thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi phê duyệt tài liệu: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // GET: Admin/Document/Reject/5
        public async Task<IActionResult> Reject(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents
                .Include(d => d.Author)
                .Include(d => d.Category)
                .Include(d => d.Publisher)
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.DocumentID == id);

            if (document == null)
            {
                return NotFound();
            }

            return View(document);
        }
        
        // POST: Admin/Document/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectReason)
        {
            var document = await _context.Documents.FindAsync(id);
            
            if (document == null)
            {
                return NotFound();
            }
            
            // Chỉ có thể từ chối tài liệu đang ở trạng thái Pending
            if (document.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Chỉ có thể từ chối tài liệu đang ở trạng thái chờ xét duyệt!";
                return RedirectToAction(nameof(Index));
            }
            
            // Cập nhật trạng thái tài liệu
            document.Status = "Rejected";
            _context.Update(document);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Tài liệu đã bị từ chối thành công!";
            return RedirectToAction(nameof(Index));
        }
        
        // GET: Admin/Document/Pending
        public async Task<IActionResult> Pending(int page = 1)
        {
            // Chuyển hướng đến Index với tham số status=Pending
            return RedirectToAction(nameof(Index), new { status = "Pending", page = page });
        }

        private bool DocumentExists(int id)
        {
            return _context.Documents.Any(e => e.DocumentID == id);
        }
    }
} 