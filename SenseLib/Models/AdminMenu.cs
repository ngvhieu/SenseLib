using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SenseLib.Models
{
    [Table("AdminMenu")]
    public class AdminMenu
    {
        [Key]
        public int MenuID { get; set; }
        public string MenuName { get; set; }
        public int ItemLevel { get; set; }
        public bool IsActive { get; set; }
        public int ParentLevel { get; set; }
        public int ItemOrder { get; set; }
        public string? ItemTarget { get; set; }
        public string? AreaName { get; set; }
        public string? ControllerName { get; set; }
        public string? ActionName { get; set; }
        public string? Icon { get; set; }
        public string? IdName { get; set; }
    }
}
