using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SenseLib.Models
{
    public class Publisher
    {
        [Key]
        public int PublisherID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string PublisherName { get; set; }
        
        [StringLength(255)]
        public string Address { get; set; }
        
        [StringLength(20)]
        public string Phone { get; set; }
        
        // Navigation properties
        public ICollection<Document> Documents { get; set; }
    }
} 