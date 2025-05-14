using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    public class Document
    {
        public Document()
        {
            // Khởi tạo các collection và giá trị mặc định
            Description = "";
            UploadDate = DateTime.Now;
            Status = "Pending"; // Trạng thái mặc định là chờ duyệt
            IsPaid = false; // Mặc định là miễn phí
            Comments = new List<Comment>();
            Ratings = new List<Rating>();
            Downloads = new List<Download>();
            Favorites = new List<Favorite>();
        }
        
        [Key]
        public int DocumentID { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Title { get; set; }
        
        public string Description { get; set; }
        
        public int? CategoryID { get; set; }
        public int? PublisherID { get; set; }
        public int? AuthorID { get; set; }
        
        public DateTime UploadDate { get; set; }
        
        [StringLength(255)]
        public string FilePath { get; set; }
        
        [StringLength(255)]
        public string ImagePath { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } // Pending, Approved, Rejected
        
        [Required]
        public bool IsPaid { get; set; }
        
        [Range(0, 1000000)]
        public decimal? Price { get; set; }
        
        public int? UserID { get; set; } // Người dùng tải lên tài liệu
        
        // Navigation properties
        [ForeignKey("CategoryID")]
        public Category Category { get; set; }
        
        [ForeignKey("PublisherID")]
        public Publisher Publisher { get; set; }
        
        [ForeignKey("AuthorID")]
        public Author Author { get; set; }
        
        [ForeignKey("UserID")]
        public User User { get; set; }
        
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Rating> Ratings { get; set; }
        public ICollection<Download> Downloads { get; set; }
        public ICollection<Favorite> Favorites { get; set; }
        
        [InverseProperty("Document")]
        public DocumentStatistics Statistics { get; set; }
    }
} 