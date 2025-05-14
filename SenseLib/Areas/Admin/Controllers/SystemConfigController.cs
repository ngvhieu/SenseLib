using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SenseLib.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SystemConfigController : Controller
    {
        private readonly DataContext _context;

        public SystemConfigController(DataContext context)
        {
            _context = context;
        }

        // GET: Admin/SystemConfig
        public async Task<IActionResult> Index()
        {
            var configs = await _context.SystemConfigs.OrderBy(c => c.ConfigKey).ToListAsync();
            return View(configs);
        }

        // GET: Admin/SystemConfig/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var config = await _context.SystemConfigs.FindAsync(id);
            if (config == null)
            {
                return NotFound();
            }
            
            return View(config);
        }

        // POST: Admin/SystemConfig/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ConfigID,ConfigKey,ConfigValue,Description")] SystemConfig config)
        {
            if (id != config.ConfigID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(config);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật cấu hình thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ConfigExists(config.ConfigID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(config);
        }

        // GET: Admin/SystemConfig/Create
        public IActionResult Create()
        {
            return View(new SystemConfig());
        }

        // POST: Admin/SystemConfig/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ConfigKey,ConfigValue,Description")] SystemConfig config)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra xem key đã tồn tại chưa
                if (await _context.SystemConfigs.AnyAsync(c => c.ConfigKey == config.ConfigKey))
                {
                    ModelState.AddModelError("ConfigKey", "Khóa cấu hình này đã tồn tại");
                    return View(config);
                }
                
                _context.Add(config);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm cấu hình thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(config);
        }

        // GET: Admin/SystemConfig/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var config = await _context.SystemConfigs.FirstOrDefaultAsync(m => m.ConfigID == id);
            if (config == null)
            {
                return NotFound();
            }

            return View(config);
        }

        // POST: Admin/SystemConfig/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var config = await _context.SystemConfigs.FindAsync(id);
            
            // Không cho phép xóa các cấu hình mặc định quan trọng
            var essentialConfigs = new[] { "MaxLoginAttempts", "LockoutTimeMinutes", "HomePagePaidDocuments", "HomePageFreeDocuments" };
            if (essentialConfigs.Contains(config.ConfigKey))
            {
                TempData["ErrorMessage"] = "Không thể xóa cấu hình hệ thống quan trọng này!";
                return RedirectToAction(nameof(Index));
            }
            
            _context.SystemConfigs.Remove(config);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa cấu hình thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/SystemConfig/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var config = await _context.SystemConfigs
                .FirstOrDefaultAsync(m => m.ConfigID == id);
            if (config == null)
            {
                return NotFound();
            }

            return View(config);
        }

        private bool ConfigExists(int id)
        {
            return _context.SystemConfigs.Any(e => e.ConfigID == id);
        }
    }
} 