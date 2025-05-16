using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using SenseLib.Models;
using System.Collections.Generic;

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
                        conversionSuccess = await ConvertWordToPdfAsync(fullSourcePath, pdfPath);
                        break;

                    case ".pptx":
                    case ".ppt":
                        conversionSuccess = await ConvertPowerPointToPdfAsync(fullSourcePath, pdfPath);
                        break;

                    case ".xlsx":
                    case ".xls":
                        conversionSuccess = await ConvertExcelToPdfAsync(fullSourcePath, pdfPath);
                        break;

                    // Thêm các định dạng khác ở đây nếu cần

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
            // Hiện tại sử dụng cách đơn giản để chuyển đổi DOCX sang PDF
            // Trong môi trường thực tế, nên sử dụng thư viện như Aspose.Words, Spire.Doc, 
            // hoặc Office Interop để chuyển đổi chính xác
            
            try
            {
                // Phương pháp 1: Sử dụng LibreOffice (nếu có)
                bool result = await ConvertUsingLibreOfficeAsync(inputPath, outputPath);
                if (result) return true;
                
                // Phương pháp 2: Sử dụng HTML trung gian (chỉ với DOCX)
                // Chuyển DOCX thành HTML, sau đó sử dụng thư viện như PuppeteerSharp để 
                // chuyển HTML thành PDF
                
                _logger.LogWarning("Chuyển đổi DOCX sang PDF chưa được triển khai đầy đủ");
                return false;
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
                // Sử dụng LibreOffice nếu có
                return await ConvertUsingLibreOfficeAsync(inputPath, outputPath);
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
                // Sử dụng LibreOffice nếu có
                return await ConvertUsingLibreOfficeAsync(inputPath, outputPath);
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
                    @"C:\Program Files (x86)\OpenOffice\program\soffice.exe"
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
                await process.WaitForExitAsync();

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

        #endregion
    }
} 