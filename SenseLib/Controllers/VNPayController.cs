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
    public class VNPayController : Controller
    {
        private readonly DataContext _context;
        private readonly VNPayService _vnpayService;

        public VNPayController(DataContext context, VNPayService vnpayService)
        {
            _context = context;
            _vnpayService = vnpayService;
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
            try
            {
                // Lấy thông tin từ callback của VNPay
                var vnpayData = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
                
                // Ghi log dữ liệu trả về từ VNPay
                Console.WriteLine("Dữ liệu trả về từ VNPay:");
                foreach (var item in vnpayData)
                {
                    Console.WriteLine($"{item.Key}: {item.Value}");
                }
                
                // Kiểm tra dữ liệu cần thiết
                if (!vnpayData.ContainsKey("vnp_ResponseCode") || 
                    !vnpayData.ContainsKey("vnp_TxnRef") || 
                    !vnpayData.ContainsKey("vnp_SecureHash"))
                {
                    // Thêm lại thông báo TempData
                    TempData["ErrorMessage"] = "Dữ liệu trả về từ VNPay không đầy đủ";
                    return RedirectToAction("Index", "Home");
                }
                
                // Lấy mã giao dịch từ VNPay
                string orderCode = vnpayData["vnp_TxnRef"];
                Console.WriteLine($"Đang tìm giao dịch với mã: {orderCode}");
                
                // Tìm giao dịch trong DB
                var purchase = await _context.Purchases
                    .Include(p => p.Document)
                    .FirstOrDefaultAsync(p => p.TransactionCode == orderCode);
    
                if (purchase == null)
                {
                    Console.WriteLine("Không tìm thấy giao dịch với mã trên. Kiểm tra tất cả các giao dịch để tìm gần đúng...");
                    
                    // Tải tất cả giao dịch Pending
                    var pendingPurchases = await _context.Purchases
                        .Include(p => p.Document)
                        .Where(p => p.Status == "Pending")
                        .OrderByDescending(p => p.PurchaseDate)
                        .Take(10)
                        .ToListAsync();
                    
                    Console.WriteLine($"Tìm thấy {pendingPurchases.Count} giao dịch Pending gần đây:");
                    foreach (var p in pendingPurchases)
                    {
                        Console.WriteLine($"ID: {p.PurchaseID}, Mã GD: {p.TransactionCode}, Document: {p.DocumentID}, Ngày: {p.PurchaseDate}");
                    }
                    
                    // Trường hợp VNPay đã thay đổi mã giao dịch, tìm giao dịch gần nhất
                    purchase = pendingPurchases.FirstOrDefault();
                    
                    if (purchase != null)
                    {
                        Console.WriteLine($"Sử dụng giao dịch gần nhất thay thế: {purchase.PurchaseID}");
                        // Cập nhật mã giao dịch thực tế từ VNPay
                        purchase.TransactionCode = orderCode;
                    }
                    else
                    {
                        // Thêm lại thông báo TempData
                        TempData["ErrorMessage"] = "Không tìm thấy thông tin giao dịch trong hệ thống";
                        return RedirectToAction("Index", "Home");
                    }
                }
                
                // Xác thực chữ ký từ VNPay
                bool isValidSignature = _vnpayService.ValidatePayment(vnpayData);
                
                // Lấy kết quả thanh toán
                var responseCode = vnpayData["vnp_ResponseCode"];
                
                // Lưu thông tin giao dịch
                purchase.Status = responseCode == "00" ? "Completed" : "Failed";
                
                // Nếu có số giao dịch từ VNPay, lưu lại
                if (vnpayData.ContainsKey("vnp_TransactionNo"))
                {
                    purchase.TransactionCode = $"{purchase.TransactionCode}|{vnpayData["vnp_TransactionNo"]}";
                }
                
                await _context.SaveChangesAsync();
                
                // Nếu chữ ký không hợp lệ, cảnh báo
                if (!isValidSignature)
                {
                    // Không hiển thị thông báo nữa
                    Console.WriteLine("Cảnh báo: Chữ ký xác thực từ VNPay không hợp lệ");
                    // Thêm lại thông báo TempData
                    TempData["WarningMessage"] = "Cảnh báo: Chữ ký xác thực từ VNPay không hợp lệ";
                }
    
                // Xử lý kết quả thanh toán - không hiển thị thông báo nữa
                if (responseCode == "00")
                {
                    Console.WriteLine($"Thanh toán thành công cho tài liệu: {purchase.Document.Title}");
                    // Thêm lại thông báo TempData
                    TempData["SuccessMessage"] = $"Thanh toán thành công cho tài liệu: {purchase.Document.Title}";
                }
                else
                {
                    string errorMessage = "Thanh toán thất bại: ";
                    switch (responseCode)
                    {
                        case "01": errorMessage += "Giao dịch đã tồn tại"; break;
                        case "02": errorMessage += "Merchant không hợp lệ"; break;
                        case "03": errorMessage += "Dữ liệu gửi sang không đúng định dạng"; break;
                        case "04": errorMessage += "Khởi tạo GD không thành công"; break;
                        case "07": errorMessage += "Giao dịch bị nghi ngờ"; break;
                        case "09": errorMessage += "Thẻ/Tài khoản chưa đăng ký dịch vụ"; break;
                        case "10": errorMessage += "Xác thực thông tin thẻ sai quá 3 lần"; break;
                        case "11": errorMessage += "Đã hết hạn chờ thanh toán"; break;
                        case "12": errorMessage += "Thẻ/Tài khoản bị khóa"; break;
                        case "24": errorMessage += "Giao dịch bị hủy"; break;
                        case "51": errorMessage += "Tài khoản không đủ số dư"; break;
                        case "65": errorMessage += "Tài khoản vượt quá hạn mức giao dịch"; break;
                        case "75": errorMessage += "Ngân hàng đang bảo trì"; break;
                        case "79": errorMessage += "Nhập sai mật khẩu quá số lần quy định"; break;
                        default: errorMessage += "Lỗi không xác định"; break;
                    }
                    Console.WriteLine(errorMessage);
                    // Thêm lại thông báo TempData
                    TempData["ErrorMessage"] = errorMessage;
                }
                
                return RedirectToAction("Details", "Document", new { id = purchase.DocumentID });
            }
            catch (Exception ex)
            {
                // Log lỗi
                Console.WriteLine($"Lỗi khi xử lý callback VNPay: {ex.Message}");
                // Thêm lại thông báo TempData
                TempData["ErrorMessage"] = "Đã xảy ra lỗi trong quá trình xử lý thanh toán";
                return RedirectToAction("Index", "Home");
            }
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