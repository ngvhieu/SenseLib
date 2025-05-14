using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionID { get; set; }
        
        public int UserID { get; set; }
        
        [Required]
        [StringLength(20)]
        public string TransactionType { get; set; }
        
        [Required]
        public DateTime TransactionDate { get; set; }
        
        // Navigation properties
        [ForeignKey("UserID")]
        public User User { get; set; }
    }
} 