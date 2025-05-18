using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using SenseLib.Services;
using SenseLib.Utilities;
using System.Security.Claims;

namespace SenseLib.Controllers
{
    [Authorize]
    public class WalletController : Controller
    {
        private readonly DataContext _context;
        private readonly WalletService _walletService;
        private readonly VNPayService _vnpayService;

        public WalletController(DataContext context, WalletService walletService, VNPayService vnpayService)
        {
            _context = context;
            _walletService = walletService;
            _vnpayService = vnpayService;
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
        
        // GET: Wallet/Deposit
        public async Task<IActionResult> Deposit()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var wallet = await _walletService.GetWalletAsync(userId);
            
            return View(wallet);
        }
        
        // POST: Wallet/Deposit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(decimal amount)
        {
            if (amount < 10000)
            {
                ModelState.AddModelError("", "Số tiền nạp tối thiểu là 10,000 VND");
                
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var wallet = await _walletService.GetWalletAsync(userId);
                return View(wallet);
            }
            
            // Tạo yêu cầu nạp tiền qua VNPay
            try 
            {
                var orderInfo = $"Nạp POINT vào ví SenseLib";
                var orderDescription = $"Nạp tiền {amount:N0} VND thành {amount:N0} POINT vào ví";
                var orderType = "billpayment";
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                // Đường dẫn callback khi thanh toán xong
                string host = $"{Request.Scheme}://{Request.Host}";
                string returnUrl = $"{host}/Wallet/DepositCallback";
                
                // Tạo URL thanh toán
                var paymentRequest = _vnpayService.CreatePaymentUrl(orderType, amount, orderDescription, orderInfo, ipAddress, returnUrl);
                
                // Lưu thông tin nạp tiền vào TempData thay vì Session
                TempData["DepositOrderCode"] = paymentRequest.TxnRef;
                TempData["DepositAmount"] = amount.ToString();
                
                // Chuyển hướng người dùng đến trang thanh toán VNPay
                return Redirect(paymentRequest.PaymentUrl);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var wallet = await _walletService.GetWalletAsync(userId);
                return View(wallet);
            }
        }
        
        // GET: Wallet/DepositCallback - Xử lý kết quả nạp tiền từ VNPay
        public async Task<IActionResult> DepositCallback()
        {
            // Lấy thông tin từ callback của VNPay
            var vnpayData = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
            
            // Kiểm tra thông tin
            if (!_vnpayService.ValidatePayment(vnpayData))
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xác thực thanh toán";
                return RedirectToAction(nameof(Index));
            }
            
            // Lấy kết quả thanh toán
            var paymentResult = _vnpayService.GetPaymentResponse(vnpayData);
            
            if (paymentResult.Success)
            {
                // Lấy thông tin người dùng hiện tại
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var wallet = await _walletService.GetWalletAsync(userId);
                
                // Nạp tiền vào ví
                var result = await _walletService.DepositAsync(
                    walletId: wallet.WalletID,
                    amount: paymentResult.Amount,
                    transactionCode: paymentResult.TransactionId,
                    description: $"Nạp tiền vào ví - Mã GD: {paymentResult.TransactionId}"
                );
                
                if (result)
                {
                    TempData["SuccessMessage"] = $"Nạp thành công {paymentResult.Amount:N0} POINT vào ví";
                }
                else
                {
                    TempData["ErrorMessage"] = "Thanh toán thành công nhưng có lỗi khi cập nhật số dư ví";
                }
            }
            else
            {
                TempData["ErrorMessage"] = $"Nạp tiền không thành công: {paymentResult.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // GET: Wallet/PayForDocument/5
        public async Task<IActionResult> PayForDocument(int id)
        {
            // Lấy thông tin tài liệu
            var document = await _context.Documents
                .Include(d => d.Author)
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (document == null)
            {
                return NotFound();
            }
            
            // Kiểm tra nếu đã mua
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var alreadyPurchased = await _context.Purchases
                .AnyAsync(p => p.UserID == userId && p.DocumentID == id && p.Status == "Completed");

            if (alreadyPurchased)
            {
                TempData["InfoMessage"] = "Bạn đã mua tài liệu này trước đó";
                return RedirectToAction("Details", "Document", new { id });
            }
            
            // Lấy thông tin ví
            var wallet = await _walletService.GetWalletAsync(userId);
            ViewBag.Wallet = wallet;
            
            // Kiểm tra số dư
            if (document.Price.HasValue && wallet.Balance < document.Price.Value)
            {
                ViewBag.NotEnoughBalance = true;
            }
            
            return View(document);
        }
        
        // POST: Wallet/ConfirmPayment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Thực hiện thanh toán từ ví
            var result = await _walletService.PayForDocumentFromWalletAsync(userId, id);
            
            if (result.success)
            {
                TempData["SuccessMessage"] = "Thanh toán tài liệu thành công!";
                return RedirectToAction("Details", "Document", new { id });
            }
            else
            {
                TempData["ErrorMessage"] = result.message;
                return RedirectToAction("PayForDocument", new { id });
            }
        }
    }
} 