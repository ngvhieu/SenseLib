using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    public class Download
    {
        [Key]
        public int DownloadID { get; set; }
        
        public int UserID { get; set; }
        public int DocumentID { get; set; }
        
        [Required]
        public DateTime DownloadDate { get; set; }
        
        // Loại tải xuống: Original (mặc định) hoặc PDF
        [StringLength(20)]
        public string DownloadType { get; set; } = "Original";
        
        // Navigation properties
        [ForeignKey("UserID")]
        public User? User { get; set; }
        
        [ForeignKey("DocumentID")]
        public Document? Document { get; set; }
    }
} 