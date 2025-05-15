using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    public class Wallet
    {
        [Key]
        public int WalletID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Balance { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime LastUpdatedDate { get; set; }
        
        // Navigation property
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
        
        public virtual ICollection<WalletTransaction> Transactions { get; set; }
    }
} 