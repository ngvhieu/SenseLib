using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using SenseLib.Services;

namespace SenseLib.Controllers
{
    [Authorize]
    public class UserActivityController : Controller
    {
        private readonly DataContext _context;
        private readonly UserActivityService _activityService;

        public UserActivityController(DataContext context, UserActivityService activityService)
        {
            _context = context;
            _activityService = activityService;
        }

        // Trang chính hiển thị tất cả hoạt động
        public async Task<IActionResult> Index(int page = 1)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            int pageSize = 10;
            var activities = await _activityService.GetUserActivitiesAsync(userId, null, page, pageSize);
            int totalItems = await _activityService.GetTotalActivitiesAsync(userId);
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // Lấy số lượng từng loại hoạt động
            ViewBag.ReadCount = await _activityService.GetTotalActivitiesAsync(userId, "Read");
            ViewBag.LikeCount = await _activityService.GetTotalActivitiesAsync(userId, "Like"); 
            ViewBag.DownloadCount = await _activityService.GetTotalActivitiesAsync(userId, "Download");
            ViewBag.CommentCount = await _activityService.GetTotalActivitiesAsync(userId, "Comment");
            ViewBag.PurchaseCount = await _activityService.GetTotalActivitiesAsync(userId, "Purchase");

            return View(activities);
        }

        // Trang hiển thị lịch sử đọc tài liệu
        public async Task<IActionResult> ReadHistory(int page = 1)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            int pageSize = 10;
            var activities = await _activityService.GetUserActivitiesAsync(userId, "Read", page, pageSize);
            int totalItems = await _activityService.GetTotalActivitiesAsync(userId, "Read");
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(activities);
        }

        // Trang hiển thị lịch sử thích tài liệu
        public async Task<IActionResult> LikeHistory(int page = 1)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            int pageSize = 10;
            var activities = await _activityService.GetUserActivitiesAsync(userId, "Like", page, pageSize);
            int totalItems = await _activityService.GetTotalActivitiesAsync(userId, "Like");
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(activities);
        }

        // Trang hiển thị lịch sử tải xuống tài liệu
        public async Task<IActionResult> DownloadHistory(int page = 1)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            int pageSize = 10;
            var activities = await _activityService.GetUserActivitiesAsync(userId, "Download", page, pageSize);
            int totalItems = await _activityService.GetTotalActivitiesAsync(userId, "Download");
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(activities);
        }

        // Trang hiển thị lịch sử bình luận
        public async Task<IActionResult> CommentHistory(int page = 1)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            int pageSize = 10;
            var activities = await _activityService.GetUserActivitiesAsync(userId, "Comment", page, pageSize);
            int totalItems = await _activityService.GetTotalActivitiesAsync(userId, "Comment");
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(activities);
        }

        // Trang hiển thị lịch sử mua tài liệu
        public async Task<IActionResult> PurchaseHistory(int page = 1)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            int pageSize = 10;
            var activities = await _activityService.GetUserActivitiesAsync(userId, "Purchase", page, pageSize);
            int totalItems = await _activityService.GetTotalActivitiesAsync(userId, "Purchase");
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(activities);
        }

        // Xóa lịch sử hoạt động
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear(string activityType = null)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Lấy danh sách hoạt động cần xóa
            var activities = _context.UserActivities.Where(ua => ua.UserID == userId);
            
            // Nếu có chỉ định loại hoạt động, chỉ xóa loại đó
            if (!string.IsNullOrEmpty(activityType))
            {
                activities = activities.Where(ua => ua.ActivityType == activityType);
            }
            
            // Xóa các hoạt động
            _context.UserActivities.RemoveRange(activities);
            await _context.SaveChangesAsync();
            
            // Thông báo thành công
            TempData["SuccessMessage"] = "Đã xóa lịch sử hoạt động thành công";
            
            // Chuyển hướng về trang trước đó
            return RedirectToAction("Index");
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }
    }
} 