using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace SenseLib.Services
{
    public interface IDocxService
    {
        Task<string> ConvertDocxToHtml(string filePath, int? pageNumber = null);
        int GetPageCount(string filePath);
    }

    public class DocxService : IDocxService
    {
        private readonly IWebHostEnvironment _hostEnvironment;

        public DocxService(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public async Task<string> ConvertDocxToHtml(string filePath, int? pageNumber = null)
        {
            // Đường dẫn tuyệt đối
            string absolutePath = Path.Combine(_hostEnvironment.WebRootPath, filePath.TrimStart('/'));
            
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"Không tìm thấy file: {absolutePath}");
            }

            StringBuilder htmlContent = new StringBuilder();
            htmlContent.Append("<div class='docx-document'>");

            try
            {
                using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(absolutePath, false))
                {
                    MainDocumentPart mainPart = wordDocument.MainDocumentPart;
                    
                    // Lấy tất cả đoạn văn bản
                    IEnumerable<Paragraph> paragraphs = mainPart.Document.Body.Descendants<Paragraph>();
                    
                    // Tính toán số đoạn văn bản mỗi trang (giả định)
                    int totalParagraphs = paragraphs.Count();
                    int paragraphsPerPage = totalParagraphs > 0 ? Math.Max(totalParagraphs / Math.Max(1, GetPageCount(absolutePath)), 1) : 20;
                    
                    // Lọc các đoạn văn bản dựa trên số trang
                    if (pageNumber.HasValue && pageNumber.Value > 0)
                    {
                        int startIdx = (pageNumber.Value - 1) * paragraphsPerPage;
                        paragraphs = paragraphs.Skip(startIdx).Take(paragraphsPerPage);
                    }

                    foreach (var paragraph in paragraphs)
                    {
                        htmlContent.Append("<p>");
                        
                        // Xử lý từng run trong đoạn
                        foreach (var run in paragraph.Descendants<Run>())
                        {
                            string text = run.InnerText;
                            bool isBold = run.RunProperties?.Bold != null;
                            bool isItalic = run.RunProperties?.Italic != null;
                            bool isUnderline = run.RunProperties?.Underline != null;
                            
                            // Áp dụng các định dạng
                            if (isBold) htmlContent.Append("<strong>");
                            if (isItalic) htmlContent.Append("<em>");
                            if (isUnderline) htmlContent.Append("<u>");
                            
                            htmlContent.Append(System.Net.WebUtility.HtmlEncode(text));
                            
                            if (isUnderline) htmlContent.Append("</u>");
                            if (isItalic) htmlContent.Append("</em>");
                            if (isBold) htmlContent.Append("</strong>");
                        }
                        
                        htmlContent.Append("</p>");
                    }
                }
            }
            catch (Exception ex)
            {
                htmlContent.Clear();
                htmlContent.Append($"<div class='error'>Lỗi khi đọc tài liệu DOCX: {ex.Message}</div>");
            }

            htmlContent.Append("</div>");
            return await Task.FromResult(htmlContent.ToString());
        }

        public int GetPageCount(string filePath)
        {
            try
            {
                string absolutePath = filePath;
                if (!Path.IsPathRooted(filePath))
                {
                    absolutePath = Path.Combine(_hostEnvironment.WebRootPath, filePath.TrimStart('/'));
                }
                
                if (!File.Exists(absolutePath))
                {
                    throw new FileNotFoundException($"Không tìm thấy file: {absolutePath}");
                }

                // Đếm số trang dựa trên ước lượng
                // (Đây chỉ là phương pháp ước lượng, không chính xác 100%)
                using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(absolutePath, false))
                {
                    MainDocumentPart mainPart = wordDocument.MainDocumentPart;
                    var paragraphs = mainPart.Document.Body.Descendants<Paragraph>().Count();
                    
                    // Giả định mỗi trang có khoảng 20 đoạn văn
                    int estimatedPages = (int)Math.Ceiling(paragraphs / 20.0);
                    return Math.Max(1, estimatedPages); // Đảm bảo có ít nhất 1 trang
                }
            }
            catch (Exception)
            {
                // Trả về giá trị mặc định nếu có lỗi
                return 5;
            }
        }
    }
} 