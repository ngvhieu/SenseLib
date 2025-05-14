using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    public class Purchase
    {
        [Key]
        public int PurchaseID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Required]
        public int DocumentID { get; set; }
        
        [Required]
        public DateTime PurchaseDate { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }
        
        public string TransactionCode { get; set; }
        
        public string Status { get; set; } // "Completed", "Pending", "Failed"
        
        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
        
        [ForeignKey("DocumentID")]
        public virtual Document Document { get; set; }
    }
} 