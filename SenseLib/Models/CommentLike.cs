using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    public class CommentLike
    {
        [Key]
        public int CommentLikeID { get; set; }
        
        public int CommentID { get; set; }
        public int UserID { get; set; }
        
        [Required]
        public DateTime LikeDate { get; set; }
        
        // Navigation properties
        [ForeignKey(nameof(CommentID))]
        public Comment? Comment { get; set; }
        
        [ForeignKey(nameof(UserID))]
        public User? User { get; set; }
    }
} 