using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    [Table("Menu")]
    public class Menu
    {
        [Key]
        public int MenuID { get; set; }
        
        [Required]
        [StringLength(255)]
        public string MenuName { get; set; }
        
        [Required]
        public bool IsActive { get; set; }
        
        [Required]
        [StringLength(255)]
        public string ControllerName { get; set; }
        
        [Required]
        [StringLength(255)]
        public string ActionName { get; set; }
        
        [Required]
        public int Levels { get; set; }
        
        [Required]
        public int ParentID { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Link { get; set; }
        
        [Required]
        public int MenuOrder { get; set; }
        
        [Required]
        public int Position { get; set; }
    }
} 