using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using SenseLib.Services;

namespace SenseLib.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DataContext _context;

    public HomeController(
        ILogger<HomeController> logger, 
        DataContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Lấy cấu hình số lượng tài liệu hiển thị từ database
        int paidDocumentsCount = 4; // Giá trị mặc định
        int freeDocumentsCount = 4; // Giá trị mặc định
        
        // Tìm cấu hình từ database
        var paidDocsConfig = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "HomePagePaidDocuments");
        var freeDocsConfig = await _context.SystemConfigs.FirstOrDefaultAsync(c => c.ConfigKey == "HomePageFreeDocuments");
        
        // Nếu cấu hình tồn tại, cập nhật giá trị
        if (paidDocsConfig != null && int.TryParse(paidDocsConfig.ConfigValue, out int configPaidDocs))
        {
            paidDocumentsCount = configPaidDocs;
        }
        
        if (freeDocsConfig != null && int.TryParse(freeDocsConfig.ConfigValue, out int configFreeDocs))
        {
            freeDocumentsCount = configFreeDocs;
        }
        
        // Lấy tài liệu có phí mới nhất theo cấu hình
        var paidDocuments = await _context.Documents
            .Where(d => (d.Status == "Approved" || d.Status == "Published") && d.IsPaid)
            .OrderByDescending(d => d.UploadDate)
            .Include(d => d.Author)
            .Include(d => d.Category)
            .Take(paidDocumentsCount)
            .ToListAsync();
            
        // Lấy tài liệu miễn phí mới nhất theo cấu hình
        var freeDocuments = await _context.Documents
            .Where(d => (d.Status == "Approved" || d.Status == "Published") && !d.IsPaid)
            .OrderByDescending(d => d.UploadDate)
            .Include(d => d.Author)
            .Include(d => d.Category)
            .Take(freeDocumentsCount)
            .ToListAsync();
            
        // Kết hợp cả hai danh sách
        var featuredDocuments = paidDocuments.Concat(freeDocuments).ToList();
            
        // Thống kê cho trang chủ
        ViewBag.DocumentsCount = await _context.Documents.CountAsync(d => d.Status == "Approved" || d.Status == "Published");
        ViewBag.UsersCount = await _context.Users.CountAsync();
        ViewBag.DownloadsCount = await _context.Downloads.CountAsync();
        ViewBag.CategoriesCount = await _context.Categories.CountAsync();

        return View(featuredDocuments);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
