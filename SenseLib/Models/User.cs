using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SenseLib.Models
{
    public class User
    {
        public User()
        {
            Comments = new List<Comment>();
            Ratings = new List<Rating>();
            Favorites = new List<Favorite>();
            Downloads = new List<Download>();
            Transactions = new List<Transaction>();
            CommentLikes = new List<CommentLike>();
            
            // Giá trị mặc định
            Username = string.Empty;
            Password = string.Empty;
            Email = string.Empty;
            FullName = string.Empty;
            Role = "User";
            Status = "Active";
            ProfileImage = "/uploads/profile/smile.jpg"; // Giá trị mặc định cho ảnh hồ sơ
            LoginAttempts = 0; // Khởi tạo số lần đăng nhập sai là 0
        }
        
        [Key]
        public int UserID { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Password { get; set; }
        
        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Role { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; }
        
        [StringLength(255)]
        public string ProfileImage { get; set; } // Đường dẫn đến hình ảnh hồ sơ
        
        // Các trường để quản lý khóa tài khoản
        public int LoginAttempts { get; set; } // Số lần đăng nhập sai liên tiếp
        public DateTime? LockoutEnd { get; set; } // Thời gian khóa tài khoản hết hạn
        
        // Navigation properties
        public ICollection<Comment> Comments { get; set; }
        public ICollection<CommentLike> CommentLikes { get; set; }
        public ICollection<Rating> Ratings { get; set; }
        public ICollection<Favorite> Favorites { get; set; }
        public ICollection<Download> Downloads { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
    }
} 