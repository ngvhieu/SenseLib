using System.ComponentModel.DataAnnotations;

namespace SenseLib.Models
{
    public class SystemConfig
    {
        [Key]
        public int ConfigID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ConfigKey { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string ConfigValue { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
    }
} 