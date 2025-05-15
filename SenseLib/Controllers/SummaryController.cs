using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SenseLib.Models;
using SenseLib.Services;
using System.Text;

namespace SenseLib.Controllers
{
    [Authorize]
    public class SummaryController : Controller
    {
        private readonly ISummaryService _summaryService;
        private readonly DataContext _context;
        private readonly ILogger<SummaryController> _logger;
        private readonly int _maxContentLength = 12000; // Khoảng 3000 tokens

        public SummaryController(ISummaryService summaryService, DataContext context, ILogger<SummaryController> logger)
        {
            _summaryService = summaryService;
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SummarizeDocument(int documentId)
        {
            try
            {
                _logger.LogInformation($"Bắt đầu tóm tắt tài liệu ID: {documentId}");
                
                // Lấy thông tin tài liệu từ database
                var document = await _context.Documents.FindAsync(documentId);
                if (document == null)
                {
                    _logger.LogWarning($"Không tìm thấy tài liệu với ID: {documentId}");
                    return Json(new { success = false, message = "Không tìm thấy tài liệu." });
                }

                // Trích xuất nội dung từ file tài liệu
                string content = "Không thể trích xuất nội dung tài liệu.";

                try
                {
                    if (!string.IsNullOrEmpty(document.FilePath))
                    {
                        // Xác định đường dẫn đầy đủ đến file
                        string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        string filePath = Path.Combine(rootPath, document.FilePath.TrimStart('/'));
                        
                        _logger.LogInformation($"Đường dẫn tài liệu: {filePath}");
                        
                        if (System.IO.File.Exists(filePath))
                        {
                            // Sử dụng DocumentParser để trích xuất nội dung từ file
                            content = await Utilities.DocumentParser.ExtractTextAsync(filePath);
                            _logger.LogInformation($"Đã trích xuất nội dung từ file: {filePath}, độ dài: {content.Length} ký tự");
                        }
                        else
                        {
                            _logger.LogWarning($"Không tìm thấy file: {filePath}");
                            content = $"Không tìm thấy file tài liệu: {document.FilePath}";
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Tài liệu ID {documentId} không có đường dẫn file");
                        content = "Tài liệu không có nội dung.";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi khi đọc nội dung tài liệu: {ex.Message}", ex);
                    content = $"Có lỗi xảy ra khi đọc nội dung tài liệu: {ex.Message}";
                }

                // Giới hạn độ dài nội dung để tránh vượt quá token limit
                string limitedContent = LimitContentLength(content, _maxContentLength);
                _logger.LogInformation($"Nội dung sau khi giới hạn: {limitedContent.Length} ký tự");

                try
                {
                    // Tóm tắt nội dung
                    var summary = await _summaryService.SummarizeTextAsync(limitedContent);
                    _logger.LogInformation($"Tóm tắt thành công, độ dài kết quả: {summary.Length} ký tự");
                    
                    return Json(new { success = true, summary });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi trong quá trình tóm tắt: {ex.Message}", ex);
                    return Json(new { 
                        success = false, 
                        message = $"Lỗi khi tóm tắt: {ex.Message}",
                        errorDetails = ex.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi không xử lý được trong SummarizeDocument: {ex.Message}", ex);
                return Json(new { 
                    success = false, 
                    message = "Đã xảy ra lỗi khi tóm tắt tài liệu. Vui lòng thử lại sau."
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SummarizeText([FromBody] SummaryRequest request)
        {
            try
            {
                _logger.LogInformation("Bắt đầu tóm tắt văn bản");
                
                if (string.IsNullOrEmpty(request?.Text))
                {
                    _logger.LogWarning("Yêu cầu tóm tắt không có nội dung");
                    return Json(new { success = false, message = "Không có nội dung để tóm tắt." });
                }

                // Giới hạn độ dài nội dung
                string content = LimitContentLength(request.Text, _maxContentLength);
                _logger.LogInformation($"Nội dung sau khi giới hạn: {content.Length} ký tự");

                try
                {
                    var summary = await _summaryService.SummarizeTextAsync(content, request.MaxLength ?? 500);
                    _logger.LogInformation($"Tóm tắt thành công, độ dài kết quả: {summary.Length} ký tự");
                    
                    return Json(new { success = true, summary });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi trong quá trình tóm tắt văn bản: {ex.Message}", ex);
                    return Json(new { 
                        success = false, 
                        message = $"Lỗi khi tóm tắt: {ex.Message}",
                        errorDetails = ex.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi không xử lý được trong SummarizeText: {ex.Message}", ex);
                return Json(new { 
                    success = false, 
                    message = "Đã xảy ra lỗi khi tóm tắt văn bản. Vui lòng thử lại sau."
                });
            }
        }

        /// <summary>
        /// Giới hạn độ dài nội dung, giữ lại phần đầu và phần cuối nếu vượt quá
        /// </summary>
        private string LimitContentLength(string content, int maxLength)
        {
            if (string.IsNullOrEmpty(content) || content.Length <= maxLength)
            {
                return content;
            }

            // Lấy phần đầu và phần cuối, thêm thông báo ở giữa
            int partLength = maxLength / 2;
            
            StringBuilder result = new StringBuilder(maxLength + 100);
            result.Append(content.Substring(0, partLength));
            result.Append("\n\n[...Nội dung quá dài đã được cắt bớt...]\n\n");
            result.Append(content.Substring(content.Length - partLength));
            
            return result.ToString();
        }
    }

    public class SummaryRequest
    {
        public string Text { get; set; }
        public int? MaxLength { get; set; }
    }
} 