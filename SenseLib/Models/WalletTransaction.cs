using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    public class WalletTransaction
    {
        [Key]
        public int TransactionID { get; set; }
        
        [Required]
        public int WalletID { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }
        
        [Required]
        public DateTime TransactionDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Type { get; set; } // "Credit" (nhận tiền), "Debit" (rút tiền)
        
        [StringLength(255)]
        public string Description { get; set; }
        
        // Lưu ID tài liệu nếu là giao dịch từ bán tài liệu
        public int? DocumentID { get; set; }
        
        // Lưu ID giao dịch mua nếu là giao dịch từ bán tài liệu
        public int? PurchaseID { get; set; }
        
        // Navigation properties
        [ForeignKey("WalletID")]
        public virtual Wallet Wallet { get; set; }
        
        [ForeignKey("DocumentID")]
        public virtual Document Document { get; set; }
        
        [ForeignKey("PurchaseID")]
        public virtual Purchase Purchase { get; set; }
    }
} 