using Microsoft.AspNetCore.Mvc;
using SenseLib.Services;
using System;
using System.Threading.Tasks;
using System.IO;
using SenseLib.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace SenseLib.Controllers
{
    public class TextToSpeechController : Controller
    {
        private readonly ILogger<TextToSpeechController> _logger;
        private readonly TextToSpeechService _ttsService;
        private readonly PdfService _pdfService;
        private readonly IWebHostEnvironment _environment;
        private readonly DataContext _context;
        private readonly IServiceProvider _serviceProvider;

        public TextToSpeechController(
            ILogger<TextToSpeechController> logger,
            TextToSpeechService ttsService,
            PdfService pdfService,
            IWebHostEnvironment environment,
            DataContext context,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _ttsService = ttsService;
            _pdfService = pdfService;
            _environment = environment;
            _context = context;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        [Route("text-to-speech")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Route("api/TextToSpeech/pdf-to-speech")]
        public async Task<IActionResult> ConvertPdfToSpeech(IFormFile pdfFile, 
            [FromForm] string languageCode = "vi-VN", 
            [FromForm] string voiceName = "vi-VN-Standard-A")
        {
            try
            {
                if (pdfFile == null || pdfFile.Length == 0)
                {
                    return BadRequest("Vui lòng tải lên file PDF");
                }

                // Kiểm tra định dạng file
                var fileExtension = Path.GetExtension(pdfFile.FileName).ToLowerInvariant();
                if (fileExtension != ".pdf")
                {
                    return BadRequest("Chỉ hỗ trợ file PDF");
                }

                // Tạo thư mục tạm để lưu file PDF
                var tempDirectory = Path.Combine(_environment.ContentRootPath, "temp");
                if (!Directory.Exists(tempDirectory))
                {
                    Directory.CreateDirectory(tempDirectory);
                }

                // Lưu file PDF tạm thời
                var uniqueFileName = $"{Guid.NewGuid():N}_{pdfFile.FileName}";
                var pdfPath = Path.Combine(tempDirectory, uniqueFileName);
                
                using (var fileStream = new FileStream(pdfPath, FileMode.Create))
                {
                    await pdfFile.CopyToAsync(fileStream);
                }

                // Đọc nội dung PDF
                var pdfText = _pdfService.ExtractTextFromPdf(pdfPath);
                
                if (string.IsNullOrWhiteSpace(pdfText))
                {
                    return BadRequest("Không thể đọc nội dung từ file PDF");
                }

                // Tạo thư mục đầu ra cho file âm thanh
                var outputDirectory = Path.Combine(_environment.WebRootPath, "audio", Guid.NewGuid().ToString());
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Chuyển đổi văn bản thành âm thanh
                var baseFileName = Path.GetFileNameWithoutExtension(pdfFile.FileName);
                var audioFiles = await _ttsService.SynthesizeLongTextAsync(
                    pdfText, outputDirectory, baseFileName, languageCode, voiceName);

                // Xoá file PDF tạm
                System.IO.File.Delete(pdfPath);

                // Đảm bảo thứ tự các file âm thanh
                audioFiles = SortAudioFiles(audioFiles);
                
                // Trả về kết quả
                return Ok(new
                {
                    Success = true,
                    Message = "Chuyển đổi thành công",
                    AudioFiles = audioFiles.Select(file => {
                        // Chuyển đường dẫn thành URL có thể truy cập
                        var relativePath = file.Replace(_environment.WebRootPath, "").Replace("\\", "/");
                        if (!relativePath.StartsWith("/")) relativePath = "/" + relativePath;
                        return relativePath;
                    }).ToList(),
                    TextContent = pdfText
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển đổi PDF sang âm thanh");
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu");
            }
        }

        [HttpGet]
        [Route("api/TextToSpeech/document/{id}")]
        public async Task<IActionResult> ConvertDocumentToSpeech(int id, string languageCode = "vi-VN", string voiceName = "vi-VN-Standard-A")
        {
            try
            {
                // Tìm tài liệu theo id
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.DocumentID == id);

                if (document == null)
                {
                    return NotFound("Không tìm thấy tài liệu");
                }

                // Kiểm tra đường dẫn file
                var filePath = document.FilePath;
                _logger.LogInformation("Đường dẫn file: {FilePath}", filePath);

                // Kiểm tra xem đường dẫn là tương đối hay tuyệt đối
                if (!Path.IsPathRooted(filePath))
                {
                    // Nếu là đường dẫn tương đối, thêm wwwroot
                    filePath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                }
                
                _logger.LogInformation("Đường dẫn đầy đủ: {FullPath}", filePath);

                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("File không tồn tại: {FilePath}", filePath);
                    
                    // Thử tìm file trong các thư mục uploads
                    var possibleLocations = new[]
                    {
                        filePath,
                        Path.Combine(_environment.WebRootPath, "uploads", "documents", Path.GetFileName(filePath)),
                        Path.Combine(_environment.ContentRootPath, filePath),
                        document.FilePath // Đường dẫn gốc từ database
                    };

                    foreach (var location in possibleLocations)
                    {
                        if (!string.IsNullOrEmpty(location) && System.IO.File.Exists(location))
                        {
                            filePath = location;
                            _logger.LogInformation("Đã tìm thấy file tại: {FilePath}", filePath);
                            break;
                        }
                    }

                    if (!System.IO.File.Exists(filePath))
                    {
                        return BadRequest($"File không tồn tại. DocumentID: {document.DocumentID}, FilePath: {document.FilePath}");
                    }
                }

                // Kiểm tra định dạng file
                var fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
                if (fileExtension != ".pdf")
                {
                    // Với file docx, doc, txt, hãy thử chuyển đổi sang PDF trước
                    if (fileExtension == ".docx" || fileExtension == ".doc" || fileExtension == ".txt")
                    {
                        _logger.LogInformation("File không phải PDF, đang thử chuyển đổi từ {FileType}", fileExtension);
                        
                        // Tạo thư mục tạm
                        var tempDirectory = Path.Combine(_environment.ContentRootPath, "temp");
                        if (!Directory.Exists(tempDirectory))
                        {
                            Directory.CreateDirectory(tempDirectory);
                        }
                        
                        var tempPdfPath = Path.Combine(tempDirectory, $"{Guid.NewGuid():N}.pdf");
                        bool conversionSuccess = false;
                        
                        try
                        {
                            // Lấy DocumentConverterService từ ServiceProvider
                            var converter = _serviceProvider.GetService<IDocumentConverterService>();
                            
                            if (converter != null)
                            {
                                // ConvertToPdfAsync trả về đường dẫn file PDF, không phải boolean
                                string pdfPath = await converter.ConvertToPdfAsync(filePath, document.DocumentID);
                                
                                if (!string.IsNullOrEmpty(pdfPath))
                                {
                                    // Chuyển đường dẫn tương đối thành đường dẫn đầy đủ
                                    var convertedPdfPath = Path.Combine(_environment.WebRootPath, pdfPath.TrimStart('/'));
                                    if (System.IO.File.Exists(convertedPdfPath))
                                    {
                                        filePath = convertedPdfPath;
                                        _logger.LogInformation("Đã tìm thấy file PDF tại: {FilePath}", filePath);
                                        conversionSuccess = true;
                                    }
                                }
                            }
                            
                            if (!conversionSuccess)
                            {
                                return BadRequest($"Không thể chuyển đổi file {fileExtension} sang PDF");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi chuyển đổi file {FileType} sang PDF", fileExtension);
                        }
                    }
                    else
                    {
                        return BadRequest($"Chỉ hỗ trợ file PDF, DOCX, DOC, TXT. File hiện tại: {fileExtension}");
                    }
                }

                // Đọc nội dung PDF
                var pdfText = _pdfService.ExtractTextFromPdf(filePath);
                
                if (string.IsNullOrWhiteSpace(pdfText))
                {
                    return BadRequest("Không thể đọc nội dung từ file PDF");
                }

                // Tạo thư mục đầu ra cho file âm thanh
                var outputDirectory = Path.Combine(_environment.WebRootPath, "audio", Guid.NewGuid().ToString());
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                // Chuyển đổi văn bản thành âm thanh
                var baseFileName = Path.GetFileNameWithoutExtension(filePath);
                var audioFiles = await _ttsService.SynthesizeLongTextAsync(
                    pdfText, outputDirectory, baseFileName, languageCode, voiceName);

                // Đảm bảo thứ tự các file âm thanh
                audioFiles = SortAudioFiles(audioFiles);
                
                // Trả về kết quả
                return Ok(new
                {
                    Success = true,
                    Message = "Chuyển đổi thành công",
                    DocumentId = id,
                    DocumentTitle = document.Title,
                    AudioFiles = audioFiles.Select(file => {
                        // Chuyển đường dẫn thành URL có thể truy cập
                        var relativePath = file.Replace(_environment.WebRootPath, "").Replace("\\", "/");
                        if (!relativePath.StartsWith("/")) relativePath = "/" + relativePath;
                        return relativePath;
                    }).ToList(),
                    TextContent = pdfText
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển đổi tài liệu {DocumentId} sang âm thanh", id);
                return StatusCode(500, "Đã xảy ra lỗi khi xử lý yêu cầu");
            }
        }
        
        /// <summary>
        /// Sắp xếp các file âm thanh theo thứ tự đúng
        /// </summary>
        private List<string> SortAudioFiles(List<string> audioFiles)
        {
            // Sắp xếp file theo thứ tự số trong tên file (baseFileName_1.mp3, baseFileName_2.mp3, ...)
            return audioFiles.OrderBy(file => {
                try {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var parts = fileName.Split('_');
                    if (parts.Length > 0)
                    {
                        // Lấy phần số cuối cùng trong tên file
                        var lastPart = parts[parts.Length - 1];
                        if (int.TryParse(lastPart, out int index))
                        {
                            return index;
                        }
                    }
                    return 999; // Nếu không phân tích được số, đặt ưu tiên thấp
                }
                catch {
                    return 999; 
                }
            }).ToList();
        }
    }
} 