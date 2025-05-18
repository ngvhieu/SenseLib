using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Security.Claims;
using System.Collections.Generic;
using SenseLib.Services;

namespace SenseLib.Controllers
{
    [Authorize]
    public class PurchaseController : Controller
    {
        private readonly DataContext _context;
        private readonly UserActivityService _userActivityService;

        public PurchaseController(DataContext context, UserActivityService userActivityService)
        {
            _context = context;
            _userActivityService = userActivityService;
        }

        // GET: Purchase/CheckoutDocument/5
        public async Task<IActionResult> CheckoutDocument(int id)
        {
            var document = await _context.Documents
                .Include(d => d.Author)
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (document == null)
            {
                return NotFound();
            }

            // Kiểm tra xem người dùng đã mua tài liệu này chưa
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var alreadyPurchased = await _context.Purchases
                .AnyAsync(p => p.UserID == userId && p.DocumentID == id && p.Status == "Completed");

            if (alreadyPurchased)
            {
                TempData["Message"] = "Bạn đã mua tài liệu này trước đó.";
                return RedirectToAction("Details", "Document", new { id = id });
            }

            // Hiển thị trang thanh toán
            return View(document);
        }

        // POST: Purchase/ConfirmPurchase/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPurchase(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return Unauthorized();
            }

            // Kiểm tra nếu đã mua
            var existingPurchase = await _context.Purchases
                .AnyAsync(p => p.UserID == userId && p.DocumentID == id && p.Status == "Completed");

            if (existingPurchase)
            {
                TempData["Message"] = "Bạn đã mua tài liệu này trước đó.";
                return RedirectToAction("Details", "Document", new { id = id });
            }

            // Tạo một giao dịch mua mới
            var purchase = new Purchase
            {
                UserID = userId,
                DocumentID = id,
                PurchaseDate = DateTime.Now,
                Amount = document.Price ?? 0,
                TransactionCode = Guid.NewGuid().ToString("N"),
                Status = "Completed" // Đơn giản hóa trong demo, thực tế sẽ có quy trình thanh toán
            };

            _context.Add(purchase);
            await _context.SaveChangesAsync();
            
            // Ghi lại hoạt động mua tài liệu
            await _userActivityService.LogPurchaseActivityAsync(userId, id, purchase.Amount);

            TempData["SuccessMessage"] = "Mua tài liệu thành công!";
            return RedirectToAction("Details", "Document", new { id = id });
        }

        // GET: Purchase/MyPurchases
        public async Task<IActionResult> MyPurchases()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var purchases = await _context.Purchases
                .Include(p => p.Document)
                .ThenInclude(d => d.Author)
                .Include(p => p.Document.Category)
                .Where(p => p.UserID == userId && p.Status == "Completed")
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            return View(purchases);
        }
        
        // GET: Purchase/PaymentHistory
        public async Task<IActionResult> PaymentHistory(string status = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Mặc định hiển thị 10 giao dịch mỗi trang
            int pageSize = 10;
            
            // Truy vấn lịch sử thanh toán
            var query = _context.Purchases
                .Include(p => p.Document)
                .ThenInclude(d => d.Author)
                .Include(p => p.Document.Category)
                .Where(p => p.UserID == userId)
                .AsQueryable();
            
            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
                ViewBag.CurrentStatus = status;
            }
            
            // Lọc theo khoảng thời gian nếu có
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
            
            // Tính tổng số tiền đã thanh toán (chỉ tính các giao dịch đã hoàn thành)
            var totalSpent = await _context.Purchases
                .Where(p => p.UserID == userId && p.Status == "Completed")
                .SumAsync(p => p.Amount);
            
            // Truyền dữ liệu phân trang và tổng số tiền cho view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalSpent = totalSpent;
            
            // Danh sách các trạng thái để hiển thị bộ lọc
            ViewBag.Statuses = new List<string> { "Completed", "Pending", "Failed" };
            
            return View(purchases);
        }
    }
} 