using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SenseLib.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SiteMenuController : Controller
    {
        private readonly DataContext _context;

        public SiteMenuController(DataContext context)
        {
            _context = context;
        }
        
        // GET: /SiteMenu/Index
        public IActionResult Index()
        {
            return View();
        }

        // GET: /SiteMenu/AddAboutMenuItem
        public async Task<IActionResult> AddAboutMenuItem()
        {
            // Kiểm tra xem menu item About đã tồn tại chưa
            bool aboutMenuExists = await _context.Menu
                .AnyAsync(m => m.ControllerName == "Home" && m.ActionName == "About");

            if (!aboutMenuExists)
            {
                // Xác định thứ tự lớn nhất hiện tại trong menu chính
                int maxOrder = await _context.Menu
                    .Where(m => m.Position == 1 && m.ParentID == 0)
                    .Select(m => m.MenuOrder)
                    .DefaultIfEmpty(0)
                    .MaxAsync();
                
                // Tạo menu item About
                var aboutMenu = new Menu
                {
                    MenuName = "Giới thiệu",
                    IsActive = true,
                    ControllerName = "Home",
                    ActionName = "About",
                    Levels = 1,
                    ParentID = 0,
                    Link = "/Home/About",
                    MenuOrder = maxOrder + 1,
                    Position = 1 // Main menu
                };

                // Thêm vào database
                _context.Menu.Add(aboutMenu);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm menu Giới thiệu vào thanh điều hướng chính.";
            }
            else
            {
                TempData["InfoMessage"] = "Menu Giới thiệu đã tồn tại.";
            }

            return RedirectToAction("Index", "SiteMenu");
        }

        // GET: /SiteMenu/AddContactMenuItem
        public async Task<IActionResult> AddContactMenuItem()
        {
            // Kiểm tra xem menu item Contact đã tồn tại chưa
            bool contactMenuExists = await _context.Menu
                .AnyAsync(m => m.ControllerName == "Home" && m.ActionName == "Contact");

            if (!contactMenuExists)
            {
                // Xác định thứ tự lớn nhất hiện tại trong menu chính
                int maxOrder = await _context.Menu
                    .Where(m => m.Position == 1 && m.ParentID == 0)
                    .Select(m => m.MenuOrder)
                    .DefaultIfEmpty(0)
                    .MaxAsync();
                
                // Tạo menu item Contact
                var contactMenu = new Menu
                {
                    MenuName = "Liên hệ",
                    IsActive = true,
                    ControllerName = "Home",
                    ActionName = "Contact",
                    Levels = 1,
                    ParentID = 0,
                    Link = "/Home/Contact",
                    MenuOrder = maxOrder + 1,
                    Position = 1 // Main menu
                };

                // Thêm vào database
                _context.Menu.Add(contactMenu);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm menu Liên hệ vào thanh điều hướng chính.";
            }
            else
            {
                TempData["InfoMessage"] = "Menu Liên hệ đã tồn tại.";
            }

            return RedirectToAction("Index", "SiteMenu");
        }
        
        // GET: /SiteMenu/AddFooterAboutMenuItem
        public async Task<IActionResult> AddFooterAboutMenuItem()
        {
            // Kiểm tra xem menu item About đã tồn tại trong footer chưa
            bool aboutMenuExists = await _context.Menu
                .AnyAsync(m => m.ControllerName == "Home" && m.ActionName == "About" && m.Position == 2);

            if (!aboutMenuExists)
            {
                // Xác định thứ tự lớn nhất hiện tại trong footer menu
                int maxOrder = await _context.Menu
                    .Where(m => m.Position == 2)
                    .Select(m => m.MenuOrder)
                    .DefaultIfEmpty(0)
                    .MaxAsync();
                
                // Tạo menu item About trong footer
                var aboutMenu = new Menu
                {
                    MenuName = "Giới thiệu",
                    IsActive = true,
                    ControllerName = "Home",
                    ActionName = "About",
                    Levels = 1,
                    ParentID = 0,
                    Link = "/Home/About",
                    MenuOrder = maxOrder + 1,
                    Position = 2 // Footer menu
                };

                // Thêm vào database
                _context.Menu.Add(aboutMenu);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm menu Giới thiệu vào footer.";
            }
            else
            {
                TempData["InfoMessage"] = "Menu Giới thiệu đã tồn tại trong footer.";
            }

            return RedirectToAction("Index", "SiteMenu");
        }
        
        // GET: /SiteMenu/AddFooterContactMenuItem
        public async Task<IActionResult> AddFooterContactMenuItem()
        {
            // Kiểm tra xem menu item Contact đã tồn tại trong footer chưa
            bool contactMenuExists = await _context.Menu
                .AnyAsync(m => m.ControllerName == "Home" && m.ActionName == "Contact" && m.Position == 2);

            if (!contactMenuExists)
            {
                // Xác định thứ tự lớn nhất hiện tại trong footer menu
                int maxOrder = await _context.Menu
                    .Where(m => m.Position == 2)
                    .Select(m => m.MenuOrder)
                    .DefaultIfEmpty(0)
                    .MaxAsync();
                
                // Tạo menu item Contact trong footer
                var contactMenu = new Menu
                {
                    MenuName = "Liên hệ",
                    IsActive = true,
                    ControllerName = "Home",
                    ActionName = "Contact",
                    Levels = 1,
                    ParentID = 0,
                    Link = "/Home/Contact",
                    MenuOrder = maxOrder + 1,
                    Position = 2 // Footer menu
                };

                // Thêm vào database
                _context.Menu.Add(contactMenu);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm menu Liên hệ vào footer.";
            }
            else
            {
                TempData["InfoMessage"] = "Menu Liên hệ đã tồn tại trong footer.";
            }

            return RedirectToAction("Index", "SiteMenu");
        }
    }
} 