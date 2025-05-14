using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    public class Rating
    {
        [Key]
        public int RatingID { get; set; }
        
        public int DocumentID { get; set; }
        public int UserID { get; set; }
        
        [Required]
        [Range(1, 5)]
        public int RatingValue { get; set; }
        
        [Required]
        public DateTime RatingDate { get; set; }
        
        // Navigation properties
        [ForeignKey("DocumentID")]
        public Document Document { get; set; }
        
        [ForeignKey("UserID")]
        public User User { get; set; }
    }
} 