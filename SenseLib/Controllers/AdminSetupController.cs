using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Collections.Generic;

namespace SenseLib.Controllers
{
    public class AdminSetupController : Controller
    {
        private readonly DataContext _context;
        
        public AdminSetupController(DataContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Kiểm tra xem đã có admin chưa
            bool hasAdmin = await _context.Users.AnyAsync(u => u.Role == "Admin");
            
            // Nếu đã có admin, thông báo không thể tạo thêm
            if (hasAdmin)
            {
                ViewBag.Message = "Tài khoản admin đã tồn tại trong hệ thống. Vui lòng liên hệ quản trị viên nếu bạn cần quyền truy cập.";
                ViewBag.HasAdmin = true;
            }
            else
            {
                ViewBag.Message = "Chưa có tài khoản admin trong hệ thống. Vui lòng tạo tài khoản admin đầu tiên.";
                ViewBag.HasAdmin = false;
            }
            
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(string username, string email, string fullName, string password, string confirmPassword)
        {
            // Kiểm tra xem đã có admin chưa
            bool hasAdmin = await _context.Users.AnyAsync(u => u.Role == "Admin");
            
            if (hasAdmin)
            {
                ViewBag.ErrorMessage = "Tài khoản admin đã tồn tại trong hệ thống.";
                ViewBag.HasAdmin = true;
                return View("Index");
            }
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.ErrorMessage = "Vui lòng điền đầy đủ thông tin";
                ViewBag.HasAdmin = false;
                return View("Index");
            }
            
            // Kiểm tra định dạng email
            if (!IsValidEmail(email))
            {
                ViewBag.ErrorMessage = "Định dạng email không hợp lệ";
                ViewBag.HasAdmin = false;
                return View("Index");
            }
            
            // Kiểm tra mật khẩu
            if (password.Length < 6)
            {
                ViewBag.ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự";
                ViewBag.HasAdmin = false;
                return View("Index");
            }
            
            if (password != confirmPassword)
            {
                ViewBag.ErrorMessage = "Mật khẩu xác nhận không khớp";
                ViewBag.HasAdmin = false;
                return View("Index");
            }
            
            // Kiểm tra email đã tồn tại chưa
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
            {
                ViewBag.ErrorMessage = "Email đã được đăng ký";
                ViewBag.HasAdmin = false;
                return View("Index");
            }
            
            // Kiểm tra username đã tồn tại chưa
            bool usernameExists = await _context.Users.AnyAsync(u => u.Username == username);
            if (usernameExists)
            {
                ViewBag.ErrorMessage = "Tên đăng nhập đã tồn tại";
                ViewBag.HasAdmin = false;
                return View("Index");
            }
            
            // Tạo tài khoản admin mới
            var admin = new User
            {
                Username = username,
                Email = email,
                FullName = fullName,
                Password = HashPassword(password), // Mã hóa mật khẩu
                Role = "Admin",
                Status = "Active"
            };
            
            // Lưu vào cơ sở dữ liệu
            _context.Users.Add(admin);
            await _context.SaveChangesAsync();
            
            // Đăng nhập cho admin
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.NameIdentifier, admin.UserID.ToString()),
                new Claim(ClaimTypes.Role, admin.Role)
            };
            
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            
            // Thêm thông báo tạo tài khoản thành công
            TempData["SuccessMessage"] = "Tạo tài khoản admin thành công!";
            
            return RedirectToAction("Index", "Home", new { area = "Admin" });
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

        // GET: /AdminSetup/AddDefaultMenus
        public async Task<IActionResult> AddDefaultMenus()
        {
            var messages = new List<string>();
            
            // Menu About
            bool aboutMenuExists = await _context.Menu
                .AnyAsync(m => m.ControllerName == "About" && m.ActionName == "Index" && m.Position == 1);

            if (!aboutMenuExists)
            {
                // Cập nhật menu About cũ nếu tồn tại
                var oldAboutMenu = await _context.Menu
                    .FirstOrDefaultAsync(m => m.ControllerName == "Home" && m.ActionName == "About");
                    
                if (oldAboutMenu != null)
                {
                    oldAboutMenu.ControllerName = "About";
                    oldAboutMenu.ActionName = "Index";
                    oldAboutMenu.Link = "/About";
                    await _context.SaveChangesAsync();
                    messages.Add("Đã cập nhật menu Giới thiệu thành công");
                }
                else
                {
                    int maxOrder = await _context.Menu
                        .Where(m => m.Position == 1 && m.ParentID == 0)
                        .Select(m => m.MenuOrder)
                        .DefaultIfEmpty(0)
                        .MaxAsync();
                    
                    var aboutMenu = new Menu
                    {
                        MenuName = "Giới thiệu",
                        IsActive = true,
                        ControllerName = "About",
                        ActionName = "Index",
                        Levels = 1,
                        ParentID = 0,
                        Link = "/About",
                        MenuOrder = maxOrder + 1,
                        Position = 1 // Main menu
                    };

                    _context.Menu.Add(aboutMenu);
                    await _context.SaveChangesAsync();
                    
                    messages.Add("Đã thêm menu Giới thiệu thành công");
                }
            }
            else
            {
                messages.Add("Menu Giới thiệu đã tồn tại");
            }
            
            // Menu Contact
            bool contactMenuExists = await _context.Menu
                .AnyAsync(m => m.ControllerName == "Contact" && m.ActionName == "Index" && m.Position == 1);

            if (!contactMenuExists)
            {
                // Cập nhật menu Contact cũ nếu tồn tại
                var oldContactMenu = await _context.Menu
                    .FirstOrDefaultAsync(m => m.ControllerName == "Home" && m.ActionName == "Contact");
                    
                if (oldContactMenu != null)
                {
                    oldContactMenu.ControllerName = "Contact";
                    oldContactMenu.ActionName = "Index";
                    oldContactMenu.Link = "/Contact";
                    await _context.SaveChangesAsync();
                    messages.Add("Đã cập nhật menu Liên hệ thành công");
                }
                else
                {
                    int maxOrder = await _context.Menu
                        .Where(m => m.Position == 1 && m.ParentID == 0)
                        .Select(m => m.MenuOrder)
                        .DefaultIfEmpty(0)
                        .MaxAsync();
                    
                    var contactMenu = new Menu
                    {
                        MenuName = "Liên hệ",
                        IsActive = true,
                        ControllerName = "Contact",
                        ActionName = "Index",
                        Levels = 1,
                        ParentID = 0,
                        Link = "/Contact",
                        MenuOrder = maxOrder + 1,
                        Position = 1 // Main menu
                    };

                    _context.Menu.Add(contactMenu);
                    await _context.SaveChangesAsync();
                    
                    messages.Add("Đã thêm menu Liên hệ thành công");
                }
            }
            else
            {
                messages.Add("Menu Liên hệ đã tồn tại");
            }
            
            // Hiển thị thông báo
            TempData["SuccessMessage"] = string.Join("<br>", messages);
            
            return RedirectToAction("Index");
        }
    }
} 