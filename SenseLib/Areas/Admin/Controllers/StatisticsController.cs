using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using Microsoft.Extensions.Logging;
using SenseLib.Utilities;

namespace SenseLib.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StatisticsController : Controller
    {
        private readonly DataContext _context;
        private readonly ILogger<StatisticsController> _logger;

        public StatisticsController(DataContext context, ILogger<StatisticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Statistics
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Truy cập trang chính thống kê");
            
            ViewData["Title"] = "Thống kê tổng quan";
            
            // Thống kê tổng quan
            ViewBag.TotalComments = await _context.Comments.CountAsync();
            ViewBag.TotalRatings = await _context.Ratings.CountAsync();
            ViewBag.TotalFavorites = await _context.Favorites.CountAsync();
            ViewBag.TotalDownloads = await _context.Downloads.CountAsync();
            
            // Thống kê người dùng
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalUsersToday = await _context.Users
                .Where(u => u.UserID > 0) // Chỉ để tránh lỗi khi không có trường CreatedDate
                .CountAsync();
            
            // Thống kê tài liệu
            ViewBag.TotalDocuments = await _context.Documents.CountAsync();
            ViewBag.TotalCategories = await _context.Categories.CountAsync();
            
            // Thống kê tài liệu nổi bật
            ViewBag.TopDownloadedDocuments = await _context.Downloads
                .GroupBy(d => d.DocumentID)
                .Select(g => new { DocumentID = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .Join(_context.Documents,
                    d => d.DocumentID,
                    doc => doc.DocumentID,
                    (d, doc) => new { Document = doc, Count = d.Count })
                .ToListAsync();
            
            // Thống kê tài liệu yêu thích nhất
            ViewBag.TopFavoritedDocuments = await _context.Favorites
                .GroupBy(f => f.DocumentID)
                .Select(g => new { DocumentID = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .Join(_context.Documents,
                    f => f.DocumentID,
                    d => d.DocumentID,
                    (f, d) => new { Document = d, Count = f.Count })
                .ToListAsync();
            
            return View();
        }

        // GET: Admin/Statistics/Comments
        public async Task<IActionResult> Comments(string sortOrder, string searchString, int? pageNumber)
        {
            _logger.LogInformation("Truy cập trang thống kê bình luận");
            
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParam"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParam"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["UserSortParam"] = sortOrder == "User" ? "user_desc" : "User";
            ViewData["DocumentSortParam"] = sortOrder == "Document" ? "document_desc" : "Document";
            ViewData["CurrentFilter"] = searchString;

            var pageSize = 10;
            var comments = _context.Comments
                .Include(c => c.User)
                .Include(c => c.Document)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                comments = comments.Where(c => 
                    c.CommentText.Contains(searchString) ||
                    c.User.Username.Contains(searchString) ||
                    c.Document.Title.Contains(searchString));
            }

            comments = sortOrder switch
            {
                "name_desc" => comments.OrderByDescending(c => c.CommentText),
                "Date" => comments.OrderBy(c => c.CommentDate),
                "date_desc" => comments.OrderByDescending(c => c.CommentDate),
                "User" => comments.OrderBy(c => c.User.Username),
                "user_desc" => comments.OrderByDescending(c => c.User.Username),
                "Document" => comments.OrderBy(c => c.Document.Title),
                "document_desc" => comments.OrderByDescending(c => c.Document.Title),
                _ => comments.OrderByDescending(c => c.CommentDate),
            };

            var paginatedComments = await PaginatedList<Comment>.CreateAsync(
                comments.AsNoTracking(), pageNumber ?? 1, pageSize);

            // Thống kê tổng quan
            ViewBag.TotalComments = await _context.Comments.CountAsync();
            ViewBag.TotalUsers = await _context.Comments.Select(c => c.UserID).Distinct().CountAsync();
            ViewBag.TotalDocuments = await _context.Comments.Select(c => c.DocumentID).Distinct().CountAsync();
            ViewBag.CommentsToday = await _context.Comments
                .Where(c => c.CommentDate.Date == DateTime.Today)
                .CountAsync();

            return View(paginatedComments);
        }

        // GET: Admin/Statistics/Ratings
        public async Task<IActionResult> Ratings(string sortOrder, string searchString, int? pageNumber)
        {
            _logger.LogInformation("Truy cập trang thống kê đánh giá");
            
            ViewData["CurrentSort"] = sortOrder;
            ViewData["RatingSortParam"] = String.IsNullOrEmpty(sortOrder) ? "rating_desc" : "";
            ViewData["DateSortParam"] = sortOrder == "Date" ? "date_desc" : "Date";
            ViewData["UserSortParam"] = sortOrder == "User" ? "user_desc" : "User";
            ViewData["DocumentSortParam"] = sortOrder == "Document" ? "document_desc" : "Document";
            ViewData["CurrentFilter"] = searchString;

            var pageSize = 10;
            var ratings = _context.Ratings
                .Include(r => r.User)
                .Include(r => r.Document)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                ratings = ratings.Where(r => 
                    r.User.Username.Contains(searchString) ||
                    r.Document.Title.Contains(searchString));
            }

            ratings = sortOrder switch
            {
                "rating_desc" => ratings.OrderByDescending(r => r.RatingValue),
                "Date" => ratings.OrderBy(r => r.RatingDate),
                "date_desc" => ratings.OrderByDescending(r => r.RatingDate),
                "User" => ratings.OrderBy(r => r.User.Username),
                "user_desc" => ratings.OrderByDescending(r => r.User.Username),
                "Document" => ratings.OrderBy(r => r.Document.Title),
                "document_desc" => ratings.OrderByDescending(r => r.Document.Title),
                _ => ratings.OrderByDescending(r => r.RatingDate),
            };

            var paginatedRatings = await PaginatedList<Rating>.CreateAsync(
                ratings.AsNoTracking(), pageNumber ?? 1, pageSize);

            // Thống kê tổng quan về đánh giá
            ViewBag.TotalRatings = await _context.Ratings.CountAsync();
            ViewBag.AverageRating = await _context.Ratings.AverageAsync(r => r.RatingValue);
            ViewBag.TotalDocumentsRated = await _context.Ratings.Select(r => r.DocumentID).Distinct().CountAsync();
            ViewBag.TotalUsersRated = await _context.Ratings.Select(r => r.UserID).Distinct().CountAsync();

            // Phân phối đánh giá theo số sao
            ViewBag.Rating5Count = await _context.Ratings.Where(r => r.RatingValue == 5).CountAsync();
            ViewBag.Rating4Count = await _context.Ratings.Where(r => r.RatingValue == 4).CountAsync();
            ViewBag.Rating3Count = await _context.Ratings.Where(r => r.RatingValue == 3).CountAsync();
            ViewBag.Rating2Count = await _context.Ratings.Where(r => r.RatingValue == 2).CountAsync();
            ViewBag.Rating1Count = await _context.Ratings.Where(r => r.RatingValue == 1).CountAsync();

            return View(paginatedRatings);
        }

        // GET: Admin/Statistics/Favorites
        public async Task<IActionResult> Favorites(string sortOrder, string searchString, int? pageNumber)
        {
            _logger.LogInformation("Truy cập trang thống kê tài liệu yêu thích");
            
            ViewData["CurrentSort"] = sortOrder;
            ViewData["UserSortParam"] = String.IsNullOrEmpty(sortOrder) ? "user_desc" : "User";
            ViewData["DocumentSortParam"] = sortOrder == "Document" ? "document_desc" : "Document";
            ViewData["CurrentFilter"] = searchString;

            var pageSize = 10;
            var favorites = _context.Favorites
                .Include(f => f.User)
                .Include(f => f.Document)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                favorites = favorites.Where(f => 
                    f.User.Username.Contains(searchString) ||
                    f.Document.Title.Contains(searchString));
            }

            favorites = sortOrder switch
            {
                "User" => favorites.OrderBy(f => f.User.Username),
                "user_desc" => favorites.OrderByDescending(f => f.User.Username),
                "Document" => favorites.OrderBy(f => f.Document.Title),
                "document_desc" => favorites.OrderByDescending(f => f.Document.Title),
                _ => favorites.OrderBy(f => f.Document.Title),
            };

            var paginatedFavorites = await PaginatedList<Favorite>.CreateAsync(
                favorites.AsNoTracking(), pageNumber ?? 1, pageSize);

            // Thống kê tổng quan về tài liệu yêu thích
            ViewBag.TotalFavorites = await _context.Favorites.CountAsync();
            ViewBag.TotalDocumentsFavorited = await _context.Favorites.Select(f => f.DocumentID).Distinct().CountAsync();
            ViewBag.TotalUsersFavorited = await _context.Favorites.Select(f => f.UserID).Distinct().CountAsync();

            // Top 5 tài liệu được yêu thích nhiều nhất
            ViewBag.TopFavoritedDocuments = await _context.Favorites
                .GroupBy(f => f.DocumentID)
                .Select(g => new { DocumentID = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .Join(_context.Documents,
                    f => f.DocumentID,
                    d => d.DocumentID,
                    (f, d) => new { Document = d, Count = f.Count })
                .ToListAsync();

            return View(paginatedFavorites);
        }

        // GET: Admin/Statistics/Downloads
        public async Task<IActionResult> Downloads(string sortOrder, string searchString, int? pageNumber)
        {
            _logger.LogInformation("Truy cập trang thống kê lượt tải xuống");
            
            ViewData["CurrentSort"] = sortOrder;
            ViewData["DateSortParam"] = String.IsNullOrEmpty(sortOrder) ? "date_desc" : "Date";
            ViewData["UserSortParam"] = sortOrder == "User" ? "user_desc" : "User";
            ViewData["DocumentSortParam"] = sortOrder == "Document" ? "document_desc" : "Document";
            ViewData["CurrentFilter"] = searchString;

            var pageSize = 10;
            var downloads = _context.Downloads
                .Include(d => d.User)
                .Include(d => d.Document)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                downloads = downloads.Where(d => 
                    d.User.Username.Contains(searchString) ||
                    d.Document.Title.Contains(searchString));
            }

            downloads = sortOrder switch
            {
                "Date" => downloads.OrderBy(d => d.DownloadDate),
                "date_desc" => downloads.OrderByDescending(d => d.DownloadDate),
                "User" => downloads.OrderBy(d => d.User.Username),
                "user_desc" => downloads.OrderByDescending(d => d.User.Username),
                "Document" => downloads.OrderBy(d => d.Document.Title),
                "document_desc" => downloads.OrderByDescending(d => d.Document.Title),
                _ => downloads.OrderByDescending(d => d.DownloadDate),
            };

            var paginatedDownloads = await PaginatedList<Download>.CreateAsync(
                downloads.AsNoTracking(), pageNumber ?? 1, pageSize);

            // Thống kê tổng quan về lượt tải xuống
            ViewBag.TotalDownloads = await _context.Downloads.CountAsync();
            ViewBag.TotalDocumentsDownloaded = await _context.Downloads.Select(d => d.DocumentID).Distinct().CountAsync();
            ViewBag.TotalUsersDownloaded = await _context.Downloads.Select(d => d.UserID).Distinct().CountAsync();
            ViewBag.DownloadsToday = await _context.Downloads
                .Where(d => d.DownloadDate.Date == DateTime.Today)
                .CountAsync();

            // Top 5 tài liệu được tải nhiều nhất
            ViewBag.TopDownloadedDocuments = await _context.Downloads
                .GroupBy(d => d.DocumentID)
                .Select(g => new { DocumentID = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .Join(_context.Documents,
                    d => d.DocumentID,
                    doc => doc.DocumentID,
                    (d, doc) => new { Document = doc, Count = d.Count })
                .ToListAsync();

            return View(paginatedDownloads);
        }

        // Lấy dữ liệu thống kê biểu đồ
        [HttpGet]
        public async Task<IActionResult> GetDownloadStats()
        {
            // Lấy thống kê tải xuống 7 ngày gần nhất
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-6);

            var downloadStats = await _context.Downloads
                .Where(d => d.DownloadDate.Date >= startDate && d.DownloadDate.Date <= endDate)
                .GroupBy(d => d.DownloadDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var dateLabels = new List<string>();
            var downloadCounts = new List<int>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                dateLabels.Add(date.ToString("dd/MM"));
                var count = downloadStats.FirstOrDefault(x => x.Date == date)?.Count ?? 0;
                downloadCounts.Add(count);
            }

            return Json(new { dateLabels, downloadCounts });
        }
        
        // GET: Admin/Statistics/UserActivity
        public async Task<IActionResult> UserActivity(string sortOrder, string searchString, int? pageNumber)
        {
            _logger.LogInformation("Truy cập trang thống kê hoạt động người dùng");
            
            ViewData["CurrentSort"] = sortOrder;
            ViewData["UsernameSortParam"] = String.IsNullOrEmpty(sortOrder) ? "username_desc" : "Username";
            ViewData["CommentsSortParam"] = sortOrder == "Comments" ? "comments_desc" : "Comments";
            ViewData["RatingsSortParam"] = sortOrder == "Ratings" ? "ratings_desc" : "Ratings";
            ViewData["DownloadsSortParam"] = sortOrder == "Downloads" ? "downloads_desc" : "Downloads";
            ViewData["FavoritesSortParam"] = sortOrder == "Favorites" ? "favorites_desc" : "Favorites";
            ViewData["CurrentFilter"] = searchString;

            var pageSize = 10;
            
            // Lấy thông tin hoạt động của từng người dùng
            var userActivities = _context.Users
                .Where(u => u.Role == "User") // Chỉ lấy người dùng không phải admin
                .Select(u => new UserActivityViewModel
                {
                    UserID = u.UserID,
                    Username = u.Username,
                    Email = u.Email,
                    Status = u.Status,
                    CommentCount = u.Comments.Count,
                    RatingCount = u.Ratings.Count,
                    DownloadCount = u.Downloads.Count,
                    FavoriteCount = u.Favorites.Count,
                    LastCommentDate = u.Comments.OrderByDescending(c => c.CommentDate).FirstOrDefault().CommentDate,
                    LastDownloadDate = u.Downloads.OrderByDescending(d => d.DownloadDate).FirstOrDefault().DownloadDate
                })
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                userActivities = userActivities.Where(u => 
                    u.Username.Contains(searchString) ||
                    u.Email.Contains(searchString));
            }

            userActivities = sortOrder switch
            {
                "Username" => userActivities.OrderBy(u => u.Username),
                "username_desc" => userActivities.OrderByDescending(u => u.Username),
                "Comments" => userActivities.OrderBy(u => u.CommentCount),
                "comments_desc" => userActivities.OrderByDescending(u => u.CommentCount),
                "Ratings" => userActivities.OrderBy(u => u.RatingCount),
                "ratings_desc" => userActivities.OrderByDescending(u => u.RatingCount),
                "Downloads" => userActivities.OrderBy(u => u.DownloadCount),
                "downloads_desc" => userActivities.OrderByDescending(u => u.DownloadCount),
                "Favorites" => userActivities.OrderBy(u => u.FavoriteCount),
                "favorites_desc" => userActivities.OrderByDescending(u => u.FavoriteCount),
                _ => userActivities.OrderByDescending(u => u.CommentCount + u.RatingCount + u.DownloadCount + u.FavoriteCount),
            };

            var paginatedUserActivities = await PaginatedList<UserActivityViewModel>.CreateAsync(
                userActivities.AsNoTracking(), pageNumber ?? 1, pageSize);

            // Thống kê tổng quan
            ViewBag.TotalUsers = await _context.Users.Where(u => u.Role == "User").CountAsync();
            ViewBag.ActiveUsers = await _context.Users.Where(u => u.Role == "User" && u.Status == "Active").CountAsync();
            ViewBag.LockedUsers = await _context.Users.Where(u => u.Role == "User" && u.Status == "Locked").CountAsync();
            
            // Thống kê người dùng hoạt động nhiều nhất
            ViewBag.MostActiveUsers = await userActivities
                .OrderByDescending(u => u.CommentCount + u.RatingCount + u.DownloadCount + u.FavoriteCount)
                .Take(5)
                .ToListAsync();
                
            // Thống kê người dùng có nhiều bình luận nhất
            ViewBag.MostCommentingUsers = await userActivities
                .Where(u => u.CommentCount > 0)
                .OrderByDescending(u => u.CommentCount)
                .Take(5)
                .ToListAsync();
                
            // Thống kê người dùng tải nhiều tài liệu nhất
            ViewBag.MostDownloadingUsers = await userActivities
                .Where(u => u.DownloadCount > 0)
                .OrderByDescending(u => u.DownloadCount)
                .Take(5)
                .ToListAsync();
                
            // Thống kê người dùng mới nhất có hoạt động
            ViewBag.RecentActiveUsers = await _context.Downloads
                .OrderByDescending(d => d.DownloadDate)
                .Select(d => d.User)
                .Distinct()
                .Take(5)
                .ToListAsync();

            return View(paginatedUserActivities);
        }
        
        // GET: Admin/Statistics/GetUserActivityStats
        [HttpGet]
        public async Task<IActionResult> GetUserActivityStats()
        {
            // Lấy thống kê hoạt động người dùng 7 ngày gần nhất
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-6);

            var commentStats = await _context.Comments
                .Where(c => c.CommentDate.Date >= startDate && c.CommentDate.Date <= endDate)
                .GroupBy(c => c.CommentDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToDictionaryAsync(k => k.Date, v => v.Count);
                
            var downloadStats = await _context.Downloads
                .Where(d => d.DownloadDate.Date >= startDate && d.DownloadDate.Date <= endDate)
                .GroupBy(d => d.DownloadDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToDictionaryAsync(k => k.Date, v => v.Count);
                
            var ratingStats = await _context.Ratings
                .Where(r => r.RatingDate.Date >= startDate && r.RatingDate.Date <= endDate)
                .GroupBy(r => r.RatingDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToDictionaryAsync(k => k.Date, v => v.Count);
                
            var favoriteStats = await _context.Favorites
                .Select(f => new { Date = DateTime.Today, Count = 1 }) // Giả lập ngày vì không có ngày tạo
                .GroupBy(f => f.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Date, v => v.Count);

            var dateLabels = new List<string>();
            var commentCounts = new List<int>();
            var downloadCounts = new List<int>();
            var ratingCounts = new List<int>();
            var favoriteCounts = new List<int>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                dateLabels.Add(date.ToString("dd/MM"));
                
                commentCounts.Add(commentStats.ContainsKey(date) ? commentStats[date] : 0);
                downloadCounts.Add(downloadStats.ContainsKey(date) ? downloadStats[date] : 0);
                ratingCounts.Add(ratingStats.ContainsKey(date) ? ratingStats[date] : 0);
                favoriteCounts.Add(favoriteStats.ContainsKey(date) ? favoriteStats[date] : 0);
            }

            return Json(new
            {
                labels = dateLabels,
                commentData = commentCounts,
                downloadData = downloadCounts,
                ratingData = ratingCounts,
                favoriteData = favoriteCounts
            });
        }
        
        // GET: Admin/Statistics/GetRatingStats
        [HttpGet]
        public async Task<IActionResult> GetRatingStats()
        {
            // Lấy số liệu đánh giá theo số sao
            var rating5Count = await _context.Ratings.Where(r => r.RatingValue == 5).CountAsync();
            var rating4Count = await _context.Ratings.Where(r => r.RatingValue == 4).CountAsync();
            var rating3Count = await _context.Ratings.Where(r => r.RatingValue == 3).CountAsync();
            var rating2Count = await _context.Ratings.Where(r => r.RatingValue == 2).CountAsync();
            var rating1Count = await _context.Ratings.Where(r => r.RatingValue == 1).CountAsync();
            
            // Lấy thống kê đánh giá 7 ngày gần nhất
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-6);
            
            var ratingStats = await _context.Ratings
                .Where(r => r.RatingDate.Date >= startDate && r.RatingDate.Date <= endDate)
                .GroupBy(r => r.RatingDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count(), Average = g.Average(r => r.RatingValue) })
                .OrderBy(x => x.Date)
                .ToListAsync();
                
            var dateLabels = new List<string>();
            var ratingCounts = new List<int>();
            var ratingAverages = new List<double>();
            
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                dateLabels.Add(date.ToString("dd/MM"));
                
                var stats = ratingStats.FirstOrDefault(x => x.Date == date);
                ratingCounts.Add(stats?.Count ?? 0);
                ratingAverages.Add(stats?.Average ?? 0);
            }
            
            return Json(new
            {
                // Dữ liệu phân bố đánh giá theo số sao
                ratingLabels = new[] { "5 sao", "4 sao", "3 sao", "2 sao", "1 sao" },
                ratingData = new[] { rating5Count, rating4Count, rating3Count, rating2Count, rating1Count },
                ratingColors = new[] { "#198754", "#0d6efd", "#0dcaf0", "#ffc107", "#dc3545" },
                
                // Dữ liệu đánh giá theo ngày
                dateLabels = dateLabels,
                countData = ratingCounts,
                averageData = ratingAverages
            });
        }
    }
} 