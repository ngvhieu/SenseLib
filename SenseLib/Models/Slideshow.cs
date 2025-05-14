using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    [Table("Slideshow")]
    public class Slideshow
    {
        [Key]
        public int SlideID { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Title { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [StringLength(255)]
        public string ImagePath { get; set; }
        
        [StringLength(255)]
        public string? Link { get; set; }
        
        [Required]
        public int DisplayOrder { get; set; }
        
        [Required]
        public bool IsActive { get; set; }
        
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
} 