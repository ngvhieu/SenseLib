using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SenseLib.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly DataContext _context;

        public HomeController(DataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Cung cấp dữ liệu thống kê cho trang chủ Admin
            ViewBag.MenuCount = await _context.Menu.CountAsync();
            ViewBag.ActiveMenuCount = await _context.Menu.Where(m => m.IsActive).CountAsync();
            ViewBag.MainMenuCount = await _context.Menu.Where(m => m.Position == 1).CountAsync();
            
            // Thêm thông tin về slideshow
            ViewBag.SlideshowCount = await _context.Slideshows.CountAsync();
            ViewBag.ActiveSlideshowCount = await _context.Slideshows.Where(s => s.IsActive).CountAsync();
            
            // Thêm thống kê về người dùng và tài liệu
            ViewBag.UserCount = await _context.Users.CountAsync();
            ViewBag.DocumentCount = await _context.Documents.CountAsync();
            
            return View();
        }
        
        public IActionResult MenuManagement()
        {
            return View();
        }
    }
} 