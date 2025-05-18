using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    public class UserActivity
    {
        [Key]
        public int ActivityID { get; set; }
        
        public int UserID { get; set; }
        
        [ForeignKey("UserID")]
        public User User { get; set; }
        
        public int? DocumentID { get; set; }
        
        [ForeignKey("DocumentID")]
        public Document? Document { get; set; }
        
        public int? CommentID { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string ActivityType { get; set; } // Read, Like, Download, Comment, Purchase
        
        [Required]
        public DateTime ActivityDate { get; set; }
        
        [MaxLength(500)]
        public string? Description { get; set; } = string.Empty;
        
        [MaxLength(1000)]
        public string? AdditionalData { get; set; } = string.Empty;
    }
} 