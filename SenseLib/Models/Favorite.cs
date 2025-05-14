using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SenseLib.Models
{
    [Table("Favorites")]
    public class Favorite
    {
        [Key]
        public int FavoriteID { get; set; }
        
        [Required]
        public int UserID { get; set; }
        
        [Required]
        public int DocumentID { get; set; }
        
        // Navigation properties
        [ForeignKey("UserID")]
        public User User { get; set; }
        
        [ForeignKey("DocumentID")]
        public Document Document { get; set; }
    }
} 