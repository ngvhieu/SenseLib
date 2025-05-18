using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser.Filter;
using iText.Kernel.Geom;
using System.Text;

namespace SenseLib.Services
{
    public class PdfService
    {
        private readonly ILogger<PdfService> _logger;

        public PdfService(ILogger<PdfService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Đọc nội dung văn bản từ file PDF
        /// </summary>
        /// <param name="pdfFilePath">Đường dẫn đến file PDF</param>
        /// <returns>Nội dung văn bản trong file PDF</returns>
        public string ExtractTextFromPdf(string pdfFilePath)
        {
            try
            {
                _logger.LogInformation("Bắt đầu trích xuất văn bản từ PDF: {FilePath}", pdfFilePath);
                StringBuilder text = new StringBuilder();
                
                using (PdfReader pdfReader = new PdfReader(pdfFilePath))
                {
                    using (PdfDocument pdfDoc = new PdfDocument(pdfReader))
                    {
                        int numberOfPages = pdfDoc.GetNumberOfPages();
                        _logger.LogInformation("Tài liệu có {PageCount} trang", numberOfPages);
                        
                        // Lặp qua từng trang theo thứ tự
                        for (int i = 1; i <= numberOfPages; i++)
                        {
                            // Sử dụng LocationTextExtractionStrategy để đảm bảo thứ tự văn bản
                            var strategy = new LocationTextExtractionStrategy();
                            string pageContent = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);
                            
                            // Loại bỏ các khoảng trắng thừa
                            pageContent = NormalizePageContent(pageContent);
                            
                            // Thêm tiêu đề cho mỗi trang để tiện theo dõi
                            text.AppendLine($"[Trang {i}/{numberOfPages}]");
                            text.AppendLine(pageContent);
                            text.AppendLine(); // Thêm dòng trống giữa các trang
                            
                            _logger.LogInformation("Đã xử lý trang {PageNumber}/{TotalPages}, độ dài nội dung: {ContentLength} ký tự", 
                                i, numberOfPages, pageContent.Length);
                            
                            // Log đoạn đầu của mỗi trang để debug
                            if (pageContent.Length > 0)
                            {
                                string previewContent = pageContent.Length > 100 
                                    ? pageContent.Substring(0, 100) + "..." 
                                    : pageContent;
                                _logger.LogDebug("Nội dung trang {PageNumber} bắt đầu với: {PageContent}", 
                                    i, previewContent.Replace("\n", " "));
                            }
                        }
                    }
                }
                
                string extractedText = text.ToString();
                _logger.LogInformation("Đã hoàn thành trích xuất văn bản, tổng độ dài: {TextLength} ký tự", extractedText.Length);
                
                // Log đoạn đầu và đoạn cuối để xác nhận thứ tự
                if (extractedText.Length > 200)
                {
                    _logger.LogInformation("Đoạn đầu văn bản: {TextStart}", extractedText.Substring(0, 200) + "...");
                    _logger.LogInformation("Đoạn cuối văn bản: {TextEnd}", "..." + extractedText.Substring(extractedText.Length - 200));
                }
                
                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đọc file PDF {FilePath}", pdfFilePath);
                throw;
            }
        }

        /// <summary>
        /// Đọc nội dung văn bản từ stream PDF
        /// </summary>
        /// <param name="pdfStream">Stream của file PDF</param>
        /// <returns>Nội dung văn bản trong file PDF</returns>
        public string ExtractTextFromPdf(Stream pdfStream)
        {
            try
            {
                _logger.LogInformation("Bắt đầu trích xuất văn bản từ PDF stream");
                StringBuilder text = new StringBuilder();
                
                using (PdfReader pdfReader = new PdfReader(pdfStream))
                {
                    using (PdfDocument pdfDoc = new PdfDocument(pdfReader))
                    {
                        int numberOfPages = pdfDoc.GetNumberOfPages();
                        _logger.LogInformation("Tài liệu có {PageCount} trang", numberOfPages);
                        
                        // Lặp qua từng trang theo thứ tự
                        for (int i = 1; i <= numberOfPages; i++)
                        {
                            // Sử dụng LocationTextExtractionStrategy để đảm bảo thứ tự văn bản
                            var strategy = new LocationTextExtractionStrategy();
                            string pageContent = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);
                            
                            // Loại bỏ các khoảng trắng thừa
                            pageContent = NormalizePageContent(pageContent);
                            
                            // Thêm tiêu đề cho mỗi trang để tiện theo dõi
                            text.AppendLine($"[Trang {i}/{numberOfPages}]");
                            text.AppendLine(pageContent);
                            text.AppendLine(); // Thêm dòng trống giữa các trang
                            
                            _logger.LogInformation("Đã xử lý trang {PageNumber}/{TotalPages}, độ dài nội dung: {ContentLength} ký tự", 
                                i, numberOfPages, pageContent.Length);
                        }
                    }
                }
                
                string extractedText = text.ToString();
                _logger.LogInformation("Đã hoàn thành trích xuất văn bản, tổng độ dài: {TextLength} ký tự", extractedText.Length);
                
                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi đọc file PDF từ stream");
                throw;
            }
        }
        
        /// <summary>
        /// Chuẩn hóa nội dung trang PDF
        /// </summary>
        private string NormalizePageContent(string pageContent)
        {
            // Thay thế dấu tab bằng khoảng trắng
            pageContent = pageContent.Replace("\t", " ");
            
            // Loại bỏ khoảng trắng thừa
            while (pageContent.Contains("  "))
            {
                pageContent = pageContent.Replace("  ", " ");
            }
            
            // Thay thế các dòng trống liên tiếp bằng một dòng trống
            while (pageContent.Contains("\n\n\n"))
            {
                pageContent = pageContent.Replace("\n\n\n", "\n\n");
            }
            
            return pageContent.Trim();
        }
    }
} 