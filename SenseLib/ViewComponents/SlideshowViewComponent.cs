using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SenseLib.ViewComponents
{
    public class SlideshowViewComponent : ViewComponent
    {
        private readonly DataContext _context;

        public SlideshowViewComponent(DataContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var slideItems = await _context.Slideshows
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .Take(5)
                .ToListAsync();
            
            return View(slideItems);
        }
    }
} 