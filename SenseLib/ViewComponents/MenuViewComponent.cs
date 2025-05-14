using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SenseLib.ViewComponents
{
    public class MenuViewComponent : ViewComponent
    {
        private readonly DataContext _context;

        public MenuViewComponent(DataContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int position = 1)
        {
            // Lấy menu theo position (1: Main menu, 2: Footer menu, v.v.)
            var items = await GetItemsAsync(position);
            
            // Debug info
            foreach(var item in items)
            {
                System.Console.WriteLine($"Menu: {item.MenuID} - {item.MenuName} - {item.ControllerName} - {item.ActionName}");
            }
            
            return View(items);
        }

        private Task<List<Menu>> GetItemsAsync(int position)
        {
            return _context.Menu
                .Where(m => m.IsActive && m.Position == position)
                .OrderBy(m => m.Levels)
                .ThenBy(m => m.MenuOrder)
                .ToListAsync();
        }
    }
} 