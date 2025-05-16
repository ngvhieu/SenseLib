using System;

namespace SenseLib.Models
{
    public class UserActivityViewModel
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        
        public int CommentCount { get; set; }
        public int RatingCount { get; set; }
        public int DownloadCount { get; set; }
        public int FavoriteCount { get; set; }
        
        public DateTime? LastCommentDate { get; set; }
        public DateTime? LastDownloadDate { get; set; }
        
        // Tổng số hoạt động
        public int TotalActivities => CommentCount + RatingCount + DownloadCount + FavoriteCount;
        
        // Thời gian hoạt động gần nhất
        public DateTime? LastActivityDate => 
            (LastCommentDate.HasValue && LastDownloadDate.HasValue) 
                ? (LastCommentDate > LastDownloadDate ? LastCommentDate : LastDownloadDate)
                : (LastCommentDate ?? LastDownloadDate);
    }
} 