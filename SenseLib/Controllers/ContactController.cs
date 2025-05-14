using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using SenseLib.Services;
using System;
using System.Threading.Tasks;

namespace SenseLib.Controllers
{
    public class ContactController : Controller
    {
        private readonly DataContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(
            DataContext context, 
            IEmailService emailService,
            ILogger<ContactController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View(new ContactMessage());
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactMessage model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Lưu thông tin liên hệ vào cơ sở dữ liệu
                    model.CreatedAt = DateTime.Now;
                    model.IsRead = false;
                    
                    _context.ContactMessages.Add(model);
                    await _context.SaveChangesAsync();
                    
                    // Gửi email thông báo đến admin
                    await _emailService.SendContactNotificationAsync(
                        model.Name,
                        model.Email,
                        model.Subject,
                        model.Message
                    );
                    
                    // Gửi email xác nhận cho người dùng
                    string confirmationMessage = $@"
                        <h2>Xin chào {model.Name},</h2>
                        <p>Cảm ơn bạn đã liên hệ với SenseLib. Chúng tôi đã nhận được thông tin liên hệ của bạn và sẽ phản hồi trong thời gian sớm nhất.</p>
                        <p><strong>Tiêu đề:</strong> {model.Subject}</p>
                        <p><strong>Nội dung:</strong></p>
                        <div style='padding: 15px; border-left: 4px solid #ccc;'>
                            {model.Message.Replace(Environment.NewLine, "<br />")}
                        </div>
                        <p>Đây là email tự động, vui lòng không trả lời email này.</p>
                        <p>Trân trọng,<br>Đội ngũ SenseLib</p>
                    ";
                    
                    await _emailService.SendEmailAsync(model.Email, "Đã nhận thông tin liên hệ của bạn", confirmationMessage);
                    
                    TempData["SuccessMessage"] = "Cảm ơn bạn đã liên hệ với chúng tôi! Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý form liên hệ");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi gửi thông tin. Vui lòng thử lại sau.");
                }
            }
            
            return View(model);
        }
    }
} 