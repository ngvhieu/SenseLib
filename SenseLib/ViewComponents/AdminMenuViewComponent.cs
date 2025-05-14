using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SenseLib.ViewComponents
{
    public class AdminMenuViewComponent : ViewComponent
    {
        private readonly DataContext _context;

        public AdminMenuViewComponent(DataContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var menuItems = await GetAdminMenuItemsAsync();
            
            // Debug info
            foreach(var item in menuItems)
            {
                System.Console.WriteLine($"AdminMenu: {item.MenuID} - {item.MenuName} - {item.ControllerName} - {item.ActionName}");
            }
            
            return View(menuItems);
        }

        private async Task<List<AdminMenu>> GetAdminMenuItemsAsync()
        {
            return await _context.AdminMenus
                .Where(m => m.IsActive)
                .OrderBy(m => m.ItemLevel)
                .ThenBy(m => m.ItemOrder)
                .ToListAsync();
        }
    }
} 