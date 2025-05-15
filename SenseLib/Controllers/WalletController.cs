using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using SenseLib.Services;
using System.Security.Claims;

namespace SenseLib.Controllers
{
    [Authorize]
    public class WalletController : Controller
    {
        private readonly DataContext _context;
        private readonly WalletService _walletService;

        public WalletController(DataContext context, WalletService walletService)
        {
            _context = context;
            _walletService = walletService;
        }

        // GET: Wallet
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Lấy thông tin ví (sẽ tự động tạo nếu chưa có)
            var wallet = await _walletService.GetWalletAsync(userId);
            
            // Lấy tổng tiền đã kiếm được
            var totalEarned = await _context.WalletTransactions
                .Where(t => t.WalletID == wallet.WalletID && t.Type == "Credit")
                .SumAsync(t => t.Amount);
            
            // Lấy tổng tiền đã rút
            var totalWithdrawn = await _context.WalletTransactions
                .Where(t => t.WalletID == wallet.WalletID && t.Type == "Debit")
                .SumAsync(t => t.Amount);
            
            // Lấy 5 giao dịch gần nhất
            var recentTransactions = await _context.WalletTransactions
                .Where(t => t.WalletID == wallet.WalletID)
                .OrderByDescending(t => t.TransactionDate)
                .Take(5)
                .ToListAsync();
            
            // Lấy số lượng tài liệu đã bán
            var documentsSold = await _context.WalletTransactions
                .Where(t => t.WalletID == wallet.WalletID && t.Type == "Credit" && t.DocumentID != null)
                .Select(t => t.DocumentID)
                .Distinct()
                .CountAsync();
            
            ViewBag.Wallet = wallet;
            ViewBag.TotalEarned = totalEarned;
            ViewBag.TotalWithdrawn = totalWithdrawn;
            ViewBag.RecentTransactions = recentTransactions;
            ViewBag.DocumentsSold = documentsSold;
            
            return View();
        }

        // GET: Wallet/Transactions
        public async Task<IActionResult> Transactions(DateTime? fromDate = null, DateTime? toDate = null, string type = null, int page = 1)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var wallet = await _walletService.GetWalletAsync(userId);
            
            // Mặc định hiển thị 10 giao dịch mỗi trang
            int pageSize = 10;
            
            // Truy vấn lịch sử giao dịch
            var query = _context.WalletTransactions
                .Where(t => t.WalletID == wallet.WalletID)
                .AsQueryable();
            
            // Lọc theo loại nếu có
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(t => t.Type == type);
                ViewBag.CurrentType = type;
            }
            
            // Lọc theo khoảng thời gian nếu có
            if (fromDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate >= fromDate.Value);
                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            }
            
            if (toDate.HasValue)
            {
                // Bao gồm cả ngày kết thúc
                var endDate = toDate.Value.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.TransactionDate <= endDate);
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            }
            
            // Sắp xếp theo ngày giảm dần (mới nhất trước)
            query = query.OrderByDescending(t => t.TransactionDate);
            
            // Tính tổng số giao dịch và số trang
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            // Điều chỉnh trang hiện tại nếu cần
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;
            
            // Lấy dữ liệu cho trang hiện tại
            var transactions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            // Load thông tin tài liệu liên quan đến giao dịch (nếu có)
            foreach (var transaction in transactions.Where(t => t.DocumentID.HasValue))
            {
                await _context.Entry(transaction)
                    .Reference(t => t.Document)
                    .LoadAsync();
            }
            
            // Truyền dữ liệu phân trang và tổng số tiền cho view
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.Wallet = wallet;
            
            // Danh sách các loại giao dịch để hiển thị bộ lọc
            ViewBag.Types = new List<string> { "Credit", "Debit" };
            
            return View(transactions);
        }

        // GET: Wallet/Withdraw
        public async Task<IActionResult> Withdraw()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var wallet = await _walletService.GetWalletAsync(userId);
            
            return View(wallet);
        }

        // POST: Wallet/Withdraw
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(decimal amount, string description)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var wallet = await _walletService.GetWalletAsync(userId);
            
            if (amount <= 0)
            {
                ModelState.AddModelError("", "Số tiền rút phải lớn hơn 0");
                return View(wallet);
            }
            
            if (amount > wallet.Balance)
            {
                ModelState.AddModelError("", "Số dư không đủ để rút tiền");
                return View(wallet);
            }
            
            // Ghi chú về phương thức rút tiền
            string withdrawNote = !string.IsNullOrEmpty(description) 
                ? description 
                : "Yêu cầu rút tiền";
            
            // Thực hiện rút tiền
            var result = await _walletService.WithdrawAsync(wallet.WalletID, amount, withdrawNote);
            
            if (result)
            {
                TempData["SuccessMessage"] = "Yêu cầu rút tiền đã được ghi nhận. Quản trị viên sẽ liên hệ để xác nhận.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi xử lý yêu cầu rút tiền");
                return View(wallet);
            }
        }
    }
} 