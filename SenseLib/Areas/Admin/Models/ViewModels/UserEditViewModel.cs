using System.ComponentModel.DataAnnotations;
using SenseLib.Models;

namespace SenseLib.Areas.Admin.Models.ViewModels
{
    public class UserEditViewModel
    {
        public UserEditViewModel()
        {
        }
        
        public UserEditViewModel(User user)
        {
            UserID = user.UserID;
            Username = user.Username;
            Email = user.Email;
            FullName = user.FullName;
            Role = user.Role;
            Status = user.Status;
            ProfileImage = user.ProfileImage;
            // Password không được set khi khởi tạo từ User
        }
        
        [Required]
        public int UserID { get; set; }
        
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự")]
        public string Username { get; set; }
        
        // Password không bắt buộc khi edit
        [StringLength(255, ErrorMessage = "Mật khẩu không được vượt quá 255 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        
        [Required(ErrorMessage = "Email là bắt buộc")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string FullName { get; set; }
        
        [Required(ErrorMessage = "Quyền là bắt buộc")]
        [StringLength(20, ErrorMessage = "Quyền không được vượt quá 20 ký tự")]
        public string Role { get; set; }
        
        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [StringLength(20, ErrorMessage = "Trạng thái không được vượt quá 20 ký tự")]
        public string Status { get; set; }
        
        [StringLength(255, ErrorMessage = "Đường dẫn ảnh hồ sơ không được vượt quá 255 ký tự")]
        public string ProfileImage { get; set; }
        
        // Phương thức chuyển từ ViewModel sang Model
        public void UpdateUser(User user)
        {
            user.Username = Username;
            user.Email = Email;  
            user.FullName = FullName;
            user.Role = Role;
            user.Status = Status;
            
            // Chỉ cập nhật Password nếu được cung cấp và không phải là placeholder
            if (!string.IsNullOrEmpty(Password) && Password != "********")
            {
                user.Password = Password; // Lưu ý: Cần hash trong controller
            }
        }
    }
} 