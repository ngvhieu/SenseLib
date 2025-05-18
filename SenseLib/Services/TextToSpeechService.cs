using Google.Cloud.TextToSpeech.V1;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace SenseLib.Services
{
    public class TextToSpeechService
    {
        private readonly ILogger<TextToSpeechService> _logger;
        private readonly IConfiguration _configuration;

        public TextToSpeechService(ILogger<TextToSpeechService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Chuyển văn bản thành file âm thanh MP3 sử dụng Google Cloud TTS
        /// </summary>
        /// <param name="text">Văn bản cần chuyển thành giọng nói</param>
        /// <param name="outputPath">Đường dẫn để lưu file âm thanh</param>
        /// <param name="languageCode">Mã ngôn ngữ (vi-VN, en-US, ...)</param>
        /// <param name="voiceName">Tên giọng đọc (VN-standard-A, ...)</param>
        /// <returns>Đường dẫn đến file âm thanh</returns>
        public async Task<string> SynthesizeSpeechAsync(
            string text, 
            string outputPath, 
            string languageCode = "vi-VN", 
            string voiceName = "vi-VN-Standard-A")
        {
            try
            {
                var client = TextToSpeechClient.Create();

                var request = new SynthesizeSpeechRequest
                {
                    Input = new SynthesisInput { Text = text },
                    Voice = new VoiceSelectionParams
                    {
                        LanguageCode = languageCode,
                        Name = voiceName
                    },
                    AudioConfig = new AudioConfig
                    {
                        AudioEncoding = AudioEncoding.Mp3
                    }
                };

                var response = await client.SynthesizeSpeechAsync(request);

                // Đảm bảo thư mục đích tồn tại
                var directory = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Ghi dữ liệu âm thanh vào file
                using (var output = File.Create(outputPath))
                {
                    response.AudioContent.WriteTo(output);
                }

                _logger.LogInformation("Đã tạo file âm thanh tại: {FilePath}", outputPath);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chuyển văn bản thành âm thanh");
                throw;
            }
        }

        /// <summary>
        /// Đọc văn bản dài bằng cách chia thành các đoạn nhỏ
        /// </summary>
        /// <param name="longText">Văn bản dài cần đọc</param>
        /// <param name="outputDirectory">Thư mục lưu các file âm thanh</param>
        /// <param name="baseFileName">Tên cơ sở cho các file âm thanh</param>
        /// <param name="languageCode">Mã ngôn ngữ</param>
        /// <param name="voiceName">Tên giọng đọc</param>
        /// <returns>Danh sách đường dẫn đến các file âm thanh</returns>
        public async Task<List<string>> SynthesizeLongTextAsync(
            string longText, 
            string outputDirectory, 
            string baseFileName,
            string languageCode = "vi-VN", 
            string voiceName = "vi-VN-Standard-A")
        {
            // Google Cloud TTS có giới hạn 5000 ký tự cho mỗi request
            const int MaxChunkSize = 4500;
            var outputFiles = new List<string>();

            // Chuẩn hóa văn bản để đảm bảo các dòng được xử lý đúng
            longText = NormalizeText(longText);
            
            // Ghi log văn bản
            _logger.LogInformation("Đang xử lý văn bản có độ dài {TextLength} ký tự", longText.Length);
            if (longText.Length > 200)
            {
                _logger.LogInformation("Đoạn đầu văn bản: {TextStart}", longText.Substring(0, 200) + "...");
            }

            // Chia văn bản thành các đoạn nhỏ
            var textChunks = SplitTextIntoChunks(longText, MaxChunkSize);
            _logger.LogInformation("Đã chia văn bản thành {ChunkCount} đoạn", textChunks.Count);
            
            // Đảm bảo thư mục đích tồn tại
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Xử lý từng đoạn văn bản
            for (int i = 0; i < textChunks.Count; i++)
            {
                var chunk = textChunks[i];
                var outputPath = Path.Combine(outputDirectory, $"{baseFileName}_{i+1}.mp3");
                
                _logger.LogInformation("Đang xử lý đoạn {ChunkNumber}/{TotalChunks} (độ dài: {ChunkLength})", 
                    i+1, textChunks.Count, chunk.Length);

                try
                {
                    await SynthesizeSpeechAsync(chunk, outputPath, languageCode, voiceName);
                    outputFiles.Add(outputPath);
                    _logger.LogInformation("Đã xử lý thành công đoạn {ChunkNumber}", i+1);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xử lý đoạn văn bản {ChunkNumber}", i+1);
                    // Tiếp tục với đoạn tiếp theo thay vì dừng toàn bộ quá trình
                }
            }

            return outputFiles;
        }

        /// <summary>
        /// Chuẩn hóa văn bản trước khi xử lý
        /// </summary>
        private string NormalizeText(string text)
        {
            _logger.LogInformation("Đang chuẩn hóa văn bản với độ dài {TextLength} ký tự", text.Length);
            
            // Xóa các marker [Trang x/y] thừa (nếu có)
            var pageMarkRegex = new System.Text.RegularExpressions.Regex(@"\[Trang \d+\/\d+\]");
            text = pageMarkRegex.Replace(text, "");
            
            // Loại bỏ các ký tự đặc biệt gây nhiễu
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // Loại bỏ khoảng trắng thừa
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }
            
            // Loại bỏ nhiều dòng trống liên tiếp
            while (text.Contains("\n\n\n"))
            {
                text = text.Replace("\n\n\n", "\n\n");
            }
            
            // Đảm bảo kết thúc câu có dấu và khoảng cách đúng
            text = text.Replace(". ", ".\n").Replace("! ", "!\n").Replace("? ", "?\n");
            
            // Xử lý thụt đầu dòng đặc biệt và các ký tự gây khó đọc
            text = text.Replace("\t", " ");
            
            string result = text.Trim();
            _logger.LogInformation("Đã chuẩn hóa văn bản, độ dài mới: {TextLength} ký tự", result.Length);
            
            // Log đoạn đầu và đoạn cuối để kiểm tra
            if (result.Length > 200)
            {
                _logger.LogDebug("Đoạn đầu sau khi chuẩn hóa: {TextStart}", result.Substring(0, 200) + "...");
                _logger.LogDebug("Đoạn cuối sau khi chuẩn hóa: {TextEnd}", "..." + result.Substring(result.Length - 200));
            }
            
            return result;
        }

        /// <summary>
        /// Chia văn bản dài thành các đoạn nhỏ, cố gắng chia ở ranh giới câu
        /// </summary>
        private List<string> SplitTextIntoChunks(string text, int maxChunkSize)
        {
            var chunks = new List<string>();
            var sentenceDelimiters = new[] { '.', '!', '?', '\n' };

            // Giữ track vị trí bắt đầu
            int startIndex = 0;
            
            while (startIndex < text.Length)
            {
                int endIndex;
                
                // Nếu đoạn văn bản còn lại ngắn hơn maxChunkSize, lấy hết
                if (startIndex + maxChunkSize >= text.Length)
                {
                    endIndex = text.Length;
                }
                else
                {
                    // Tìm vị trí kết thúc câu phù hợp
                    int searchEndIndex = Math.Min(startIndex + maxChunkSize, text.Length - 1);
                    int delimiterPos = text.LastIndexOfAny(sentenceDelimiters, searchEndIndex);
                    
                    // Nếu tìm thấy dấu kết thúc câu và nó nằm sau vị trí bắt đầu
                    if (delimiterPos > startIndex)
                    {
                        endIndex = delimiterPos + 1; // +1 để bao gồm dấu câu
                    }
                    else
                    {
                        // Nếu không tìm thấy dấu kết thúc câu, chia ở khoảng trắng gần nhất
                        int spacePos = text.LastIndexOf(' ', searchEndIndex);
                        if (spacePos > startIndex)
                        {
                            endIndex = spacePos + 1;
                        }
                        else
                        {
                            // Nếu không tìm thấy khoảng trắng, cắt ở maxChunkSize
                            endIndex = Math.Min(startIndex + maxChunkSize, text.Length);
                        }
                    }
                }
                
                // Thêm đoạn mới vào danh sách
                if (endIndex > startIndex)
                {
                    string chunk = text.Substring(startIndex, endIndex - startIndex).Trim();
                    if (!string.IsNullOrWhiteSpace(chunk))
                    {
                        chunks.Add(chunk);
                    }
                }
                
                // Cập nhật vị trí bắt đầu mới
                startIndex = endIndex;
            }

            return chunks;
        }
    }
} 