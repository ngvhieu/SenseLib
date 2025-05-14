using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SenseLib.Models
{
    public class Category
    {
        public Category()
        {
            Documents = new List<Document>();
        }
        
        [Key]
        public int CategoryID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; }
        
        [Required(AllowEmptyStrings = true)]
        public string Description { get; set; } = "";
        
        [Required]
        [StringLength(10)]
        public string Status { get; set; }
        
        // Navigation properties
        public ICollection<Document> Documents { get; set; }
    }
} 