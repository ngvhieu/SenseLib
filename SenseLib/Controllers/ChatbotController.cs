using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SenseLib.Models;
using SenseLib.Services;
using SenseLib.Utilities;

namespace SenseLib.Controllers
{
    [Authorize]
    public class ChatbotController : Controller
    {
        private readonly DataContext _context;
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            DataContext context,
            IChatbotService chatbotService,
            ILogger<ChatbotController> logger)
        {
            _context = context;
            _chatbotService = chatbotService;
            _logger = logger;
        }

        // GET: /Chatbot
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Chatbot/Document/5
        public async Task<IActionResult> Document(int id)
        {
            var document = await _context.Documents
                .Include(d => d.User)
                .Include(d => d.Category)
                .FirstOrDefaultAsync(d => d.DocumentID == id);

            if (document == null)
            {
                _logger.LogWarning($"Tài liệu không tồn tại: ID {id}");
                return NotFound();
            }

            // Kiểm tra xem người dùng có quyền truy cập tài liệu không
            if (document.IsPaid)
            {
                var currentUserId = int.Parse(User.Identity.Name);
                
                // Kiểm tra xem người dùng đã mua tài liệu chưa
                var purchased = await _context.Purchases
                    .AnyAsync(p => p.UserID == currentUserId && 
                                  p.DocumentID == id && 
                                  p.Status == "Completed");
                
                // Nếu tài liệu có phí và người dùng chưa mua
                if (!purchased && document.UserID != currentUserId)
                {
                    _logger.LogWarning($"Người dùng {currentUserId} cố gắng truy cập tài liệu có phí {id} mà chưa mua");
                    return RedirectToAction("Details", "Document", new { id = id });
                }
            }

            var model = new DocumentChatViewModel
            {
                Document = document,
                ChatHistory = new List<ChatMessage>()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskQuestion([FromBody] QuestionRequest request)
        {
            try
            {
                if (request == null || request.DocumentId <= 0 || string.IsNullOrEmpty(request.Question))
                {
                    _logger.LogWarning("Yêu cầu không hợp lệ: request null hoặc dữ liệu không đủ");
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
                }
                
                _logger.LogInformation($"Nhận câu hỏi cho tài liệu {request.DocumentId}: {request.Question}");
                
                // Lấy thông tin tài liệu
                var document = await _context.Documents.FindAsync(request.DocumentId);
                if (document == null)
                {
                    _logger.LogWarning($"Không tìm thấy tài liệu: {request.DocumentId}");
                    return NotFound(new { success = false, message = "Không tìm thấy tài liệu" });
                }
                
                // Kiểm tra quyền truy cập
                if (document.IsPaid)
                {
                    var currentUserId = int.Parse(User.Identity.Name);
                    
                    // Kiểm tra người dùng đã mua tài liệu chưa
                    var purchased = await _context.Purchases
                        .AnyAsync(p => p.UserID == currentUserId && 
                                      p.DocumentID == request.DocumentId && 
                                      p.Status == "Completed");
                    
                    // Nếu tài liệu có phí và người dùng chưa mua
                    if (!purchased && document.UserID != currentUserId)
                    {
                        _logger.LogWarning($"Người dùng {currentUserId} cố gắng hỏi đáp tài liệu có phí {request.DocumentId} mà chưa mua");
                        return Forbid();
                    }
                }

                // Trích xuất nội dung từ file
                string documentContent = "Không thể trích xuất nội dung.";
                if (!string.IsNullOrEmpty(document.FilePath))
                {
                    try
                    {
                        string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        string filePath = Path.Combine(rootPath, document.FilePath.TrimStart('/'));
                        
                        _logger.LogInformation($"Đường dẫn file: {filePath}");
                        
                        if (System.IO.File.Exists(filePath))
                        {
                            _logger.LogInformation($"Bắt đầu trích xuất nội dung từ {Path.GetFileName(filePath)}");
                            documentContent = await DocumentParser.ExtractTextAsync(filePath);
                            _logger.LogInformation($"Trích xuất thành công, độ dài: {documentContent.Length} ký tự");
                            
                            // Giới hạn độ dài nội dung nếu quá lớn
                            if (documentContent.Length > 100000)
                            {
                                _logger.LogWarning($"Nội dung quá dài ({documentContent.Length} ký tự), cắt bớt xuống 100000 ký tự");
                                documentContent = documentContent.Substring(0, 100000);
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"File không tồn tại: {filePath}");
                            documentContent = "Không tìm thấy file tài liệu.";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Lỗi khi trích xuất nội dung: {ex.Message}");
                        _logger.LogError($"Exception stack trace: {ex.StackTrace}");
                        
                        if (ex.InnerException != null)
                        {
                            _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                        }
                        
                        documentContent = "Lỗi khi trích xuất nội dung tài liệu.";
                    }
                }
                else
                {
                    _logger.LogWarning($"Tài liệu {request.DocumentId} không có đường dẫn file");
                }

                // Chuyển đổi lịch sử chat từ request
                List<ChatMessage> history = null;
                if (request.ChatHistory != null && request.ChatHistory.Count > 0)
                {
                    history = request.ChatHistory;
                    _logger.LogInformation($"Lịch sử chat: {request.ChatHistory.Count} tin nhắn");
                }

                // Gọi service chatbot để lấy câu trả lời
                _logger.LogInformation("Bắt đầu gọi ChatbotService");
                var response = await _chatbotService.AskQuestionAsync(documentContent, request.Question, history);
                _logger.LogInformation($"Kết quả từ ChatbotService: Success={response.Success}");
                
                return Json(new
                {
                    success = response.Success,
                    answer = response.Answer,
                    error = response.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi không xử lý được khi xử lý câu hỏi: {ex.Message}");
                _logger.LogError($"Exception stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                
                return StatusCode(500, new { 
                    success = false, 
                    message = "Lỗi máy chủ khi xử lý câu hỏi",
                    detail = ex.Message
                });
            }
        }
    }

    public class QuestionRequest
    {
        public int DocumentId { get; set; }
        public string Question { get; set; }
        public List<ChatMessage> ChatHistory { get; set; }
    }

    public class DocumentChatViewModel
    {
        public Document Document { get; set; }
        public List<ChatMessage> ChatHistory { get; set; }
    }
} 