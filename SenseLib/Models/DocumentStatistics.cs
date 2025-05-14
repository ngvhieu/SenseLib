using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    public class DocumentStatistics
    {
		[Key]
        public int StatisticsID { get; set; }
        
        public int DocumentID { get; set; }
        
        [Required]
        public int ViewCount { get; set; }
        
        [Required]
        public DateTime LastUpdated { get; set; }
        
        // Navigation property
        [ForeignKey(nameof(DocumentID))]
        public Document? Document { get; set; }
    }
} 