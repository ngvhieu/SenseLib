using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace SenseLib.Utilities
{
    public static class DocumentParser
    {
        /// <summary>
        /// Trích xuất văn bản từ tài liệu PDF
        /// </summary>
        /// <param name="filePath">Đường dẫn đến file PDF</param>
        /// <returns>Nội dung văn bản trích xuất được</returns>
        public static async Task<string> ExtractTextFromPdfAsync(string filePath)
        {
            try 
            {
                var text = new StringBuilder();
                
                // iText7 sử dụng cấu trúc khác với iTextSharp
                using (var pdfReader = new PdfReader(filePath))
                {
                    using (var pdfDocument = new PdfDocument(pdfReader))
                    {
                        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                        {
                            var page = pdfDocument.GetPage(i);
                            var strategy = new SimpleTextExtractionStrategy();
                            string pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                            text.AppendLine(pageText);
                        }
                    }
                }
                
                return text.ToString();
            }
            catch (Exception ex)
            {
                return $"Lỗi khi đọc file PDF: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Trích xuất văn bản từ tài liệu DOCX
        /// </summary>
        /// <param name="filePath">Đường dẫn đến file DOCX</param>
        /// <returns>Nội dung văn bản trích xuất được</returns>
        public static async Task<string> ExtractTextFromDocxAsync(string filePath)
        {
            try
            {
                StringBuilder text = new StringBuilder();
                
                using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
                {
                    if (doc.MainDocumentPart != null && doc.MainDocumentPart.Document != null && 
                        doc.MainDocumentPart.Document.Body != null)
                    {
                        // Lấy tất cả phần tử Text trong document
                        var paragraphs = doc.MainDocumentPart.Document.Body.Elements<Paragraph>();
                        foreach (var paragraph in paragraphs)
                        {
                            text.AppendLine(paragraph.InnerText);
                        }
                    }
                }
                
                return text.ToString();
            }
            catch (Exception ex)
            {
                return $"Lỗi khi đọc file DOCX: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Trích xuất văn bản từ tài liệu dựa trên loại file
        /// </summary>
        /// <param name="filePath">Đường dẫn đến file</param>
        /// <returns>Nội dung văn bản trích xuất được</returns>
        public static async Task<string> ExtractTextAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return "File không tồn tại";
            }
            
            string extension = Path.GetExtension(filePath).ToLower();
            
            switch (extension)
            {
                case ".pdf":
                    return await ExtractTextFromPdfAsync(filePath);
                
                case ".docx":
                    return await ExtractTextFromDocxAsync(filePath);
                
                case ".txt":
                    return await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                
                default:
                    return $"Không hỗ trợ định dạng file {extension}";
            }
        }
    }
} 