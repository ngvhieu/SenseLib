using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;

namespace SenseLib.Services
{
    public class UserActivityService
    {
        private readonly DataContext _context;

        public UserActivityService(DataContext context)
        {
            _context = context;
        }

        // Thêm hoạt động đọc tài liệu
        public async Task LogReadActivityAsync(int userId, int documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            var activity = new UserActivity
            {
                UserID = userId,
                DocumentID = documentId,
                ActivityType = "Read",
                ActivityDate = DateTime.Now,
                Description = $"Đã đọc tài liệu: {document?.Title ?? "Không xác định"}"
            };
            
            await _context.UserActivities.AddAsync(activity);
            await _context.SaveChangesAsync();
        }

        // Thêm hoạt động thích tài liệu
        public async Task LogLikeActivityAsync(int userId, int documentId, bool isLiked)
        {
            var document = await _context.Documents.FindAsync(documentId);
            var activity = new UserActivity
            {
                UserID = userId,
                DocumentID = documentId,
                ActivityType = isLiked ? "Like" : "Unlike",
                ActivityDate = DateTime.Now,
                Description = isLiked ? 
                    $"Đã thích tài liệu: {document?.Title ?? "Không xác định"}" : 
                    $"Đã bỏ thích tài liệu: {document?.Title ?? "Không xác định"}"
            };
            
            await _context.UserActivities.AddAsync(activity);
            await _context.SaveChangesAsync();
        }

        // Thêm hoạt động tải xuống tài liệu
        public async Task LogDownloadActivityAsync(int userId, int documentId)
        {
            var document = await _context.Documents.FindAsync(documentId);
            var activity = new UserActivity
            {
                UserID = userId,
                DocumentID = documentId,
                ActivityType = "Download",
                ActivityDate = DateTime.Now,
                Description = $"Đã tải xuống tài liệu: {document?.Title ?? "Không xác định"}"
            };
            
            await _context.UserActivities.AddAsync(activity);
            await _context.SaveChangesAsync();
        }

        // Thêm hoạt động bình luận
        public async Task LogCommentActivityAsync(int userId, int documentId, int commentId, string commentContent)
        {
            var document = await _context.Documents.FindAsync(documentId);
            var activity = new UserActivity
            {
                UserID = userId,
                DocumentID = documentId,
                ActivityType = "Comment",
                CommentID = commentId,
                ActivityDate = DateTime.Now,
                Description = $"Đã bình luận tài liệu: {document?.Title ?? "Không xác định"}",
                AdditionalData = commentContent.Length > 100 ? commentContent.Substring(0, 100) + "..." : commentContent
            };
            
            await _context.UserActivities.AddAsync(activity);
            await _context.SaveChangesAsync();
        }

        // Thêm hoạt động mua tài liệu
        public async Task LogPurchaseActivityAsync(int userId, int documentId, decimal amount)
        {
            var document = await _context.Documents.FindAsync(documentId);
            var activity = new UserActivity
            {
                UserID = userId,
                DocumentID = documentId,
                ActivityType = "Purchase",
                ActivityDate = DateTime.Now,
                Description = $"Đã mua tài liệu: {document?.Title ?? "Không xác định"}",
                AdditionalData = $"Giá: {amount} P"
            };
            
            await _context.UserActivities.AddAsync(activity);
            await _context.SaveChangesAsync();
        }

        // Lấy danh sách hoạt động của người dùng
        public async Task<List<UserActivity>> GetUserActivitiesAsync(int userId, string activityType = null, int page = 1, int pageSize = 10)
        {
            var query = _context.UserActivities
                .Include(a => a.Document)
                .Where(a => a.UserID == userId);

            if (!string.IsNullOrEmpty(activityType))
            {
                query = query.Where(a => a.ActivityType == activityType);
            }

            var activities = await query
                .OrderByDescending(a => a.ActivityDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Đảm bảo AdditionalData không chứa giá trị null 
            foreach (var activity in activities)
            {
                if (activity.AdditionalData == null)
                {
                    activity.AdditionalData = string.Empty;
                }
                
                if (activity.Description == null)
                {
                    activity.Description = string.Empty;
                }
            }

            return activities;
        }

        // Lấy tổng số hoạt động của người dùng
        public async Task<int> GetTotalActivitiesAsync(int userId, string activityType = null)
        {
            var query = _context.UserActivities.Where(a => a.UserID == userId);

            if (!string.IsNullOrEmpty(activityType))
            {
                query = query.Where(a => a.ActivityType == activityType);
            }

            return await query.CountAsync();
        }

        // Lấy hoạt động theo loại
        public async Task<List<UserActivity>> GetUserActivitiesByTypeAsync(int userId, string activityType, int page = 1, int pageSize = 10)
        {
            return await _context.UserActivities
                .Where(ua => ua.UserID == userId && ua.ActivityType == activityType)
                .Include(ua => ua.Document)
                .OrderByDescending(ua => ua.ActivityDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
} 