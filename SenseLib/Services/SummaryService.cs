using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Text;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SenseLib.Services
{
    public interface ISummaryService
    {
        Task<string> SummarizeTextAsync(string text, int maxLength = 2000);
    }

    public class SummaryService : ISummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SummaryService> _logger;
        private readonly int _maxRetries;
        private readonly int _initialRetryDelay;

        public SummaryService(IConfiguration configuration, ILogger<SummaryService> logger)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
            _maxRetries = configuration.GetValue<int>("Claude:MaxRetries", 3);
            _initialRetryDelay = configuration.GetValue<int>("Claude:InitialRetryDelay", 2000);
            _logger = logger;
        }

        public async Task<string> SummarizeTextAsync(string text, int maxLength = 2000)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "Không có nội dung để tóm tắt.";
            }

            try 
            {
                // Nếu văn bản ngắn, trả về nguyên văn
                if (text.Length <= 1000)
                {
                    return text;
                }

                // Gọi Claude API
                string summary = await CallClaudeAPI(text, maxLength);
                
                if (!string.IsNullOrEmpty(summary))
                {
                    // Giới hạn độ dài tóm tắt nếu cần
                    if (summary.Length > maxLength)
                    {
                        summary = summary.Substring(0, maxLength - 3) + "...";
                    }
                    return summary;
                }
                
                return "Không thể tạo tóm tắt từ API. Vui lòng thử lại sau.";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi trong SummarizeTextAsync: {ex.Message}");
                return $"Đã xảy ra lỗi khi tóm tắt nội dung. Vui lòng thử lại sau.";
            }
        }

        private async Task<string> CallClaudeAPI(string text, int maxLength = 2000)
        {
            int retryCount = 0;
            int retryDelay = _initialRetryDelay;
            
            // Kiểm tra API key
            string apiKey = _configuration["Claude:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Claude API key chưa được cấu hình");
                return "Lỗi cấu hình API key. Vui lòng liên hệ quản trị viên.";
            }
            
            // Lấy model
            string modelId = _configuration["Claude:ModelId"] ?? "claude-3-haiku-20240307";
            
            // Giới hạn độ dài văn bản
            if (text.Length > 100000)
            {
                text = text.Substring(0, 100000);
            }
            
            while (retryCount <= _maxRetries)
            {
                try
                {
                    _logger.LogInformation($"Gọi Claude API với mô hình: {modelId} (lần thử: {retryCount + 1})");
                    
                    // Chuẩn bị HTTP request
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                    
                    // Prompt đặc biệt cho tóm tắt
                    string prompt = $@"
Hãy tóm tắt văn bản sau một cách ngắn gọn, rõ ràng và súc tích. 
Giữ nguyên các ý chính, thông tin quan trọng và quan điểm chính của văn bản gốc.
Tạo bản tóm tắt với độ dài khoảng {Math.Min(maxLength, 2000)} ký tự.
Viết bản tóm tắt bằng ngôn ngữ của văn bản gốc. Nếu văn bản bằng tiếng Việt, hãy tóm tắt bằng tiếng Việt.

Văn bản cần tóm tắt:
{text}";

                    // Tạo request body
                    var requestBody = new
                    {
                        model = modelId,
                        max_tokens = 4096,
                        temperature = 0.1,
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        }
                    };
                    
                    // Chuyển đổi thành JSON
                    string jsonContent = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    
                    // Gửi request đến API
                    var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogInformation($"Phản hồi từ Claude API ({response.StatusCode})");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            // Phân tích JSON phản hồi
                            using JsonDocument doc = JsonDocument.Parse(responseBody);
                            JsonElement root = doc.RootElement;
                            
                            if (root.TryGetProperty("content", out JsonElement contentArray))
                            {
                                foreach (var contentItem in contentArray.EnumerateArray())
                                {
                                    if (contentItem.TryGetProperty("type", out JsonElement typeElement) && 
                                        typeElement.GetString() == "text" &&
                                        contentItem.TryGetProperty("text", out JsonElement textElement))
                                    {
                                        string summary = textElement.GetString();
                                        _logger.LogInformation($"Tóm tắt thành công với Claude ({summary.Length} ký tự)");
                                        return summary;
                                    }
                                }
                            }
                            
                            _logger.LogWarning($"Không thể trích xuất tóm tắt từ phản hồi Claude: {responseBody}");
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError($"Lỗi xử lý phản hồi JSON từ Claude: {ex.Message}");
                        }
                    }
                    else if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                    {
                        _logger.LogWarning($"Claude API trả về lỗi {response.StatusCode}. Thử lại lần {retryCount + 1} sau {retryDelay}ms");
                        await Task.Delay(retryDelay);
                        retryDelay *= 2;
                        retryCount++;
                        continue;
                    }
                    else
                    {
                        _logger.LogError($"Claude API trả về lỗi: {response.StatusCode}, Nội dung: {responseBody}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi gọi Claude API: {ex.Message}");
                    if (retryCount < _maxRetries)
                    {
                        retryCount++;
                        await Task.Delay(retryDelay);
                        retryDelay *= 2;
                        continue;
                    }
                }
                
                retryCount++;
            }
            
            return "Không thể tạo tóm tắt từ Claude API. Vui lòng thử lại sau.";
        }
    }
} 