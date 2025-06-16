using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SenseLib.Models;
using SenseLib.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Security.Cryptography;
using BCrypt.Net;

namespace SenseLib.Controllers
{
    [Route("api")]
    [ApiController]
    [EnableCors("AllowAndroid")]
    public class ApiController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserActivityService _userActivityService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IDocumentConverterService _documentConverterService;

        public ApiController(
            DataContext context,
            IConfiguration configuration,
            UserActivityService userActivityService,
            IWebHostEnvironment hostEnvironment,
            IDocumentConverterService documentConverterService)
        {
            _context = context;
            _configuration = configuration;
            _userActivityService = userActivityService;
            _hostEnvironment = hostEnvironment;
            _documentConverterService = documentConverterService;
        }

        // API endpoint đăng nhập - Trả về JWT Token
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                // Kiểm tra thông tin đăng nhập
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user == null)
                {
                    return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });
                }

                bool passwordValid = false;
                
                // Kiểm tra theo 3 cách:
                // 1. Kiểm tra trực tiếp (khi Android đã hash)
                if (user.Password == model.Password)
                {
                    passwordValid = true;
                    Console.WriteLine("Xác thực thành công: Mật khẩu đã được hash từ Android");
                }
                // 2. Hash mật khẩu bằng SHA-256 rồi so sánh (khi Android gửi mật khẩu thô)
                else if (user.Password == HashPassword(model.Password))
                {
                    passwordValid = true;
                    Console.WriteLine("Xác thực thành công: Mật khẩu được hash bởi server (SHA-256)");
                }
                // 3. Kiểm tra bằng BCrypt (cho các tài khoản cũ)
                else
                {
                    try
                    {
                        passwordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.Password);
                        if (passwordValid)
                        {
                            Console.WriteLine("Xác thực thành công: Mật khẩu được verify bởi BCrypt");
                            
                            // Cập nhật mật khẩu sang định dạng mới (SHA-256)
                            user.Password = HashPassword(model.Password);
                            _context.Update(user);
                            await _context.SaveChangesAsync();
                            Console.WriteLine("Đã cập nhật mật khẩu sang định dạng SHA-256");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Không thể xác thực bằng BCrypt: {ex.Message}");
                    }
                }

                if (!passwordValid)
                {
                    return Unauthorized(new { message = "Email hoặc mật khẩu không đúng" });
                }

                // Tạo JWT token
                var token = GenerateJwtToken(user);

                // Ghi nhận hoạt động đăng nhập
                await LogUserActivity(user.UserID, "Login", "Đăng nhập vào ứng dụng di động");

                // Trả về thông tin người dùng và token
                return Ok(new
                {
                    token,
                    user = new
                    {
                        id = user.UserID,
                        name = user.FullName,
                        email = user.Email,
                        avatarUrl = string.IsNullOrEmpty(user.ProfileImage) ? null : $"/uploads/profiles/{user.ProfileImage}"
                    }
                });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi chi tiết
                Console.WriteLine($"Lỗi đăng nhập: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // API endpoint đăng ký
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                // Kiểm tra email đã tồn tại
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    return BadRequest(new { message = "Email đã được sử dụng" });
                }

                // Kiểm tra xem mật khẩu đã được hash từ Android chưa
                string password = model.Password;
                
                // Kiểm tra độ dài: nếu dài 44 ký tự và kết thúc bằng = thì là chuỗi đã được hash bằng Base64
                bool isAlreadyHashed = password.Length == 44 && password.EndsWith("=");
                
                if (!isAlreadyHashed)
                {
                    // Nếu mật khẩu chưa được hash, thực hiện hash tại server
                    password = HashPassword(model.Password);
                    Console.WriteLine("Đã hash mật khẩu tại server");
                }
                else
                {
                    Console.WriteLine("Sử dụng mật khẩu đã được hash từ Android");
                }

                // Tạo người dùng mới
                var user = new User
                {
                    Email = model.Email,
                    Password = password,
                    FullName = model.FullName,
                    Username = model.Email, // Sử dụng email làm username
                    Role = "User",
                    Status = "Active"
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                // Tạo ví cho người dùng mới
                var wallet = new Wallet
                {
                    UserID = user.UserID,
                    Balance = 0,
                    CreatedDate = DateTime.Now,
                    LastUpdatedDate = DateTime.Now
                };

                await _context.Wallets.AddAsync(wallet);
                await _context.SaveChangesAsync();

                // Ghi nhận hoạt động đăng ký
                await LogUserActivity(user.UserID, "Register", "Đăng ký tài khoản mới từ ứng dụng di động");

                // Tạo JWT token
                var token = GenerateJwtToken(user);

                // Trả về thông tin người dùng và token
                return Ok(new
                {
                    token,
                    user = new
                    {
                        id = user.UserID,
                        name = user.FullName,
                        email = user.Email,
                        avatarUrl = string.IsNullOrEmpty(user.ProfileImage) ? null : $"/uploads/profiles/{user.ProfileImage}"
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đăng ký: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // API endpoint lấy danh sách tài liệu
        [HttpGet("documents")]
        public async Task<IActionResult> GetDocuments(string search = "", int categoryId = 0, string sortOrder = "date_desc", string priceType = "all", int page = 1, int pageSize = 10)
        {
            // Xây dựng truy vấn
            var documentsQuery = _context.Documents
                .Where(d => d.Status == "Approved" || d.Status == "Published")
                .Include(d => d.Author)
                .Include(d => d.Category)
                .Include(d => d.Publisher)
                .Include(d => d.Ratings)
                .Include(d => d.Statistics)
                .AsQueryable();

            // Lọc theo danh mục nếu có
            if (categoryId > 0)
            {
                documentsQuery = documentsQuery.Where(d => d.CategoryID == categoryId);
            }

            // Lọc theo loại tài liệu (miễn phí/có phí)
            if (priceType == "free")
            {
                documentsQuery = documentsQuery.Where(d => !d.IsPaid);
            }
            else if (priceType == "paid")
            {
                documentsQuery = documentsQuery.Where(d => d.IsPaid);
            }

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                documentsQuery = documentsQuery.Where(d => 
                    d.Title.Contains(search) ||
                    (d.Description != null && d.Description.Contains(search)) ||
                    (d.Author != null && d.Author.AuthorName.Contains(search)));
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

            // Tính tổng số tài liệu
            int totalItems = await documentsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            // Phân trang
            var documents = await documentsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Kiểm tra các tài liệu người dùng đã mua (nếu đăng nhập)
            var purchasedDocuments = new List<int>();
            var userId = 0;
            
            if (User.Identity.IsAuthenticated)
            {
                userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                purchasedDocuments = await _context.Purchases
                    .Where(p => p.UserID == userId && p.Status == "Completed")
                    .Select(p => p.DocumentID)
                    .ToListAsync();
            }

            // Chuyển đổi dữ liệu để trả về
            var result = new
            {
                totalItems,
                totalPages,
                currentPage = page,
                pageSize,
                items = documents.Select(d => new
                {
                    id = d.DocumentID,
                    title = d.Title,
                    description = d.Description,
                    coverImageUrl = d.ImagePath,
                    authorName = d.Author?.AuthorName,
                    categoryName = d.Category?.CategoryName,
                    uploadDate = d.UploadDate,
                    price = d.Price,
                    isPaid = d.IsPaid,
                    isPurchased = purchasedDocuments.Contains(d.DocumentID),
                    averageRating = d.Ratings.Any() ? d.Ratings.Average(r => r.RatingValue) : 0,
                    ratingCount = d.Ratings.Count(),
                    viewCount = d.Statistics?.ViewCount ?? 0
                })
            };

            return Ok(result);
        }

        // API endpoint lấy chi tiết tài liệu
        [HttpGet("documents/{id}")]
        public async Task<IActionResult> GetDocumentDetails(int id)
        {
            var document = await _context.Documents
                .Include(d => d.Author)
                .Include(d => d.Category)
                .Include(d => d.Publisher)
                .Include(d => d.Ratings)
                .Include(d => d.Statistics)
                .Include(d => d.Comments.Where(c => c.ParentCommentID == null))
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (document == null)
            {
                return NotFound(new { message = "Không tìm thấy tài liệu" });
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
            
            // Kiểm tra người dùng đã mua tài liệu chưa
            bool isPurchased = false;
            bool isFavorite = false;
            int userId = 0;

            if (User.Identity.IsAuthenticated)
            {
                userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                isPurchased = await _context.Purchases
                    .AnyAsync(p => p.UserID == userId && p.DocumentID == id && p.Status == "Completed");
                
                isFavorite = await _context.Favorites.AnyAsync(f => f.UserID == userId && f.DocumentID == id);
                
                // Ghi nhận hoạt động xem chi tiết tài liệu
                await LogUserActivity(userId, "View", $"Xem chi tiết tài liệu: {document.Title}");
            }

            // Chuyển đổi dữ liệu để trả về
            var result = new
            {
                id = document.DocumentID,
                title = document.Title,
                description = document.Description,
                coverImageUrl = document.ImagePath,
                filePath = document.FilePath,
                authorName = document.Author?.AuthorName,
                categoryName = document.Category?.CategoryName,
                publisherName = document.Publisher?.PublisherName,
                uploadDate = document.UploadDate,
                price = document.Price,
                isPaid = document.IsPaid,
                isPurchased = isPurchased,
                isFavorite = isFavorite,
                averageRating = document.Ratings.Any() ? document.Ratings.Average(r => r.RatingValue) : 0,
                ratingCount = document.Ratings.Count(),
                viewCount = document.Statistics?.ViewCount ?? 0,
                comments = document.Comments
                    .Where(c => c.ParentCommentID == null)
                    .OrderByDescending(c => c.CommentDate)
                    .Select(c => new
                    {
                        id = c.CommentID,
                        text = c.CommentText,
                        userName = c.User?.FullName,
                        userAvatar = c.User?.ProfileImage,
                        date = c.CommentDate,
                        likeCount = c.CommentLikes?.Count ?? 0
                    })
            };

            return Ok(result);
        }

        // API endpoint lấy danh sách bình luận của tài liệu
        [HttpGet("comments/{documentId}")]
        public async Task<IActionResult> GetComments(int documentId, int page = 1, int pageSize = 10)
        {
            var query = _context.Comments
                .Where(c => c.DocumentID == documentId && c.ParentCommentID == null)
                .Include(c => c.User)
                .Include(c => c.CommentLikes);

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            int userId = User.Identity.IsAuthenticated
                ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)
                : 0;

            var items = await query
                .OrderByDescending(c => c.CommentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var resultComments = items.Select(c => new
            {
                id = c.CommentID,
                content = c.CommentText,
                createdDate = c.CommentDate,
                userId = c.UserID,
                documentId = c.DocumentID,
                username = c.User?.Username,
                userAvatar = c.User?.ProfileImage,
                likeCount = c.LikeCount,
                isLiked = c.CommentLikes.Any(cl => cl.UserID == userId)
            });

            return Ok(new {
                totalItems,
                totalPages,
                currentPage = page,
                pageSize,
                items = resultComments
            });
        }

        // API endpoint thêm bình luận cho tài liệu
        [HttpPost("documents/{documentId}/comments")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> AddComment(int documentId, [FromBody] Dictionary<string, string> data)
        {
            if (!data.TryGetValue("commentText", out var text) || string.IsNullOrWhiteSpace(text))
                return BadRequest(new { message = "Nội dung bình luận không được để trống" });

            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
                return NotFound(new { message = "Tài liệu không tồn tại" });

            var comment = new Comment
            {
                DocumentID = documentId,
                UserID = userId,
                CommentText = text,
                CommentDate = DateTime.Now,
                LikeCount = 0
            };
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            var result = new
            {
                id = comment.CommentID,
                content = comment.CommentText,
                createdDate = comment.CommentDate,
                userId = comment.UserID,
                documentId = comment.DocumentID,
                username = user?.Username,
                userAvatar = user?.ProfileImage,
                likeCount = comment.LikeCount,
                isLiked = false
            };

            await LogUserActivity(userId, "Comment", $"Bình luận tài liệu {documentId}: '{text}'");
            return CreatedAtAction(nameof(GetComments), new { documentId = comment.DocumentID }, comment);
        }

        // API endpoint toggle like cho bình luận
        [HttpPost("comments/{commentId}/like")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ToggleCommentLike(int commentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            var comment = await _context.Comments
                .Include(c => c.CommentLikes)
                .FirstOrDefaultAsync(c => c.CommentID == commentId);

            if (comment == null)
                return NotFound(new { message = "Bình luận không tồn tại" });

            var existing = comment.CommentLikes.FirstOrDefault(cl => cl.UserID == userId);
            bool isLiked;
            if (existing != null)
            {
                _context.CommentLikes.Remove(existing);
                comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
                isLiked = false;
                await LogUserActivity(userId, "UnlikeComment", $"Bỏ thích bình luận {commentId}");
            }
            else
            {
                _context.CommentLikes.Add(new CommentLike
                {
                    CommentID = commentId,
                    UserID = userId,
                    LikeDate = DateTime.Now
                });
                comment.LikeCount++;
                isLiked = true;
                await LogUserActivity(userId, "LikeComment", $"Thích bình luận {commentId}");
            }

            await _context.SaveChangesAsync();
            return Ok(new { isLiked, likeCount = comment.LikeCount, commentId });
        }

        // API endpoint lấy danh sách danh mục
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new { id = c.CategoryID, name = c.CategoryName })
                .ToListAsync();
                
            return Ok(categories);
        }

        // API endpoint để đánh giá tài liệu
        [HttpPost("documents/{id}/rate")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> RateDocument(int id, [FromBody] RatingModel model)
        {
            // Lấy thông tin người dùng đăng nhập
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Kiểm tra tài liệu tồn tại
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound(new { message = "Không tìm thấy tài liệu" });
            }

            // Kiểm tra đánh giá đã tồn tại
            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.DocumentID == id && r.UserID == userId);

            if (existingRating != null)
            {
                // Cập nhật đánh giá
                existingRating.RatingValue = model.Rating;
                existingRating.RatingDate = DateTime.Now;
                _context.Ratings.Update(existingRating);
            }
            else
            {
                // Tạo đánh giá mới
                var rating = new Rating
                {
                    DocumentID = id,
                    UserID = userId,
                    RatingValue = model.Rating,
                    RatingDate = DateTime.Now
                };
                await _context.Ratings.AddAsync(rating);
            }

            await _context.SaveChangesAsync();

            // Ghi nhận hoạt động đánh giá
            await LogUserActivity(userId, "Rate", $"Đánh giá tài liệu: {document.Title} - {model.Rating} sao");

            // Tính toán lại đánh giá trung bình và số lượng đánh giá
            var ratings = await _context.Ratings.Where(r => r.DocumentID == id).ToListAsync();
            float averageRating = (float)ratings.Average(r => r.RatingValue);
            int ratingCount = ratings.Count;

            return Ok(new { averageRating, ratingCount });
        }

        // API endpoint để kiểm tra kết nối
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { 
                message = "Kết nối thành công!",
                timestamp = DateTime.Now,
                serverInfo = new {
                    version = "1.0",
                    status = "Online"
                }
            });
        }

        // API: Tải xuống file gốc
        [HttpGet("documents/{id}/download")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return NotFound(new { message = "Không tìm thấy tài liệu" });
            if (document.IsPaid)
            {
                var hasPurchased = await _context.Purchases.AnyAsync(p => p.UserID == userId && p.DocumentID == id && p.Status == "Completed");
                if (!hasPurchased)
                    return StatusCode(StatusCodes.Status402PaymentRequired, new { message = "Bạn cần mua tài liệu này để tải xuống." });
            }
            string filePath = Path.Combine(_hostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "Tài liệu không tồn tại." });
            string fileName = document.Title + Path.GetExtension(filePath);
            fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
            string mimeType = GetMimeType(Path.GetExtension(filePath));
            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, mimeType, fileName);
        }

        // API: Tải xuống PDF
        [HttpGet("documents/{id}/download-pdf")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> DownloadPdfDocument(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
                return NotFound(new { message = "Không tìm thấy tài liệu" });
            if (document.IsPaid)
            {
                var hasPurchased = await _context.Purchases.AnyAsync(p => p.UserID == userId && p.DocumentID == id && p.Status == "Completed");
                if (!hasPurchased)
                    return StatusCode(StatusCodes.Status402PaymentRequired, new { message = "Bạn cần mua tài liệu này để tải xuống." });
            }
            string pdfPath = null;
            string originalFilePath = Path.Combine(_hostEnvironment.WebRootPath, document.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(originalFilePath))
                return NotFound(new { message = "Tài liệu gốc không tồn tại." });
            string fileExtension = Path.GetExtension(originalFilePath).ToLower();
            if (fileExtension == ".pdf")
            {
                pdfPath = document.FilePath;
            }
            else if (_documentConverterService != null)
            {
                if (_documentConverterService.IsPdfAvailable(document.FilePath, document.DocumentID))
                {
                    pdfPath = _documentConverterService.GetPdfPath(document.FilePath, document.DocumentID);
                }
                else
                {
                    pdfPath = await _documentConverterService.ConvertToPdfAsync(document.FilePath, document.DocumentID);
                    if (string.IsNullOrEmpty(pdfPath))
                        return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể chuyển đổi tài liệu sang PDF." });
                }
            }
            else
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Dịch vụ chuyển đổi PDF không khả dụng." });
            }
            string pdfFilePath = Path.Combine(_hostEnvironment.WebRootPath, pdfPath.TrimStart('/'));
            if (!System.IO.File.Exists(pdfFilePath))
                return NotFound(new { message = "File PDF không tồn tại." });
            string fileName = document.Title + ".pdf";
            fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
            var fileBytes = System.IO.File.ReadAllBytes(pdfFilePath);
            return File(fileBytes, "application/pdf", fileName);
        }

        // API: Lấy tài liệu theo danh mục
        [HttpGet("categories/{id}/documents")]
        public async Task<IActionResult> GetDocumentsByCategory(int id, int page = 1, int pageSize = 10)
        {
            var documentsQuery = _context.Documents
                .Where(d => d.CategoryID == id && (d.Status == "Approved" || d.Status == "Published"))
                .Include(d => d.Author)
                .Include(d => d.Category)
                .Include(d => d.Publisher)
                .Include(d => d.Ratings)
                .Include(d => d.Statistics);
            int totalItems = await documentsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var documents = await documentsQuery
                .OrderByDescending(d => d.UploadDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var result = new
            {
                totalItems,
                totalPages,
                currentPage = page,
                pageSize,
                items = documents.Select(d => new
                {
                    id = d.DocumentID,
                    title = d.Title,
                    description = d.Description,
                    coverImageUrl = d.ImagePath,
                    authorName = d.Author?.AuthorName,
                    categoryName = d.Category?.CategoryName,
                    uploadDate = d.UploadDate,
                    price = d.Price,
                    isPaid = d.IsPaid,
                    averageRating = d.Ratings.Any() ? d.Ratings.Average(r => r.RatingValue) : 0,
                    ratingCount = d.Ratings.Count(),
                    viewCount = d.Statistics?.ViewCount ?? 0
                })
            };
            return Ok(result);
        }

        // API: Mua tài liệu
        [HttpPost("purchases/{documentId}")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> PurchaseDocument(int documentId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var document = await _context.Documents.FindAsync(documentId);
            if (document == null)
                return NotFound(new { message = "Không tìm thấy tài liệu" });
            if (!document.IsPaid)
                return BadRequest(new { message = "Tài liệu này là miễn phí." });
            var hasPurchased = await _context.Purchases.AnyAsync(p => p.UserID == userId && p.DocumentID == documentId && p.Status == "Completed");
            if (hasPurchased)
                return BadRequest(new { message = "Bạn đã mua tài liệu này rồi." });
            // Trừ tiền ví
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserID == userId);
            if (wallet == null || wallet.Balance < (document.Price ?? 0))
                return StatusCode(StatusCodes.Status402PaymentRequired, new { message = "Số dư ví không đủ." });
            wallet.Balance -= (document.Price ?? 0);
            wallet.LastUpdatedDate = DateTime.Now;
            // Tạo purchase
            var purchase = new Purchase
            {
                UserID = userId,
                DocumentID = documentId,
                PurchaseDate = DateTime.Now,
                Amount = document.Price ?? 0,
                Status = "Completed"
            };
            await _context.Purchases.AddAsync(purchase);
            await _context.SaveChangesAsync();
            await LogUserActivity(userId, "Purchase", $"Mua tài liệu: {document.Title}");
            return Ok(new { message = "Mua tài liệu thành công." });
        }

        // API: Lấy danh sách đã mua
        [HttpGet("purchases")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetPurchasedDocuments(int page = 1, int pageSize = 10)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var purchasesQuery = _context.Purchases
                .Where(p => p.UserID == userId && p.Status == "Completed")
                .Include(p => p.Document)
                    .ThenInclude(d => d.Author)
                .Include(p => p.Document)
                    .ThenInclude(d => d.Category)
                .Include(p => p.Document)
                    .ThenInclude(d => d.Ratings)
                .OrderByDescending(p => p.PurchaseDate);
            int totalItems = await purchasesQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var purchases = await purchasesQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var result = new
            {
                totalItems,
                totalPages,
                currentPage = page,
                pageSize,
                items = purchases.Select(p => new
                {
                    id = p.Document.DocumentID,
                    title = p.Document.Title,
                    description = p.Document.Description,
                    coverImageUrl = p.Document.ImagePath,
                    authorName = p.Document.Author?.AuthorName,
                    categoryName = p.Document.Category?.CategoryName,
                    uploadDate = p.Document.UploadDate,
                    price = p.Document.Price,
                    isPaid = p.Document.IsPaid,
                    averageRating = p.Document.Ratings.Any() ? p.Document.Ratings.Average(r => r.RatingValue) : 0,
                    ratingCount = p.Document.Ratings.Count(),
                    purchaseDate = p.PurchaseDate
                })
            };
            return Ok(result);
        }

        // API: Upload tài liệu (multipart)
        [HttpPost("documents")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        [RequestSizeLimit(52428800)] // 50MB
        public async Task<IActionResult> UploadDocument([FromForm] string title, [FromForm] string description, [FromForm] int? categoryId, [FromForm] int? authorId, [FromForm] int? publisherId, [FromForm] bool isPaid, [FromForm] decimal? price, [FromForm] IFormFile documentFile, [FromForm] IFormFile coverImage)
        {
            if (documentFile == null)
                return BadRequest(new { message = "Vui lòng chọn file tài liệu để tải lên." });
            string[] allowedExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".xlsm", ".ppt", ".pptx", ".pptm", ".txt", ".rtf", ".csv", ".odt", ".ods", ".odp", ".md", ".html", ".htm", ".xml", ".json", ".log", ".zip", ".rar", ".7z", ".mp3", ".mp4", ".avi", ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".svg" };
            string extension = Path.GetExtension(documentFile.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { message = "Định dạng file không được hỗ trợ." });
            if (documentFile.Length > 50 * 1024 * 1024)
                return BadRequest(new { message = "Kích thước file không được vượt quá 50MB." });
            string uploadsPath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "documents");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);
            string fileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadsPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await documentFile.CopyToAsync(stream);
            }
            string relativeFilePath = $"/uploads/documents/{fileName}";
            string imagePath = null;
            if (coverImage != null)
            {
                string imageExt = Path.GetExtension(coverImage.FileName).ToLower();
                string[] allowedImageExt = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
                if (!allowedImageExt.Contains(imageExt))
                    return BadRequest(new { message = "Định dạng ảnh bìa không hợp lệ." });
                string imageFileName = Guid.NewGuid().ToString() + imageExt;
                string imageUploadsPath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "images");
                if (!Directory.Exists(imageUploadsPath))
                    Directory.CreateDirectory(imageUploadsPath);
                string imageFilePath = Path.Combine(imageUploadsPath, imageFileName);
                using (var stream = new FileStream(imageFilePath, FileMode.Create))
                {
                    await coverImage.CopyToAsync(stream);
                }
                imagePath = $"/uploads/images/{imageFileName}";
            }
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var document = new Document
            {
                Title = title,
                Description = description ?? string.Empty,
                CategoryID = categoryId,
                AuthorID = authorId,
                PublisherID = publisherId,
                IsPaid = isPaid,
                Price = isPaid ? price : 0,
                FilePath = relativeFilePath,
                ImagePath = imagePath,
                UserID = userId,
                Status = "Pending",
                UploadDate = DateTime.Now
            };
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
            await LogUserActivity(userId, "Upload", $"Tải lên tài liệu: {title}");
            return Ok(new { message = "Tải lên tài liệu thành công!", documentId = document.DocumentID });
        }

        // API endpoint đổi mật khẩu
        [HttpPost("account/change-password")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            try
            {
                // Lấy ID người dùng từ token
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Không tìm thấy người dùng" });
                }

                // Kiểm tra mật khẩu hiện tại
                bool currentPasswordValid = false;
                
                // 1. Kiểm tra trực tiếp với mật khẩu đã hash từ Android
                if (user.Password == model.CurrentPassword)
                {
                    currentPasswordValid = true;
                    Console.WriteLine("Xác thực mật khẩu hiện tại thành công: Mật khẩu đã được hash từ Android");
                }
                // 2. Hash mật khẩu hiện tại và so sánh
                else if (user.Password == HashPassword(model.CurrentPassword))
                {
                    currentPasswordValid = true;
                    Console.WriteLine("Xác thực mật khẩu hiện tại thành công: Mật khẩu được hash bởi server");
                }
                // 3. Kiểm tra bằng BCrypt (cho các tài khoản cũ)
                else
                {
                    try
                    {
                        currentPasswordValid = BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.Password);
                        if (currentPasswordValid)
                        {
                            Console.WriteLine("Xác thực mật khẩu hiện tại thành công: Mật khẩu được verify bởi BCrypt");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Không thể xác thực mật khẩu hiện tại bằng BCrypt: {ex.Message}");
                    }
                }
                
                if (!currentPasswordValid)
                {
                    return BadRequest(new { message = "Mật khẩu hiện tại không chính xác" });
                }

                // Kiểm tra mật khẩu mới
                if (string.IsNullOrEmpty(model.NewPassword) || model.NewPassword.Length < 6)
                {
                    return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự" });
                }

                if (model.NewPassword != model.ConfirmPassword)
                {
                    return BadRequest(new { message = "Mật khẩu xác nhận không khớp với mật khẩu mới" });
                }

                // Kiểm tra xem mật khẩu mới đã được hash từ Android chưa
                string newPassword = model.NewPassword;
                
                // Nếu dài 44 ký tự và kết thúc bằng = thì là chuỗi đã được hash bằng Base64
                bool isAlreadyHashed = newPassword.Length == 44 && newPassword.EndsWith("=");
                
                if (!isAlreadyHashed)
                {
                    // Nếu mật khẩu chưa được hash, thực hiện hash tại server
                    newPassword = HashPassword(model.NewPassword);
                    Console.WriteLine("Đã hash mật khẩu mới tại server");
                }
                else
                {
                    Console.WriteLine("Sử dụng mật khẩu mới đã được hash từ Android");
                }

                // Cập nhật mật khẩu mới
                user.Password = newPassword;
                _context.Update(user);
                await _context.SaveChangesAsync();

                // Ghi nhận hoạt động
                await LogUserActivity(userId, "ChangePassword", "Đổi mật khẩu thành công từ ứng dụng di động");

                return Ok(new { message = "Đổi mật khẩu thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi đổi mật khẩu: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new { message = "Lỗi server: " + ex.Message });
            }
        }

        // Helper method để tạo JWT token
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "SenseLibSecretKeyForJwtAuthenticationAndAuthorization123");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.FullName ?? ""),
                    new Claim(ClaimTypes.Role, user.Role ?? "User")
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"] ?? "SenseLib",
                Audience = _configuration["Jwt:Audience"] ?? "SenseLibApp"
            };
            
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        // Phương thức hỗ trợ để ghi nhận hoạt động người dùng
        private async Task LogUserActivity(int userId, string activityType, string description)
        {
            var activity = new UserActivity
            {
                UserID = userId,
                ActivityType = activityType,
                ActivityDate = DateTime.Now,
                Description = description
            };
            
            await _context.UserActivities.AddAsync(activity);
            await _context.SaveChangesAsync();
        }

        private string GetMimeType(string extension)
        {
            switch (extension.ToLower())
            {
                case ".pdf":
                    return "application/pdf";
                case ".doc":
                    return "application/msword";
                case ".docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls":
                    return "application/vnd.ms-excel";
                case ".xlsx":
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".ppt":
                    return "application/vnd.ms-powerpoint";
                case ".pptx":
                    return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case ".txt":
                    return "text/plain";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".zip":
                    return "application/zip";
                case ".rar":
                    return "application/x-rar-compressed";
                default:
                    return "application/octet-stream";
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
    }

    // Model cho đăng nhập
    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    // Model cho đăng ký
    public class RegisterModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
    }

    // Model cho đánh giá
    public class RatingModel
    {
        public int Rating { get; set; }
    }

    // Model cho đổi mật khẩu
    public class ChangePasswordModel
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
} 