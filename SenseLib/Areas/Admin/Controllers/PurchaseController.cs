using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Collections.Generic;

namespace SenseLib.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PurchaseController : Controller
    {
        private readonly DataContext _context;

        public PurchaseController(DataContext context)
        {
            _context = context;
        }

        // GET: Admin/Purchase
        public async Task<IActionResult> Index(string keyword = null, string status = null, DateTime? fromDate = null, 
            DateTime? toDate = null, int? userId = null, int page = 1)
        {
            // Mặc định hiển thị 15 giao dịch mỗi trang
            int pageSize = 15;
            
            // Truy vấn tất cả giao dịch
            var query = _context.Purchases
                .Include(p => p.Document)
                .ThenInclude(d => d.Author)
                .Include(p => p.User)
                .AsQueryable();
            
            // Lọc theo từ khóa (tìm kiếm theo tên tài liệu, mã giao dịch, hoặc tên người dùng)
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p => 
                    p.Document.Title.Contains(keyword) || 
                    p.TransactionCode.Contains(keyword) || 
                    p.User.Username.Contains(keyword) ||
                    p.User.Email.Contains(keyword));
                
                ViewBag.Keyword = keyword;
            }
            
            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
                ViewBag.CurrentStatus = status;
            }
            
            // Lọc theo khoảng thời gian
            if (fromDate.HasValue)
            {
                query = query.Where(p => p.PurchaseDate >= fromDate.Value);
                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            }
            
            if (toDate.HasValue)
            {
                // Bao gồm cả ngày kết thúc
                var endDate = toDate.Value.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.PurchaseDate <= endDate);
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            }
            
            // Lọc theo người dùng cụ thể
            if (userId.HasValue && userId > 0)
            {
                query = query.Where(p => p.UserID == userId);
                ViewBag.SelectedUserId = userId;
            }
            
            // Sắp xếp theo ngày mua giảm dần (mới nhất trước)
            query = query.OrderByDescending(p => p.PurchaseDate);
            
            // Tính tổng số giao dịch và số trang
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            // Điều chỉnh trang hiện tại nếu cần
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            
            // Lấy dữ liệu cho trang hiện tại
            var purchases = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            // Tính tổng doanh thu từ các giao dịch đã hoàn thành
            var totalRevenue = await _context.Purchases
                .Where(p => p.Status == "Completed")
                .SumAsync(p => p.Amount);
            
            // Danh sách người dùng để hiển thị trong dropdown lọc
            var users = await _context.Users
                .Select(u => new { u.UserID, u.Username })
                .ToListAsync();
            
            // Truyền dữ liệu cho view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.Statuses = new List<string> { "Completed", "Pending", "Failed" };
            ViewBag.Users = users;
            ViewBag.CompletedCount = await _context.Purchases.CountAsync(p => p.Status == "Completed");
            ViewBag.PendingCount = await _context.Purchases.CountAsync(p => p.Status == "Pending");
            ViewBag.FailedCount = await _context.Purchases.CountAsync(p => p.Status == "Failed");
            
            return View(purchases);
        }

        // GET: Admin/Purchase/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var purchase = await _context.Purchases
                .Include(p => p.Document)
                .ThenInclude(d => d.Author)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.PurchaseID == id);

            if (purchase == null)
            {
                return NotFound();
            }

            return View(purchase);
        }

        // POST: Admin/Purchase/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var purchase = await _context.Purchases.FindAsync(id);
            
            if (purchase == null)
            {
                return NotFound();
            }
            
            // Cập nhật trạng thái
            purchase.Status = status;
            _context.Update(purchase);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái giao dịch #{id} thành {status}";
            
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Admin/Purchase/UserPurchases/5
        public async Task<IActionResult> UserPurchases(int id, int page = 1)
        {
            // Kiểm tra người dùng tồn tại
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            
            // Số lượng mục trên mỗi trang
            int pageSize = 10;
            
            // Lấy tất cả giao dịch của người dùng này
            var query = _context.Purchases
                .Include(p => p.Document)
                .ThenInclude(d => d.Author)
                .Where(p => p.UserID == id)
                .OrderByDescending(p => p.PurchaseDate);
            
            // Tính tổng số trang
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            // Điều chỉnh trang hiện tại
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            
            // Lấy dữ liệu cho trang hiện tại
            var purchases = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            // Tính tổng tiền đã chi tiêu của người dùng
            var totalSpent = await _context.Purchases
                .Where(p => p.UserID == id && p.Status == "Completed")
                .SumAsync(p => p.Amount);
            
            // Truyền dữ liệu cho view
            ViewBag.User = user;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalSpent = totalSpent;
            
            return View(purchases);
        }

        // GET: Admin/Purchase/Reports
        public async Task<IActionResult> Reports(int days = 30)
        {
            // Lấy ngày bắt đầu dựa trên số ngày được chọn
            var startDate = DateTime.Now.AddDays(-days);
            
            // Doanh thu theo ngày trong khoảng thời gian đã chọn
            var dailyRevenue = await _context.Purchases
                .Where(p => p.Status == "Completed" && p.PurchaseDate >= startDate)
                .GroupBy(p => p.PurchaseDate.Date)
                .Select(g => new { 
                    Date = g.Key, 
                    Revenue = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .OrderBy(g => g.Date)
                .ToListAsync();
            
            // Chuyển đổi kết quả thành dạng phù hợp cho biểu đồ
            var labels = dailyRevenue.Select(d => d.Date.ToString("dd/MM/yyyy")).ToList();
            var revenueData = dailyRevenue.Select(d => d.Revenue).ToList();
            var countData = dailyRevenue.Select(d => d.Count).ToList();
            
            // Doanh thu theo danh mục
            var categoryRevenue = await _context.Purchases
                .Where(p => p.Status == "Completed")
                .Include(p => p.Document)
                .ThenInclude(d => d.Category)
                .GroupBy(p => p.Document.Category.CategoryName)
                .Select(g => new { 
                    Category = g.Key, 
                    Revenue = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Revenue)
                .Take(10)
                .ToListAsync();
            
            // Truyền dữ liệu cho view
            ViewBag.DaysSelected = days;
            ViewBag.Labels = labels;
            ViewBag.RevenueData = revenueData;
            ViewBag.CountData = countData;
            ViewBag.CategoryRevenue = categoryRevenue;
            
            // Tổng doanh thu
            ViewBag.TotalRevenue = await _context.Purchases
                .Where(p => p.Status == "Completed")
                .SumAsync(p => p.Amount);
            
            // Tổng số giao dịch
            ViewBag.TotalTransactions = await _context.Purchases.CountAsync();
            ViewBag.CompletedTransactions = await _context.Purchases.CountAsync(p => p.Status == "Completed");
            
            // Top 5 tài liệu bán chạy nhất
            ViewBag.TopSellingDocuments = await _context.Purchases
                .Where(p => p.Status == "Completed")
                .Include(p => p.Document)
                .ThenInclude(d => d.Author)
                .GroupBy(p => new { p.DocumentID, p.Document.Title, p.Document.Author.AuthorName })
                .Select(g => new {
                    DocumentID = g.Key.DocumentID,
                    Title = g.Key.Title,
                    Author = g.Key.AuthorName,
                    Count = g.Count(),
                    Revenue = g.Sum(p => p.Amount)
                })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToListAsync();
            
            return View();
        }
    }
} 