using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SenseLib.Controllers.Api
{
    [Route("api/purchases")]
    [ApiController]
    [Authorize]
    public class PurchasesApiController : ControllerBase
    {
        private readonly DataContext _context;

        public PurchasesApiController(DataContext context)
        {
            _context = context;
        }

        // GET: api/purchases/document-ids
        [HttpGet("document-ids")]
        public async Task<ActionResult<List<int>>> GetPurchasedDocumentIds()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Người dùng không hợp lệ." });
            }

            try
            {
                var purchasedDocumentIds = await _context.Purchases
                    .Where(p => p.UserID == userId && p.Status == "Completed")
                    .Select(p => p.DocumentID)
                    .ToListAsync();

                return Ok(purchasedDocumentIds);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        // GET: api/purchases/check/{documentId}
        [HttpGet("check/{documentId}")]
        public async Task<ActionResult<bool>> CheckPurchaseStatus(int documentId)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Người dùng không hợp lệ." });
            }

            try
            {
                var isPurchased = await _context.Purchases
                    .AnyAsync(p => p.UserID == userId && p.DocumentID == documentId && p.Status == "Completed");

                return Ok(new { isPurchased });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        // POST: api/purchases/sync
        [HttpPost("sync")]
        public async Task<IActionResult> SyncPurchases([FromBody] SyncPurchasesRequest request)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Người dùng không hợp lệ." });
            }

            try
            {
                // Lấy tất cả ID tài liệu đã mua từ cơ sở dữ liệu
                var purchasedDocumentIds = await _context.Purchases
                    .Where(p => p.UserID == userId && p.Status == "Completed")
                    .Select(p => p.DocumentID)
                    .ToListAsync();

                // So sánh với danh sách của client để tìm các ID thiếu
                var missingIds = purchasedDocumentIds
                    .Except(request.LocalDocumentIds)
                    .ToList();

                // Trả về danh sách cập nhật đầy đủ từ server
                return Ok(new SyncPurchasesResponse
                {
                    ServerDocumentIds = purchasedDocumentIds,
                    MissingIds = missingIds
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
    }

    public class SyncPurchasesRequest
    {
        public List<int> LocalDocumentIds { get; set; } = new List<int>();
    }

    public class SyncPurchasesResponse
    {
        public List<int> ServerDocumentIds { get; set; } = new List<int>();
        public List<int> MissingIds { get; set; } = new List<int>();
    }
} 