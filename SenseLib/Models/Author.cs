using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SenseLib.Models
{
    public class Author
    {
        public Author()
        {
            Documents = new List<Document>();
        }
        
        [Key]
        public int AuthorID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string AuthorName { get; set; }
        
        [Required(AllowEmptyStrings = true)]
        public string Bio { get; set; } = "";
        
        // Navigation properties
        public ICollection<Document> Documents { get; set; }
    }
} 