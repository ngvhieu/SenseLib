using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SenseLib.Models;
using SenseLib.Services;
using System.Threading.Tasks;

namespace SenseLib.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ContactController : Controller
    {
        private readonly DataContext _context;
        private readonly IEmailService _emailService;

        public ContactController(DataContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Admin/Contact
        public async Task<IActionResult> Index()
        {
            var messages = await _context.ContactMessages
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            
            return View(messages);
        }

        // GET: Admin/Contact/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.ContactMessages
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (message == null)
            {
                return NotFound();
            }

            // Đánh dấu là đã đọc nếu chưa đọc
            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        // POST: Admin/Contact/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            
            if (message != null)
            {
                _context.ContactMessages.Remove(message);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Contact/Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string replyMessage)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] = "ID không hợp lệ";
                return RedirectToAction(nameof(Index));
            }

            var contactMessage = await _context.ContactMessages.FindAsync(id);
            
            if (contactMessage == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tin nhắn liên hệ";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(replyMessage))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập nội dung phản hồi";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                // Gửi email phản hồi
                string htmlMessage = $@"
                    <h2>Xin chào {contactMessage.Name},</h2>
                    <p>Cảm ơn bạn đã liên hệ với SenseLib. Dưới đây là phản hồi cho yêu cầu của bạn:</p>
                    <p><strong>Tiêu đề ban đầu:</strong> {contactMessage.Subject}</p>
                    <p><strong>Nội dung ban đầu:</strong></p>
                    <div style='padding: 15px; border-left: 4px solid #ccc; margin-bottom: 20px;'>
                        {contactMessage.Message.Replace(Environment.NewLine, "<br />")}
                    </div>
                    <p><strong>Phản hồi của chúng tôi:</strong></p>
                    <div style='padding: 15px; border-left: 4px solid #0d6efd;'>
                        {replyMessage.Replace(Environment.NewLine, "<br />")}
                    </div>
                    <p>Nếu bạn cần thêm thông tin hoặc có thắc mắc khác, đừng ngại liên hệ lại với chúng tôi.</p>
                    <p>Trân trọng,<br>Đội ngũ SenseLib</p>
                ";

                await _emailService.SendEmailAsync(
                    contactMessage.Email,
                    $"Phản hồi: {contactMessage.Subject}",
                    htmlMessage
                );

                // Cập nhật trạng thái đã đọc cho tin nhắn
                if (!contactMessage.IsRead)
                {
                    contactMessage.IsRead = true;
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Đã gửi phản hồi thành công";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi gửi phản hồi: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
} 