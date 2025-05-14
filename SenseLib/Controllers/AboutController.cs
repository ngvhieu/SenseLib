using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;

namespace SenseLib.Controllers
{
    public class AboutController : Controller
    {
        private readonly DataContext _context;

        public AboutController(DataContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Có thể thêm dữ liệu động cho trang Giới thiệu nếu cần
            ViewBag.FoundedYear = 2023;
            ViewBag.CurrentDocumentCount = _context.Documents.Count();
            ViewBag.CurrentUserCount = _context.Users.Count();
            
            return View();
        }
    }
} 