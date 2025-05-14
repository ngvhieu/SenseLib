using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    public class Comment
    {
        public Comment()
        {
            CommentLikes = new List<CommentLike>();
            Replies = new List<Comment>();
        }
        
        [Key]
        public int CommentID { get; set; }
        
        public int DocumentID { get; set; }
        public int UserID { get; set; }
        
        [Required]
        public string CommentText { get; set; }
        
        [Required]
        public DateTime CommentDate { get; set; }
        
        // Số lượt thích
        public int LikeCount { get; set; }
        
        // Trường để hỗ trợ trả lời
        public int? ParentCommentID { get; set; }
        
        // Navigation properties
        [ForeignKey(nameof(DocumentID))]
        public Document? Document { get; set; }
        
        [ForeignKey(nameof(UserID))]
        public User? User { get; set; }
        
        [ForeignKey(nameof(ParentCommentID))]
        public Comment? ParentComment { get; set; }
        
        // Collection các trả lời
        public ICollection<Comment> Replies { get; set; }
        
        // Collection các lượt thích
        public ICollection<CommentLike> CommentLikes { get; set; }
    }
} 