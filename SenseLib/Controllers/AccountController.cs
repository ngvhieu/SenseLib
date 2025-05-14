using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using SenseLib.Services;

namespace SenseLib.Controllers
{
    public class AccountController : Controller
    {
        private readonly DataContext _context;
        private readonly IEmailService _emailService;
        
        public AccountController(DataContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đầy đủ thông tin đăng nhập";
                return View();
            }
            
            // Trước tiên, tìm người dùng theo email (không cần kiểm tra mật khẩu ở bước này)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            // Nếu không tìm thấy người dùng
            if (user == null)
            {
                ViewBag.ErrorMessage = "Email hoặc mật khẩu không chính xác";
                return View();
            }
            
            // Lấy cấu hình số lần đăng nhập sai tối đa và thời gian khóa
            int maxLoginAttempts = 5; // Giá trị mặc định
            int lockoutTimeMinutes = 30; // Giá trị mặc định
            
            // Tìm cấu hình từ database
            var maxAttemptsConfig = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "MaxLoginAttempts");
            var lockoutTimeConfig = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "LockoutTimeMinutes");
            
            // Nếu cấu hình tồn tại, cập nhật giá trị
            if (maxAttemptsConfig != null && int.TryParse(maxAttemptsConfig.ConfigValue, out int configMaxAttempts))
            {
                maxLoginAttempts = configMaxAttempts;
            }
            
            if (lockoutTimeConfig != null && int.TryParse(lockoutTimeConfig.ConfigValue, out int configLockoutTime))
            {
                lockoutTimeMinutes = configLockoutTime;
            }
            
            // Kiểm tra xem tài khoản có đang bị khóa không
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
            {
                var remainingMinutes = Math.Ceiling((user.LockoutEnd.Value - DateTime.Now).TotalMinutes);
                ViewBag.ErrorMessage = $"Tài khoản của bạn đã bị khóa tạm thời. Vui lòng thử lại sau {remainingMinutes} phút.";
                return View();
            }
            
            // Mã hóa mật khẩu để so sánh
            string hashedPassword = HashPassword(password);
            
            // Kiểm tra mật khẩu
            if (user.Password != hashedPassword)
            {
                // Tăng số lần đăng nhập sai
                user.LoginAttempts += 1;
                
                // Nếu vượt quá số lần đăng nhập sai tối đa, khóa tài khoản
                if (user.LoginAttempts >= maxLoginAttempts)
                {
                    user.LockoutEnd = DateTime.Now.AddMinutes(lockoutTimeMinutes);
                    ViewBag.ErrorMessage = $"Bạn đã nhập sai mật khẩu quá {maxLoginAttempts} lần. Tài khoản của bạn đã bị khóa tạm thời trong {lockoutTimeMinutes} phút.";
                }
                else
                {
                    int remainingAttempts = maxLoginAttempts - user.LoginAttempts;
                    ViewBag.ErrorMessage = $"Email hoặc mật khẩu không chính xác. Bạn còn {remainingAttempts} lần thử trước khi tài khoản bị khóa.";
                }
                
                // Lưu các thay đổi vào database
                await _context.SaveChangesAsync();
                
                return View();
            }
            
            // Kiểm tra trạng thái tài khoản
            if (user.Status != "Active")
            {
                ViewBag.ErrorMessage = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên";
                return View();
            }
            
            // Đăng nhập thành công, đặt lại số lần đăng nhập sai và xóa thời gian khóa
            user.LoginAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();
            
            // Tạo claims cho người dùng
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };
            
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(rememberMe ? 14 : 1)
            };
            
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
            
            // Thêm thông báo đăng nhập thành công
            TempData["SuccessMessage"] = "Đăng nhập thành công!";
            
            // Chuyển hướng dựa vào role
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            
            return RedirectToAction("Index", "Home");
        }
        
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string email, string fullName, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.ErrorMessage = "Vui lòng điền đầy đủ thông tin đăng ký";
                return View();
            }
            
            // Kiểm tra định dạng email
            if (!IsValidEmail(email))
            {
                ViewBag.ErrorMessage = "Định dạng email không hợp lệ. Vui lòng kiểm tra lại";
                return View();
            }
            
            // Kiểm tra mật khẩu
            if (password.Length < 6)
            {
                ViewBag.ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự";
                return View();
            }
            
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu đã nhập";
                return View();
            }
            
            // Kiểm tra email đã tồn tại chưa
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                ViewBag.ErrorMessage = "Email này đã được đăng ký trong hệ thống";
                return View();
            }
            
            // Kiểm tra username đã tồn tại chưa
            bool usernameExists = await _context.Users.AnyAsync(u => u.Username == username);
            if (usernameExists)
            {
                ViewBag.ErrorMessage = "Tên đăng nhập này đã tồn tại. Vui lòng chọn tên khác";
                return View();
            }
            
            // Tạo người dùng mới
            var user = new User
            {
                Username = username,
                Email = email,
                FullName = fullName,
                Password = HashPassword(password), // Mã hóa mật khẩu
                Role = "User",
                Status = "Active",
                ProfileImage = "smile.jpg" // Ảnh đại diện mặc định
            };
            
            // Lưu vào cơ sở dữ liệu
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            // Đăng nhập người dùng
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };
            
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            
            // Thêm thông báo đăng ký thành công
            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công!";
            
            return RedirectToAction("Index", "Home");
        }
        
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập địa chỉ email của bạn";
                return View();
            }
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            if (user == null)
            {
                // Không tiết lộ rằng email không tồn tại trong hệ thống
                ViewBag.SuccessMessage = "Nếu địa chỉ email này tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu";
                return View();
            }
            
            try
            {
                // Tạo token ngẫu nhiên
                string resetToken = GenerateResetToken();
                
                // Lưu token vào cơ sở dữ liệu
                var tokenEntity = new PasswordResetToken
                {
                    UserId = user.UserID,
                    Token = resetToken,
                    ExpiryDate = DateTime.Now.AddHours(24), // Token hết hạn sau 24 giờ
                    IsUsed = false
                };
                
                _context.PasswordResetTokens.Add(tokenEntity);
                await _context.SaveChangesAsync();
                
                // Tạo liên kết đặt lại mật khẩu
                var resetLink = Url.Action("ResetPassword", "Account", 
                    new { token = resetToken, email = user.Email }, 
                    Request.Scheme);
                    
                // Gửi email với liên kết đặt lại mật khẩu
                await _emailService.SendPasswordResetAsync(user.Email, user.FullName, resetLink);
                
                ViewBag.SuccessMessage = "Chúng tôi đã gửi email hướng dẫn đặt lại mật khẩu. Vui lòng kiểm tra hộp thư của bạn.";
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không hiển thị thông báo chi tiết cho người dùng
                Console.WriteLine($"Lỗi khi gửi email đặt lại mật khẩu: {ex.Message}");
                ViewBag.SuccessMessage = "Chúng tôi đã gửi email hướng dẫn đặt lại mật khẩu. Vui lòng kiểm tra hộp thư của bạn.";
            }
            
            return View();
        }
        
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            // Kiểm tra token và email
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Account");
            }
            
            // Tìm token hợp lệ
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.User.Email == email && !t.IsUsed && t.ExpiryDate > DateTime.Now);
                
            if (resetToken == null)
            {
                ViewBag.ErrorMessage = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn";
                return View();
            }
            
            // Truyền token và email cho view
            ViewBag.Token = token;
            ViewBag.Email = email;
            
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Account");
            }
            
            // Xác thực mật khẩu
            if (string.IsNullOrEmpty(password) || password.Length < 6)
            {
                ViewBag.ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự. Vui lòng nhập lại";
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View();
            }
            
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu xác nhận không khớp với mật khẩu mới. Vui lòng kiểm tra lại";
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View();
            }
            
            // Tìm token và người dùng tương ứng
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token && t.User.Email == email && !t.IsUsed && t.ExpiryDate > DateTime.Now);
                
            if (resetToken == null)
            {
                ViewBag.ErrorMessage = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng thử lại";
                return View();
            }
            
            // Cập nhật mật khẩu
            resetToken.User.Password = HashPassword(password);
            
            // Đánh dấu token đã sử dụng
            resetToken.IsUsed = true;
            
            // Lưu thay đổi
            _context.Update(resetToken.User);
            _context.Update(resetToken);
            await _context.SaveChangesAsync();
            
            ViewBag.SuccessMessage = "Mật khẩu của bạn đã được đặt lại thành công. Bạn có thể đăng nhập bằng mật khẩu mới.";
            return View();
        }
        
        // Tạo token ngẫu nhiên cho đặt lại mật khẩu
        private string GenerateResetToken()
        {
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                var tokenData = new byte[32];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return NotFound();
            }
            
            return View(user);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(int userId, string fullName, string email, IFormFile profileImage)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            
            // Kiểm tra người dùng hiện tại có đúng là người cần cập nhật hồ sơ
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (userId != currentUserId)
            {
                return Forbid();
            }
            
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            
            // Cập nhật thông tin cơ bản
            user.FullName = fullName;
            user.Email = email;
            
            // Xử lý tải lên ảnh hồ sơ nếu có
            if (profileImage != null && profileImage.Length > 0)
            {
                // Xác thực kiểu tệp (chỉ cho phép hình ảnh)
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(profileImage.FileName).ToLower();
                
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Chỉ chấp nhận các tệp hình ảnh (jpg, jpeg, png, gif)";
                    return RedirectToAction("Profile");
                }
                
                // Tạo tên tệp duy nhất để tránh trùng lặp
                var fileName = Guid.NewGuid().ToString() + extension;
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles", fileName);
                
                // Lưu tệp vào thư mục uploads/profiles
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }
                
                // Cập nhật đường dẫn hình ảnh trong DB
                // Xóa tệp ảnh cũ nếu không phải ảnh mặc định
                if (user.ProfileImage != "smile.jpg")
                {
                    var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles", user.ProfileImage);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }
                
                user.ProfileImage = fileName;
            }
            
            // Lưu thay đổi vào database
            _context.Update(user);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int userId, string currentPassword, string newPassword, string confirmPassword)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            
            // Kiểm tra người dùng hiện tại
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (userId != currentUserId)
            {
                return Forbid();
            }
            
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            
            // Kiểm tra mật khẩu hiện tại
            string hashedCurrentPassword = HashPassword(currentPassword);
            if (user.Password != hashedCurrentPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không chính xác. Vui lòng kiểm tra lại";
                return RedirectToAction("Profile");
            }
            
            // Kiểm tra mật khẩu mới
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp với mật khẩu mới";
                return RedirectToAction("Profile");
            }
            
            if (newPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự";
                return RedirectToAction("Profile");
            }
            
            // Cập nhật mật khẩu mới
            user.Password = HashPassword(newPassword);
            _context.Update(user);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }
        
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
        
        // Hàm mã hóa mật khẩu
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        // Kiểm tra định dạng email
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
} 