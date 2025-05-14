using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using Microsoft.AspNetCore.Antiforgery;
using SenseLib.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace SenseLib.Controllers
{
    public class DocumentController : Controller
    {
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IAntiforgery _antiforgery;
        private readonly IFavoriteService _favoriteService;
        private readonly ILogger<DocumentController> _logger;

        public DocumentController(
            DataContext context, 
            IWebHostEnvironment hostEnvironment, 
            IAntiforgery antiforgery, 
            IFavoriteService favoriteService,
            ILogger<DocumentController> logger)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _antiforgery = antiforgery;
            _favoriteService = favoriteService;
            _logger = logger;
        }

        // GET: Document
        public async Task<IActionResult> Index(string searchString, int? categoryId, string sortOrder, string priceType = "all", int page = 1)
        {
            // Mặc định hiển thị 12 tài liệu mỗi trang
            int pageSize = 12;

            // Lấy danh sách tài liệu đã được xuất bản/phê duyệt
            var documentsQuery = _context.Documents
                .Where(d => d.Status == "Approved" || d.Status == "Published")
                .Include(d => d.Author)
                .Include(d => d.Category)
                .Include(d => d.Publisher)
                .AsQueryable();

            // Lọc theo danh mục nếu có
            if (categoryId.HasValue && categoryId > 0)
            {
                documentsQuery = documentsQuery.Where(d => d.CategoryID == categoryId);
            }

            // Lọc theo loại tài liệu (miễn phí/có phí)
            if (priceType == "free")
            {
                documentsQuery = documentsQuery.Where(d => !d.IsPaid);
                ViewBag.PriceType = "free";
            }
            else if (priceType == "paid")
            {
                documentsQuery = documentsQuery.Where(d => d.IsPaid);
                ViewBag.PriceType = "paid";
            }
            else
            {
                ViewBag.PriceType = "all";
            }

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                // Tìm kiếm theo tiêu đề, mô tả, tên tác giả
                documentsQuery = documentsQuery.Where(d => 
                    d.Title.Contains(searchString) ||
                    (d.Description != null && d.Description.Contains(searchString)) ||
                    (d.Author != null && d.Author.AuthorName.Contains(searchString)));
                
                // Lưu từ khóa tìm kiếm để hiển thị trong view
                ViewBag.CurrentSearch = searchString;
            }

            // Sắp xếp kết quả
            switch (sortOrder)
            {
                case "title":
                    documentsQuery = documentsQuery.OrderBy(d => d.Title);
                    break;
                case "title_desc":
                    documentsQuery = documentsQuery.OrderByDescending(d => d.Title);
                    break;
                case "date":
                    documentsQuery = documentsQuery.OrderBy(d => d.UploadDate);
                    break;
                case "date_desc":
                default:
                    documentsQuery = documentsQuery.OrderByDescending(d => d.UploadDate);
                    break;
            }

            // Tính tổng số trang
            int totalItems = await documentsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            // Điều chỉnh trang hiện tại nếu cần
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // Lấy danh sách tài liệu cho trang hiện tại
            var documents = await documentsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Kiểm tra các tài liệu người dùng đã mua (nếu đăng nhập)
            var purchasedDocuments = new List<int>();
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                purchasedDocuments = await _context.Purchases
                    .Where(p => p.UserID == userId && p.Status == "Completed")
                    .Select(p => p.DocumentID)
                    .ToListAsync();
                
                ViewBag.PurchasedDocuments = purchasedDocuments;
            }
            
            // Truyền dữ liệu phân trang cho view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.SortOrder = sortOrder;
            
            // Truyền danh sách danh mục để hiển thị bộ lọc
            ViewBag.Categories = await _context.Categories.ToListAsync();
            
            return View(documents);
        }

        // GET: Document/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Lấy thông tin tài liệu kèm các thông tin liên quan
            var document = await _context.Documents
                .Include(d => d.Author)
                .Include(d => d.Category)
                .Include(d => d.Publisher)
                .Include(d => d.Comments.Where(c => c.ParentCommentID == null))  // Chỉ lấy các comment gốc
                    .ThenInclude(c => c.User)
                .Include(d => d.Comments.Where(c => c.ParentCommentID == null))  // Lặp lại để join thêm các replies
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .Include(d => d.Comments)
                    .ThenInclude(c => c.CommentLikes)
                .Include(d => d.Ratings)
                .Include(d => d.Statistics)
                .Include(d=> d.Downloads)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (document == null)
            {
                return NotFound();
            }

            // Tăng lượt xem
            if (document.Statistics == null)
            {
                document.Statistics = new DocumentStatistics()
                {
                    DocumentID = document.DocumentID,
                    ViewCount = 1,
                    LastUpdated = DateTime.Now
                };
                _context.DocumentStatistics.Add(document.Statistics);
            }
            else
            {
                document.Statistics.ViewCount++;
                document.Statistics.LastUpdated = DateTime.Now;
            }
            await _context.SaveChangesAsync();

            // Lấy thông tin đánh giá trung bình
            ViewBag.AverageRating = document.Ratings.Any() 
                ? document.Ratings.Average(r => r.RatingValue) 
                : 0;

            // Lấy thông tin người dùng hiện tại nếu đã đăng nhập
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                ViewBag.UserID = userId;  // Thêm UserID vào ViewBag
                ViewBag.UserRating = document.Ratings
                    .FirstOrDefault(r => r.UserID == userId)?.RatingValue ?? 0;
                
                // Kiểm tra xem người dùng đã yêu thích tài liệu này chưa
                ViewBag.IsFavorite = await _favoriteService.IsFavorite(userId, document.DocumentID);
                
                // Kiểm tra người dùng đã mua tài liệu này chưa
                if (document.IsPaid)
                {
                    ViewBag.HasPurchased = await _context.Purchases
                        .AnyAsync(p => p.UserID == userId && p.DocumentID == document.DocumentID && p.Status == "Completed");
                }
                
                // Lấy danh sách các commentID mà người dùng đã thích
                var likedCommentIds = await _context.CommentLikes
                    .Where(cl => cl.UserID == userId)
                    .Select(cl => cl.CommentID)
                    .ToListAsync();
                
                ViewBag.LikedCommentIds = likedCommentIds;
            }

            return View(document);
        }

        // POST: Document/Rate/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int id, int ratingValue)
        {
            if (ratingValue < 1 || ratingValue > 5)
            {
                return BadRequest("Đánh giá không hợp lệ");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var document = await _context.Documents
                .Include(d => d.Ratings)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (document == null)
            {
                return NotFound();
            }

            var existingRating = document.Ratings
                .FirstOrDefault(r => r.UserID == userId);

            if (existingRating != null)
            {
                existingRating.RatingValue = ratingValue;
                existingRating.RatingDate = DateTime.Now;
            }
            else
            {
                document.Ratings.Add(new Rating
                {
                    DocumentID = id,
                    UserID = userId,
                    RatingValue = ratingValue,
                    RatingDate = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Document/ToggleFavorite/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                // Kiểm tra tài liệu có tồn tại không
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                {
                    TempData["ErrorMessage"] = "Tài liệu không tồn tại";
                    return RedirectToAction("Index");
                }
                
                // Thực hiện toggle yêu thích
                var isFavoriteNow = await _favoriteService.ToggleFavorite(userId, id);
                
                // Ghi log
                _logger.LogInformation("Người dùng {UserId} đã {Action} yêu thích tài liệu {DocumentId}", 
                    userId, isFavoriteNow ? "thêm" : "xóa", id);
                
                // Thông báo kết quả
                TempData["SuccessMessage"] = isFavoriteNow 
                    ? "Đã thêm vào danh sách yêu thích" 
                    : "Đã xóa khỏi danh sách yêu thích";
                
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi toggle yêu thích. DocumentId: {DocumentId}", id);
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        
        // POST: Document/ToggleFavoriteAjax/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavoriteAjax(int id)
        {
            _logger.LogInformation("Bắt đầu ToggleFavoriteAjax - DocumentId: {DocumentId}", id);
            
            try
            {
                // Lấy user ID
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                _logger.LogInformation("UserId: {UserId}", userId);
                
                // Kiểm tra tài liệu có tồn tại không
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                {
                    _logger.LogWarning("Tài liệu không tồn tại - ID: {DocumentId}", id);
                    return Json(new { success = false, message = "Tài liệu không tồn tại" });
                }
                
                // Thực hiện toggle favorite bằng service
                bool isFavoriteNow = await _favoriteService.ToggleFavorite(userId, id);
                
                // Ghi log
                _logger.LogInformation("Kết quả: Người dùng {UserId} đã {Action} yêu thích tài liệu {DocumentId}", 
                    userId, isFavoriteNow ? "thêm" : "xóa", id);
                
                // Trả về kết quả
                return Json(new { 
                    success = true, 
                    isFavorite = isFavoriteNow,
                    message = isFavoriteNow 
                        ? "Đã thêm vào danh sách yêu thích" 
                        : "Đã xóa khỏi danh sách yêu thích"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi toggle yêu thích qua Ajax. DocumentId: {DocumentId}", id);
                
                return Json(new { 
                    success = false, 
                    message = "Đã xảy ra lỗi khi thực hiện thao tác yêu thích. Vui lòng thử lại sau."
                });
            }
        }

        // POST: Document/Comment/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(int id, string commentText)
        {
            if (string.IsNullOrWhiteSpace(commentText))
            {
                return BadRequest("Nội dung bình luận không được để trống");
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var document = await _context.Documents
                .Include(d => d.Comments)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (document == null)
            {
                return NotFound();
            }

            document.Comments.Add(new Comment
            {
                DocumentID = id,
                UserID = userId,
                CommentText = commentText,
                CommentDate = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }
        
        // POST: Document/ReplyComment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplyComment(int documentId, int parentCommentId, string replyText)
        {
            if (string.IsNullOrWhiteSpace(replyText))
            {
                TempData["ErrorMessage"] = "Nội dung trả lời không được để trống";
                return RedirectToAction(nameof(Details), new { id = documentId });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Kiểm tra comment gốc có tồn tại không
            var parentComment = await _context.Comments
                .FirstOrDefaultAsync(c => c.CommentID == parentCommentId);
                
            if (parentComment == null)
            {
                TempData["ErrorMessage"] = "Bình luận gốc không tồn tại";
                return RedirectToAction(nameof(Details), new { id = documentId });
            }

            // Thêm bình luận mới là reply
            var reply = new Comment
            {
                DocumentID = documentId,
                UserID = userId,
                CommentText = replyText,
                CommentDate = DateTime.Now,
                ParentCommentID = parentCommentId
            };
            
            _context.Comments.Add(reply);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Đã trả lời bình luận thành công";
            return RedirectToAction(nameof(Details), new { id = documentId });
        }
        
        // POST: Document/LikeComment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LikeComment(int documentId, int commentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Kiểm tra bình luận có tồn tại không
            var comment = await _context.Comments
                .Include(c => c.CommentLikes)
                .FirstOrDefaultAsync(c => c.CommentID == commentId);
                
            if (comment == null)
            {
                return Json(new { success = false, message = "Bình luận không tồn tại" });
            }

            // Kiểm tra người dùng đã thích bình luận này chưa
            var existingLike = await _context.CommentLikes
                .FirstOrDefaultAsync(cl => cl.CommentID == commentId && cl.UserID == userId);
                
            bool isLiked = false;
            
            if (existingLike != null)
            {
                // Nếu đã thích thì bỏ thích
                _context.CommentLikes.Remove(existingLike);
                comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
                isLiked = false;
            }
            else
            {
                // Nếu chưa thích thì thêm thích
                _context.CommentLikes.Add(new CommentLike
                {
                    CommentID = commentId,
                    UserID = userId,
                    LikeDate = DateTime.Now
                });
                comment.LikeCount++;
                isLiked = true;
            }
            
            await _context.SaveChangesAsync();
            
            return Json(new { 
                success = true, 
                isLiked = isLiked, 
                likeCount = comment.LikeCount,
                commentId = commentId
            });
        }
        
        // POST: Document/DeleteComment
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int documentId, int commentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            
            // Kiểm tra bình luận có tồn tại không
            var comment = await _context.Comments
                .FirstOrDefaultAsync(c => c.CommentID == commentId);
                
            if (comment == null)
            {
                TempData["ErrorMessage"] = "Bình luận không tồn tại";
                return RedirectToAction(nameof(Details), new { id = documentId });
            }

            // Chỉ cho phép chủ bình luận hoặc admin xóa
            if (comment.UserID != userId && userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa bình luận này";
                return RedirectToAction(nameof(Details), new { id = documentId });
            }
            
            // Xóa bình luận
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Đã xóa bình luận thành công";
            return RedirectToAction(nameof(Details), new { id = documentId });
        }

        // GET: Document/Download/5
        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var document = await _context.Documents
                .Include(d => d.Downloads)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (document == null)
            {
                return NotFound();
            }

            // Kiểm tra xem người dùng đã mua tài liệu này chưa (nếu là tài liệu có phí)
            if (document.IsPaid)
            {
                var hasPurchased = await _context.Purchases
                    .AnyAsync(p => p.UserID == userId && p.DocumentID == id && p.Status == "Completed");
                
                if (!hasPurchased)
                {
                    TempData["ErrorMessage"] = "Bạn cần mua tài liệu này để có thể tải xuống.";
                    return RedirectToAction(nameof(Details), new { id });
                }
            }

            // Ghi lại lượt tải
            document.Downloads.Add(new Download
            {
                DocumentID = id,
                UserID = userId,
                DownloadDate = DateTime.Now
            });
            await _context.SaveChangesAsync();

            // Check if file exists
            string filePath = Path.Combine(_hostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            // Return file for download
            return PhysicalFile(filePath, "application/octet-stream", Path.GetFileName(filePath));
        }

        // POST: Document/DownloadAjax/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DownloadAjax(int id)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var document = await _context.Documents
                    .Include(d => d.Downloads)
                    .FirstOrDefaultAsync(d => d.DocumentID == id);

                if (document == null)
                {
                    return Json(new { success = false, message = "Tài liệu không tồn tại" });
                }

                // Kiểm tra xem người dùng đã mua tài liệu này chưa (nếu là tài liệu có phí)
                if (document.IsPaid)
                {
                    var hasPurchased = await _context.Purchases
                        .AnyAsync(p => p.UserID == userId && p.DocumentID == id && p.Status == "Completed");
                    
                    if (!hasPurchased)
                    {
                        return Json(new { 
                            success = false, 
                            message = "Bạn cần mua tài liệu này để có thể tải xuống.",
                            requirePurchase = true,
                            documentId = id
                        });
                    }
                }

                // Ghi lại lượt tải
                document.Downloads.Add(new Download
                {
                    DocumentID = id,
                    UserID = userId,
                    DownloadDate = DateTime.Now
                });
                await _context.SaveChangesAsync();

                // Kiểm tra file tồn tại
                string filePath = Path.Combine(_hostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
                if (!System.IO.File.Exists(filePath))
                {
                    return Json(new { success = false, message = "File không tồn tại" });
                }

                // Trả về đường dẫn để tải file
                var fileUrl = Url.Content("~" + document.FilePath);
                return Json(new { 
                    success = true, 
                    fileUrl = fileUrl,
                    fileName = Path.GetFileName(filePath)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Document/Read/5
        [HttpGet]
        public async Task<IActionResult> Read(int id, int page = 1)
        {
            var document = await _context.Documents
                .Include(d => d.Author)
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.DocumentID == id);
                
            if (document == null)
            {
                return NotFound();
            }

            // Kiểm tra đường dẫn tệp
            string filePath = Path.Combine(_hostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                TempData["ErrorMessage"] = "Tài liệu không tồn tại.";
                return RedirectToAction(nameof(Details), new { id });
            }
            
            // Đặt giới hạn số trang tối đa được đọc miễn phí là 5 trang
            int maxPreviewPages = 5;
            bool hasPurchased = false;
            
            // Nếu người dùng đã đăng nhập, kiểm tra đã mua tài liệu chưa
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                // Kiểm tra nếu là tài liệu có phí và người dùng đã mua
                if (document.IsPaid)
                {
                    hasPurchased = await _context.Purchases
                        .AnyAsync(p => p.UserID == userId && p.DocumentID == id && p.Status == "Completed");
                }
            }
            
            // Kiểm tra số trang người dùng được phép xem
            int totalPages = GetDocumentPageCount(filePath);
            int allowedPages = hasPurchased ? totalPages : Math.Min(maxPreviewPages, totalPages);
            
            // Nếu người dùng đang cố xem trang vượt quá giới hạn cho phép
            if (page > allowedPages)
            {
                // Giới hạn trang hiện tại không vượt quá số trang được phép
                page = allowedPages;
                if (document.IsPaid && !hasPurchased)
                {
                    TempData["WarningMessage"] = $"Bạn chỉ được xem tối đa {maxPreviewPages} trang đầu tiên. Hãy mua tài liệu để xem toàn bộ nội dung.";
                }
            }

            // Cập nhật lượt xem
            var stats = await _context.DocumentStatistics.FirstOrDefaultAsync(s => s.DocumentID == id);
            if (stats == null)
            {
                // Tạo mới nếu chưa có thống kê
                stats = new DocumentStatistics
                {
                    DocumentID = document.DocumentID,
                    ViewCount = 1,
                    LastUpdated = DateTime.Now
                };
                _context.DocumentStatistics.Add(stats);
            }
            else
            {
                // Cập nhật lượt xem
                stats.ViewCount++;
                stats.LastUpdated = DateTime.Now;
                _context.Update(stats);
            }
            await _context.SaveChangesAsync();

            // Truyền thông tin tài liệu và trang đang xem cho view
            ViewBag.Document = document;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.AllowedPages = allowedPages;
            ViewBag.HasPurchased = hasPurchased;
            ViewBag.IsPaid = document.IsPaid;
            ViewBag.DocumentTitle = document.Title;
            ViewBag.FilePath = document.FilePath;
            
            return View();
        }
        
        // Hàm phụ trợ để lấy số trang của tài liệu
        private int GetDocumentPageCount(string filePath)
        {
            // Trong thực tế, sẽ cần sử dụng thư viện để đọc số trang của file PDF, DOCX, ...
            // Đây là một triển khai đơn giản cho mục đích demo
            
            string fileExtension = Path.GetExtension(filePath).ToLower();
            
            // Giả định số trang dựa trên kích thước file
            FileInfo fileInfo = new FileInfo(filePath);
            long fileSize = fileInfo.Length;
            
            // Ước tính số trang (đây chỉ là ví dụ)
            int estimatedPages;
            
            if (fileExtension == ".pdf")
            {
                // Ước tính cho file PDF: 100KB ~ 1 trang
                estimatedPages = (int)(fileSize / 102400) + 1;
            }
            else if (fileExtension == ".docx" || fileExtension == ".doc")
            {
                // Ước tính cho file DOCX: 50KB ~ 1 trang
                estimatedPages = (int)(fileSize / 51200) + 1;
            }
            else
            {
                // Mặc định
                estimatedPages = 10;
            }
            
            // Đảm bảo có ít nhất 1 trang
            return Math.Max(1, estimatedPages);
        }

        // GET: Document/Favorites
        [Authorize]
        public async Task<IActionResult> Favorites(int page = 1)
        {
            // Lấy ID người dùng hiện tại
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Số lượng tài liệu trên mỗi trang
            int pageSize = 12;
            
            // Lấy tổng số tài liệu yêu thích
            int totalItems = await _favoriteService.GetUserFavoritesCount(userId);
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            // Điều chỉnh trang hiện tại nếu cần
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            
            // Lấy danh sách tài liệu yêu thích cho trang hiện tại
            var favorites = await _favoriteService.GetUserFavorites(userId, page, pageSize);
            
            // Lấy danh sách tài liệu đã mua
            var purchasedDocuments = await _context.Purchases
                .Where(p => p.UserID == userId && p.Status == "Completed")
                .Select(p => p.DocumentID)
                .ToListAsync();
            
            // Truyền dữ liệu phân trang cho view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PurchasedDocuments = purchasedDocuments;
            
            return View(favorites);
        }

        // GET: Document/DownloadHistory
        [Authorize]
        public async Task<IActionResult> DownloadHistory(int page = 1)
        {
            // Lấy ID người dùng hiện tại
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Số lượng tài liệu trên mỗi trang
            int pageSize = 12;
            
            // Lấy lịch sử tải xuống của người dùng
            var downloadsQuery = _context.Downloads
                .Where(d => d.UserID == userId)
                .Include(d => d.Document)
                    .ThenInclude(d => d.Author)
                .Include(d => d.Document)
                    .ThenInclude(d => d.Category)
                .Include(d => d.Document)
                    .ThenInclude(d => d.Publisher)
                .Include(d => d.Document)
                    .ThenInclude(d => d.Statistics)
                .OrderByDescending(d => d.DownloadDate)
                .AsQueryable();
            
            // Tính tổng số trang
            int totalItems = await downloadsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            // Điều chỉnh trang hiện tại nếu cần
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            
            // Lấy danh sách tải xuống cho trang hiện tại
            var downloads = await downloadsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            // Truyền dữ liệu phân trang cho view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            
            return View(downloads);
        }

        // API để lấy danh sách danh mục cho AJAX
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.Status == "Active")
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new { categoryID = c.CategoryID, categoryName = c.CategoryName })
                    .ToListAsync();
                
                return Json(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách danh mục");
                return Json(new List<object>());
            }
        }
    }
} 