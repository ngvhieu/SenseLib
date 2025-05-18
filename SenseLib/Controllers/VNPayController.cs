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
using Microsoft.Extensions.Logging;

namespace SenseLib.Controllers
{
    [Authorize]
    public class VNPayController : Controller
    {
        private readonly DataContext _context;
        private readonly VNPayService _vnpayService;
        private readonly WalletService _walletService;
        private readonly ILogger<VNPayController> _logger;
        private readonly UserActivityService _userActivityService;

        public VNPayController(DataContext context, VNPayService vnpayService, WalletService walletService, ILogger<VNPayController> logger, UserActivityService userActivityService)
        {
            _context = context;
            _vnpayService = vnpayService;
            _walletService = walletService;
            _logger = logger;
            _userActivityService = userActivityService;
        }

        // GET: VNPay/PaymentRequest/5
        public async Task<IActionResult> PaymentRequest(int id)
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
                // Thêm lại thông báo TempData
                TempData["InfoMessage"] = "Bạn đã mua tài liệu này trước đó.";
                return RedirectToAction("Details", "Document", new { id = id });
            }

            // Hiển thị trang xác nhận thanh toán
            return View(document);
        }

        // POST: VNPay/CreatePayment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePayment(int id)
        {
            try
            {
                var document = await _context.Documents
                    .Include(d => d.Author)
                    .Include(d => d.Category)
                    .FirstOrDefaultAsync(d => d.DocumentID == id);
                    
                if (document == null || !document.Price.HasValue || document.Price <= 0)
                {
                    return NotFound("Không tìm thấy tài liệu hoặc tài liệu không có giá");
                }
    
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var user = await _context.Users.FindAsync(userId);
    
                if (user == null)
                {
                    return Unauthorized("Người dùng không hợp lệ");
                }
    
                // Kiểm tra nếu đã mua
                var existingPurchase = await _context.Purchases
                    .AnyAsync(p => p.UserID == userId && p.DocumentID == id && p.Status == "Completed");
    
                if (existingPurchase)
                {
                    // Thêm lại thông báo TempData
                    TempData["InfoMessage"] = "Bạn đã mua tài liệu này trước đó.";
                    return RedirectToAction("Details", "Document", new { id = id });
                }
    
                // Tạo thông tin thanh toán - chỉ dùng ký tự ASCII đơn giản
                string orderInfo = $"Thanh toan tai lieu ID:{id}";
                string orderDescription = $"Mua tai lieu {document.Title}";
                string orderType = "billpayment";
                
                // Đảm bảo URL callback đầy đủ
                string host = $"{Request.Scheme}://{Request.Host}";
                string returnUrl = $"{host}/VNPay/PaymentCallback";
                
                // Lấy địa chỉ IP
                string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                if (ipAddress?.Contains(":") == true) // IPv6
                {
                    ipAddress = "127.0.0.1"; // Sử dụng IPv4 mặc định
                }
    
                // Tạo URL thanh toán với dữ liệu đã được làm sạch
                var paymentRequest = _vnpayService.CreatePaymentUrl(
                    orderType, 
                    document.Price.Value, 
                    orderDescription, 
                    orderInfo, 
                    ipAddress, 
                    returnUrl
                );
                
                // Lấy mã giao dịch từ VNPay
                string orderCode = paymentRequest.TxnRef;
    
                // Tạo giao dịch Pending trong DB
                var purchase = new Purchase
                {
                    UserID = userId,
                    DocumentID = id,
                    PurchaseDate = DateTime.Now,
                    Amount = document.Price.Value,
                    TransactionCode = orderCode,
                    Status = "Pending" // Trạng thái đang chờ thanh toán
                };
    
                _context.Add(purchase);
                await _context.SaveChangesAsync();
    
                // Ghi log URL để debug
                Console.WriteLine($"URL thanh toán: {paymentRequest.PaymentUrl}");
                Console.WriteLine($"Mã giao dịch: {orderCode}");
    
                // Chuyển hướng người dùng đến trang thanh toán VNPay
                return Redirect(paymentRequest.PaymentUrl);
            }
            catch (Exception ex)
            {
                // Log lỗi và thông báo cho người dùng
                Console.WriteLine($"Lỗi khi tạo yêu cầu thanh toán: {ex.Message}");
                // Thêm lại thông báo TempData
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tạo yêu cầu thanh toán. Vui lòng thử lại sau.";
                return RedirectToAction("Details", "Document", new { id = id });
            }
        }

        // GET: VNPay/PaymentCallback
        public async Task<IActionResult> PaymentCallback()
        {
            var response = _vnpayService.ProcessPaymentCallback(Request.Query);
            
            _logger.LogInformation("VNPay callback received. OrderId: {OrderId}, ResponseCode: {ResponseCode}, Message: {Message}", 
                response.OrderId, response.ResponseCode, response.Message);
            
            // Lấy thông tin từ URL
            var orderId = response.OrderId;
            var documentId = 0;
            var purchaseType = "";
            
            try
            {
                // Parse thông tin từ order ID (format: DOCUMENT-123 hoặc WALLET-456)
                if (!string.IsNullOrEmpty(orderId))
                {
                    var parts = orderId.Split('-');
                    if (parts.Length >= 2)
                    {
                        purchaseType = parts[0];
                        
                        if (purchaseType == "DOCUMENT" && int.TryParse(parts[1], out int docId))
                        {
                            documentId = docId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing order ID: {OrderId}", orderId);
            }
            
            // Kiểm tra trạng thái thanh toán
            if (response.IsSuccess)
            {
                _logger.LogInformation("Payment successful. PurchaseType: {PurchaseType}, DocumentId: {DocumentId}", 
                    purchaseType, documentId);
                
                // Lấy thông tin người dùng hiện tại
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                if (purchaseType == "DOCUMENT" && documentId > 0)
                {
                    // Xử lý mua tài liệu
                    var document = await _context.Documents.FindAsync(documentId);
                    
                    if (document != null)
                    {
                        // Kiểm tra xem đã mua chưa
                        var existingPurchase = await _context.Purchases
                            .AnyAsync(p => p.UserID == userId && p.DocumentID == documentId && p.Status == "Completed");
                            
                        if (!existingPurchase)
                        {
                            decimal amount = document.Price ?? 0;
                            
                            // Lưu thông tin mua hàng
                            var purchase = new Purchase
                            {
                                UserID = userId,
                                DocumentID = documentId,
                                PurchaseDate = DateTime.Now,
                                Amount = amount,
                                TransactionCode = response.TransactionId,
                                Status = "Completed"
                            };
                            
                            _context.Purchases.Add(purchase);
                            await _context.SaveChangesAsync();
                            
                            // Ghi lại hoạt động mua tài liệu
                            await _userActivityService.LogPurchaseActivityAsync(userId, documentId, amount);
                            
                            // Xử lý chuyển tiền cho tác giả tài liệu
                            await _walletService.ProcessPurchasePaymentAsync(purchase);
                            
                            TempData["SuccessMessage"] = "Thanh toán thành công! Bạn đã mua tài liệu này.";
                            return RedirectToAction("Details", "Document", new { id = documentId });
                        }
                        else
                        {
                            TempData["InfoMessage"] = "Bạn đã mua tài liệu này trước đó.";
                            return RedirectToAction("Details", "Document", new { id = documentId });
                        }
                    }
                }
            }
            else
            {
                // Thêm lại thông báo TempData
                TempData["ErrorMessage"] = response.Message;
            }
            
            return RedirectToAction("Index", "Home");
        }
        
        // GET: VNPay/PaymentHistory
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