using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    public class PasswordResetToken
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Token { get; set; }
        
        [Required]
        public DateTime ExpiryDate { get; set; }
        
        public bool IsUsed { get; set; } = false;
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
} 