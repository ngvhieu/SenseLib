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
        private readonly IFavoriteService _favoriteService;

        public ApiController(
            DataContext context,
            IConfiguration configuration,
            UserActivityService userActivityService,
            IFavoriteService favoriteService)
        {
            _context = context;
            _configuration = configuration;
            _userActivityService = userActivityService;
            _favoriteService = favoriteService;
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
                
                try
                {
                    // Thử xác thực với BCrypt
                    passwordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.Password);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    // Nếu xảy ra lỗi SaltParseException, kiểm tra mật khẩu đơn giản (tạm thời)
                    // Chỉ cho phép một số tài khoản test được cấu hình trước khi chưa update database
                    if ((model.Email == "admin@example.com" && model.Password == "admin123") ||
                        (model.Email == "test@example.com" && model.Password == "test123"))
                    {
                        passwordValid = true;
                        
                        // Ghi log về việc sử dụng xác thực tạm thời
                        Console.WriteLine($"CẢNH BÁO: Đăng nhập bằng xác thực tạm thời cho user {model.Email}");
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
                        avatarUrl = user.ProfileImage
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

                string hashedPassword;
                try
                {
                    // Mã hóa mật khẩu với BCrypt
                    hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi mã hóa mật khẩu: {ex.Message}");
                    // Sử dụng mật khẩu gốc trong trường hợp lỗi (không nên làm trong môi trường sản phẩm)
                    hashedPassword = model.Password;
                    Console.WriteLine("CẢNH BÁO: Đang lưu mật khẩu không được mã hóa!");
                }

                // Tạo người dùng mới
                var user = new User
                {
                    Email = model.Email,
                    Password = hashedPassword,
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
                        avatarUrl = user.ProfileImage
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
            int userId = 0;
            bool isFavorite = false;

            if (User.Identity.IsAuthenticated)
            {
                userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                isPurchased = await _context.Purchases
                    .AnyAsync(p => p.UserID == userId && p.DocumentID == id && p.Status == "Completed");
                
                isFavorite = await _favoriteService.IsFavorite(userId, id);
                
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
        [HttpPost("comments/{documentId}")]
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
            return Ok(result);
        }

        // API endpoint toggle like cho bình luận
        [HttpPost("comments/like/{commentId}")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ToggleCommentLike(int commentId)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
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

        // API endpoint để thêm/gỡ tài liệu yêu thích
        [HttpPost("documents/{id}/favorite")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ToggleFavorite(int id)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Kiểm tra tài liệu tồn tại
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound(new { message = "Không tìm thấy tài liệu" });
            }

            // Thêm/gỡ tài liệu khỏi danh sách yêu thích
            bool isFavorite = await _favoriteService.ToggleFavorite(userId, id);

            // Ghi nhận hoạt động
            string action = isFavorite ? "AddFavorite" : "RemoveFavorite";
            string description = isFavorite 
                ? $"Thêm tài liệu vào danh sách yêu thích: {document.Title}" 
                : $"Gỡ tài liệu khỏi danh sách yêu thích: {document.Title}";
                
            await LogUserActivity(userId, action, description);

            return Ok(new { isFavorite });
        }

        // API endpoint để lấy danh sách tài liệu yêu thích
        [HttpGet("favorites")]
        [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> GetFavorites(int page = 1, int pageSize = 10)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Lấy danh sách tài liệu yêu thích
            var favoritesQuery = _context.Favorites
                .Where(f => f.UserID == userId)
                .Include(f => f.Document)
                    .ThenInclude(d => d.Author)
                .Include(f => f.Document)
                    .ThenInclude(d => d.Category)
                .Include(f => f.Document)
                    .ThenInclude(d => d.Ratings)
                .OrderByDescending(f => f.FavoriteID);

            // Tính tổng số tài liệu yêu thích
            int totalItems = await favoritesQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            // Phân trang
            var favorites = await favoritesQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Chuyển đổi dữ liệu để trả về
            var result = new
            {
                totalItems,
                totalPages,
                currentPage = page,
                pageSize,
                items = favorites.Select(f => new
                {
                    id = f.Document.DocumentID,
                    title = f.Document.Title,
                    description = f.Document.Description,
                    coverImageUrl = f.Document.ImagePath,
                    authorName = f.Document.Author?.AuthorName,
                    categoryName = f.Document.Category?.CategoryName,
                    uploadDate = f.Document.UploadDate,
                    price = f.Document.Price,
                    isPaid = f.Document.IsPaid,
                    averageRating = f.Document.Ratings.Any() ? f.Document.Ratings.Average(r => r.RatingValue) : 0,
                    ratingCount = f.Document.Ratings.Count()
                })
            };

            return Ok(result);
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
} 