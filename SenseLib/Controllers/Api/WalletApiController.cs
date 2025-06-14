using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SenseLib.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using SenseLib.Models;
using System.Linq;

namespace SenseLib.Controllers.Api
{
    [ApiController]
    [Authorize]
    public class WalletApiController : ControllerBase
    {
        private readonly WalletService _walletService;

        public WalletApiController(WalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet("api/wallet/balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Người dùng không hợp lệ." });
            }

            try
            {
                var wallet = await _walletService.GetWalletAsync(userId);
                return Ok(new { balance = wallet.Balance });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpGet("api/wallet/transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "Người dùng không hợp lệ." });
            }

            try
            {
                // Lấy thông tin ví
                var wallet = await _walletService.GetWalletAsync(userId);
                
                // Lấy danh sách giao dịch
                var transactions = await _walletService.GetTransactionsAsync(wallet.WalletID, page, pageSize);
                
                // Đếm tổng số giao dịch
                var totalTransactions = await _walletService.CountTransactionsAsync(wallet.WalletID);
                
                // Tính tổng số trang
                var totalPages = (int)Math.Ceiling((double)totalTransactions / pageSize);
                
                // Tạo kết quả phân trang
                var result = new ApiResponse<WalletTransaction>
                {
                    Items = transactions,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = totalTransactions,
                    TotalPages = totalPages
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpPost("api/wallet/deposit-direct")]
        public async Task<IActionResult> DirectDeposit([FromBody] DepositRequestPayload payload)
        {
            if (payload == null || payload.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Số tiền nạp phải lớn hơn 0." });
            }

            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized(new { success = false, message = "Người dùng không hợp lệ." });
                }

                var wallet = await _walletService.GetWalletAsync(userId);
                if (wallet == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy ví của người dùng." });
                }

                var transactionCode = payload.TransactionId ?? $"DEV_DEPOSIT_{DateTime.UtcNow.Ticks}";
                var description = $"Nạp tiền trực tiếp (dev) vào ví: {payload.Amount:N0}";

                var result = await _walletService.DepositAsync(wallet.WalletID, payload.Amount, transactionCode, description);

                if (result)
                {
                    var updatedWallet = await _walletService.GetWalletAsync(userId);
                    return Ok(new { 
                        success = true, 
                        message = "Nạp tiền trực tiếp thành công!", 
                        transactionId = transactionCode,
                        amount = payload.Amount 
                    });
                }
                else
                {
                    return StatusCode(500, new { success = false, message = "Không thể xử lý giao dịch nạp tiền trực tiếp." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
    }

    public class DepositRequestPayload
    {
        public decimal Amount { get; set; }
        public string TransactionId { get; set; }
    }
} 