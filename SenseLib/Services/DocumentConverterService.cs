using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using SenseLib.Models;
using System.Collections.Generic;
using iText.Layout.Element;
using iText.Layout;
using iText.Kernel.Pdf;
using iText.IO.Image;
using Markdig;
using HtmlAgilityPack;

namespace SenseLib.Services
{
    public interface IDocumentConverterService
    {
        Task<string> ConvertToPdfAsync(string sourcePath, int documentId);
        bool IsPdfAvailable(string sourcePath, int documentId);
        string GetPdfPath(string sourcePath, int documentId);
    }

    public class DocumentConverterService : IDocumentConverterService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IDocxService _docxService;
        private readonly ILogger<DocumentConverterService> _logger;

        public DocumentConverterService(
            IWebHostEnvironment environment,
            IDocxService docxService,
            ILogger<DocumentConverterService> logger)
        {
            _environment = environment;
            _docxService = docxService;
            _logger = logger;
        }

        public async Task<string> ConvertToPdfAsync(string sourcePath, int documentId)
        {
            try
            {
                string fullSourcePath = Path.Combine(_environment.WebRootPath, sourcePath.TrimStart('/'));
                
                if (!File.Exists(fullSourcePath))
                {
                    _logger.LogError($"Tệp nguồn không tồn tại: {fullSourcePath}");
                    return null;
                }

                // Tạo thư mục PDF nếu chưa tồn tại
                string pdfDirectory = Path.Combine(_environment.WebRootPath, "uploads", "pdf");
                if (!Directory.Exists(pdfDirectory))
                {
                    Directory.CreateDirectory(pdfDirectory);
                }

                string fileName = Path.GetFileNameWithoutExtension(fullSourcePath);
                string pdfFileName = $"{documentId}_{fileName}.pdf";
                string pdfPath = Path.Combine(pdfDirectory, pdfFileName);

                // Kiểm tra nếu file PDF đã tồn tại
                if (File.Exists(pdfPath))
                {
                    return $"/uploads/pdf/{pdfFileName}";
                }

                // Lấy phần mở rộng của file nguồn
                string extension = Path.GetExtension(fullSourcePath).ToLower();

                // Chuyển đổi file sang PDF dựa vào định dạng
                bool conversionSuccess = false;

                switch (extension)
                {
                    case ".pdf":
                        // Nếu là PDF thì chỉ cần copy
                        File.Copy(fullSourcePath, pdfPath, true);
                        conversionSuccess = true;
                        break;

                    case ".docx":
                    case ".doc":
                    case ".rtf":
                    case ".odt":
                        conversionSuccess = await ConvertWordToPdfAsync(fullSourcePath, pdfPath);
                        break;

                    case ".pptx":
                    case ".ppt":
                    case ".odp":
                        conversionSuccess = await ConvertPowerPointToPdfAsync(fullSourcePath, pdfPath);
                        break;

                    case ".xlsx":
                    case ".xls":
                    case ".csv":
                    case ".ods":
                        conversionSuccess = await ConvertExcelToPdfAsync(fullSourcePath, pdfPath);
                        break;
                        
                    case ".txt":
                    case ".md":
                    case ".html":
                    case ".htm":
                    case ".xml":
                    case ".json":
                    case ".log":
                        conversionSuccess = await ConvertTextToPdfAsync(fullSourcePath, pdfPath);
                        break;
                        
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".gif":
                    case ".bmp":
                    case ".svg":
                        conversionSuccess = await ConvertImageToPdfAsync(fullSourcePath, pdfPath);
                        break;

                    // Các định dạng khác không thể chuyển đổi sang PDF
                    case ".zip":
                    case ".rar":
                    case ".7z":
                    case ".mp3":
                    case ".mp4":
                    case ".avi":
                        // Đối với các định dạng này, chỉ lưu thông tin không chuyển đổi
                        _logger.LogInformation($"File {extension} không được hỗ trợ chuyển đổi sang PDF");
                        return null;

                    default:
                        _logger.LogWarning($"Không hỗ trợ chuyển đổi định dạng {extension} sang PDF");
                        return null;
                }

                if (conversionSuccess && File.Exists(pdfPath))
                {
                    return $"/uploads/pdf/{pdfFileName}";
                }
                else
                {
                    _logger.LogError($"Chuyển đổi thất bại: {fullSourcePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi chuyển đổi tài liệu sang PDF: {ex.Message}");
                return null;
            }
        }

        public bool IsPdfAvailable(string sourcePath, int documentId)
        {
            string pdfPath = GetPdfPath(sourcePath, documentId);
            string fullPdfPath = Path.Combine(_environment.WebRootPath, pdfPath.TrimStart('/'));
            return File.Exists(fullPdfPath);
        }

        public string GetPdfPath(string sourcePath, int documentId)
        {
            string fileName = Path.GetFileNameWithoutExtension(sourcePath);
            string pdfFileName = $"{documentId}_{fileName}.pdf";
            return $"/uploads/pdf/{pdfFileName}";
        }

        #region Private Methods

        private async Task<bool> ConvertWordToPdfAsync(string inputPath, string outputPath)
        {
            try
            {
                // Phương pháp 1: Sử dụng LibreOffice (nếu có)
                bool result = await ConvertUsingLibreOfficeAsync(inputPath, outputPath);
                if (result) return true;
                
                // Phương pháp 2: Sử dụng iText7 để tạo PDF đơn giản
                _logger.LogInformation($"LibreOffice không khả dụng, đang dùng iText7 để tạo PDF từ file: {inputPath}");
                
                try
                {
                    // Đọc nội dung file (chỉ hoạt động với định dạng văn bản đơn giản)
                    string text = "Không thể đọc nội dung file này. Vui lòng tải LibreOffice để xem tốt hơn.";
                    
                    // Nếu là .txt thì có thể đọc trực tiếp
                    if (Path.GetExtension(inputPath).ToLower() == ".txt")
                    {
                        text = await File.ReadAllTextAsync(inputPath);
                    }
                    
                    // Tạo PDF đơn giản với thông báo
                    using (var stream = new FileStream(outputPath, FileMode.Create))
                    {
                        using (var writer = new PdfWriter(stream))
                        {
                            using (var pdf = new PdfDocument(writer))
                            {
                                var document = new iText.Layout.Document(pdf);
                                
                                // Thêm tiêu đề
                                var title = new Paragraph("Nội dung tài liệu")
                                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                                    .SetFontSize(16);
                                document.Add(title);
                                
                                // Thêm thông báo
                                var note = new Paragraph("Đây là bản xem đơn giản vì LibreOffice không được cài đặt")
                                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                                    .SetFontSize(12);
                                document.Add(note);
                                
                                // Thêm nội dung
                                var content = new Paragraph(text);
                                document.Add(content);
                                
                                document.Close();
                            }
                        }
                    }
                    
                    return File.Exists(outputPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi tạo PDF từ file với iText7: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi chuyển đổi Word sang PDF: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ConvertExcelToPdfAsync(string inputPath, string outputPath)
        {
            try
            {
                // Phương pháp 1: Sử dụng LibreOffice (nếu có)
                bool result = await ConvertUsingLibreOfficeAsync(inputPath, outputPath);
                if (result) return true;
                
                // Phương pháp 2: Sử dụng iText7 để tạo PDF thông báo đơn giản
                _logger.LogInformation($"LibreOffice không khả dụng, đang tạo PDF thông báo cho file Excel: {inputPath}");
                
                try
                {
                    // Tạo PDF đơn giản với thông báo
                    using (var stream = new FileStream(outputPath, FileMode.Create))
                    {
                        using (var writer = new PdfWriter(stream))
                        {
                            using (var pdf = new PdfDocument(writer))
                            {
                                var document = new iText.Layout.Document(pdf);
                                
                                // Thêm tiêu đề
                                var title = new Paragraph("Không thể hiển thị file Excel")
                                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                                    .SetFontSize(16);
                                document.Add(title);
                                
                                // Thêm thông báo
                                var note = new Paragraph("Để xem file Excel, vui lòng cài đặt LibreOffice hoặc tải file gốc")
                                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                                    .SetFontSize(12);
                                document.Add(note);
                                
                                // Thêm tên file
                                var fileName = new Paragraph($"Tên file: {Path.GetFileName(inputPath)}")
                                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                                document.Add(fileName);
                                
                                document.Close();
                            }
                        }
                    }
                    
                    return File.Exists(outputPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi tạo PDF thông báo cho Excel: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi chuyển đổi Excel sang PDF: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ConvertPowerPointToPdfAsync(string inputPath, string outputPath)
        {
            try
            {
                // Phương pháp 1: Sử dụng LibreOffice (nếu có)
                bool result = await ConvertUsingLibreOfficeAsync(inputPath, outputPath);
                if (result) return true;
                
                // Phương pháp 2: Sử dụng iText7 để tạo PDF thông báo đơn giản
                _logger.LogInformation($"LibreOffice không khả dụng, đang tạo PDF thông báo cho file PowerPoint: {inputPath}");
                
                try
                {
                    // Tạo PDF đơn giản với thông báo
                    using (var stream = new FileStream(outputPath, FileMode.Create))
                    {
                        using (var writer = new PdfWriter(stream))
                        {
                            using (var pdf = new PdfDocument(writer))
                            {
                                var document = new iText.Layout.Document(pdf);
                                
                                // Thêm tiêu đề
                                var title = new Paragraph("Không thể hiển thị file PowerPoint")
                                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                                    .SetFontSize(16);
                                document.Add(title);
                                
                                // Thêm thông báo
                                var note = new Paragraph("Để xem file PowerPoint, vui lòng cài đặt LibreOffice hoặc tải file gốc")
                                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                                    .SetFontSize(12);
                                document.Add(note);
                                
                                // Thêm tên file
                                var fileName = new Paragraph($"Tên file: {Path.GetFileName(inputPath)}")
                                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);
                                document.Add(fileName);
                                
                                document.Close();
                            }
                        }
                    }
                    
                    return File.Exists(outputPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi khi tạo PDF thông báo cho PowerPoint: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi chuyển đổi PowerPoint sang PDF: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ConvertUsingLibreOfficeAsync(string inputPath, string outputPath)
        {
            try
            {
                // Kiểm tra các đường dẫn phổ biến cho LibreOffice/OpenOffice
                var possiblePaths = new List<string>
                {
                    @"C:\Program Files\LibreOffice\program\soffice.exe",
                    @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
                    @"C:\Program Files\OpenOffice\program\soffice.exe",
                    @"C:\Program Files (x86)\OpenOffice\program\soffice.exe",   
                    @"C:\Program Files\LibreOffice\program\soffice.exe"
                };

                string soffice = null;
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        soffice = path;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(soffice))
                {
                    _logger.LogWarning("Không tìm thấy LibreOffice/OpenOffice để chuyển đổi tài liệu");
                    // Không tìm thấy LibreOffice, nhưng không return false ở đây để có thể dùng phương pháp khác
                    _logger.LogInformation("Sẽ thử sử dụng phương pháp thay thế");
                    return false;
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = soffice,
                        Arguments = $"--headless --convert-to pdf --outdir \"{Path.GetDirectoryName(outputPath)}\" \"{inputPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _logger.LogInformation($"Thực thi lệnh: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
                
                process.Start();
                
                // Đọc output và error để tránh deadlock
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();
                
                // Log kết quả
                if (!string.IsNullOrEmpty(output))
                    _logger.LogInformation($"Output: {output}");
                
                if (!string.IsNullOrEmpty(error))
                    _logger.LogError($"Error: {error}");

                // LibreOffice tạo file với tên gốc nhưng phần mở rộng là .pdf
                string tempOutputPath = Path.Combine(
                    Path.GetDirectoryName(outputPath),
                    Path.GetFileNameWithoutExtension(inputPath) + ".pdf");

                // Kiểm tra file tạm có tồn tại không
                if (File.Exists(tempOutputPath))
                {
                    // Nếu file đích đã tồn tại, xóa nó
                    if (File.Exists(outputPath))
                    {
                        File.Delete(outputPath);
                    }
                    
                    // Di chuyển từ file tạm sang file đích
                    File.Move(tempOutputPath, outputPath);
                    
                    _logger.LogInformation($"Chuyển đổi thành công: {inputPath} -> {outputPath}");
                    return true;
                }
                else
                {
                    _logger.LogError($"File tạm không được tạo: {tempOutputPath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi dùng LibreOffice: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ConvertTextToPdfAsync(string inputPath, string outputPath)
        {
            try
            {
                // Thử sử dụng LibreOffice trước
                bool result = await ConvertUsingLibreOfficeAsync(inputPath, outputPath);
                if (result) return true;
                
                _logger.LogInformation($"Đang chuyển đổi file text sang PDF: {inputPath}");
                
                // Lấy nội dung file
                string text = await File.ReadAllTextAsync(inputPath);
                string extension = Path.GetExtension(inputPath).ToLower();
                
                // Sử dụng iText7 để tạo PDF
                using (var stream = new FileStream(outputPath, FileMode.Create))
                {
                    using (var writer = new PdfWriter(stream))
                    {
                        using (var pdf = new PdfDocument(writer))
                        {
                            var document = new iText.Layout.Document(pdf);
                            
                            // Thêm tiêu đề
                            var title = new Paragraph("File Content")
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                                .SetFontSize(14);
                            document.Add(title);
                            
                            // Xử lý nội dung dựa trên định dạng file
                            switch (extension)
                            {
                                case ".md":
                                    // Chuyển Markdown sang HTML sử dụng thư viện Markdig
                                    var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                                    string html = Markdown.ToHtml(text, pipeline);
                                    
                                    // Chuyển HTML sang PDF 
                                    {
                                        var htmlDoc = new HtmlDocument();
                                        htmlDoc.LoadHtml(html);
                                        string plainText = htmlDoc.DocumentNode.InnerText;
                                        var mdParagraph = new Paragraph(plainText);
                                        document.Add(mdParagraph);
                                    }
                                    break;
                                    
                                case ".html":
                                case ".htm":
                                    // Chuyển HTML sang Text và thêm vào PDF
                                    {
                                        var htmlDoc = new HtmlDocument();
                                        htmlDoc.LoadHtml(text);
                                        string plainText = htmlDoc.DocumentNode.InnerText;
                                        var htmlParagraph = new Paragraph(plainText);
                                        document.Add(htmlParagraph);
                                    }
                                    break;
                                    
                                case ".xml":
                                case ".json":
                                    // Định dạng hiển thị JSON/XML
                                    var codeText = new Paragraph(text)
                                        .SetFontFamily("Courier");
                                    document.Add(codeText);
                                    break;
                                    
                                default: // .txt, .log, etc
                                    // Thêm nội dung trực tiếp
                                    var textParagraph = new Paragraph(text);
                                    document.Add(textParagraph);
                                    break;
                            }
                            
                            document.Close();
                        }
                    }
                }
                
                return File.Exists(outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi chuyển đổi file text sang PDF: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> ConvertImageToPdfAsync(string inputPath, string outputPath)
        {
            try
            {
                // Thử sử dụng LibreOffice trước
                bool result = await ConvertUsingLibreOfficeAsync(inputPath, outputPath);
                if (result) return true;
                
                _logger.LogInformation($"Đang chuyển đổi file ảnh sang PDF: {inputPath}");
                
                // Sử dụng iText7 để tạo PDF với hình ảnh
                using (var stream = new FileStream(outputPath, FileMode.Create))
                {
                    using (var writer = new PdfWriter(stream))
                    {
                        using (var pdf = new PdfDocument(writer))
                        {
                            var document = new iText.Layout.Document(pdf);
                            
                            // Tính toán kích thước trang dựa trên hình ảnh
                            var imageData = ImageDataFactory.Create(inputPath);
                            var image = new Image(imageData);
                            
                            // Thiết lập hình ảnh vừa với trang
                            document.Add(image.SetAutoScale(true));
                            
                            document.Close();
                        }
                    }
                }
                
                return File.Exists(outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi chuyển đổi file ảnh sang PDF: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
} 