using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using SenseLib.Services;
using Microsoft.Extensions.Logging;

namespace SenseLib.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly DataContext _context;
        private readonly IFavoriteService _favoriteService;
        private readonly ILogger<UserController> _logger;

        public UserController(DataContext context, IFavoriteService favoriteService, ILogger<UserController> logger)
        {
            _context = context;
            _favoriteService = favoriteService;
            _logger = logger;
        }

        // GET: User/Downloads
        public async Task<IActionResult> Downloads(int page = 1)
        {
            // Mặc định hiển thị 10 tài liệu mỗi trang
            int pageSize = 10;

            // Lấy ID của người dùng đã đăng nhập
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Lấy danh sách lượt tải của người dùng
            var downloadsQuery = _context.Downloads
                .Where(d => d.UserID == userId)
                .Include(d => d.Document)
                    .ThenInclude(d => d.Author)
                .Include(d => d.Document)
                    .ThenInclude(d => d.Category)
                .OrderByDescending(d => d.DownloadDate)
                .AsQueryable();

            // Tính tổng số trang
            int totalItems = await downloadsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            // Điều chỉnh trang hiện tại nếu cần
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // Lấy danh sách tài liệu cho trang hiện tại
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

        // GET: User/Favorites
        public async Task<IActionResult> Favorites(int page = 1)
        {
            try
            {
                // Mặc định hiển thị 10 tài liệu mỗi trang
                int pageSize = 10;

                // Lấy ID của người dùng đã đăng nhập
                int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                _logger.LogInformation("Truy cập trang yêu thích của người dùng {UserId}, trang {Page}", userId, page);

                // Kiểm tra người dùng có tồn tại không
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Người dùng {UserId} không tồn tại", userId);
                    return NotFound("Không tìm thấy thông tin người dùng");
                }

                // Lấy tổng số tài liệu yêu thích
                int totalItems = await _favoriteService.GetUserFavoritesCount(userId);
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                
                // Điều chỉnh trang hiện tại nếu cần
                if (page < 1) page = 1;
                if (page > totalPages && totalPages > 0) page = totalPages;

                // Lấy danh sách tài liệu yêu thích
                var documents = await _favoriteService.GetUserFavorites(userId, page, pageSize);
                
                _logger.LogInformation("Đã lấy {Count} tài liệu yêu thích cho người dùng {UserId}", 
                    documents.Count, userId);

                // Truyền dữ liệu phân trang cho view
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;
                
                // Chuyển đổi danh sách Document thành danh sách Favorite để phù hợp với model của view
                var favoritesList = documents.Select(d => new Favorite 
                { 
                    DocumentID = d.DocumentID,
                    UserID = userId,
                    Document = d
                }).ToList();
                
                _logger.LogInformation("Đã chuyển đổi {Count} Document thành Favorite để hiển thị", favoritesList.Count);
                
                // Trả về danh sách favorites cho view
                return View(favoritesList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách yêu thích: {Message}", ex.Message);
                TempData["Error"] = "Có lỗi xảy ra khi lấy danh sách yêu thích: " + ex.Message;
                return View(new List<Favorite>());
            }
        }
    }
} 