using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace SenseLib.Services
{
    public interface IChatbotService
    {
        Task<ChatbotResponse> AskQuestionAsync(string documentContent, string question, List<ChatMessage> history = null);
    }
    
    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatbotService> _logger;
        private readonly int _maxRetries;
        private readonly int _initialRetryDelay;
        private readonly int _maxTokens;

        public ChatbotService(IConfiguration configuration, ILogger<ChatbotService> logger)
        {
            _httpClient = new HttpClient();
            _configuration = configuration;
            _logger = logger;
            _maxRetries = configuration.GetValue<int>("Claude:MaxRetries", 3);
            _initialRetryDelay = configuration.GetValue<int>("Claude:InitialRetryDelay", 2000);
            _maxTokens = configuration.GetValue<int>("Claude:MaxTokens", 4096);
        }

        public async Task<ChatbotResponse> AskQuestionAsync(string documentContent, string question, List<ChatMessage> history = null)
        {
            if (string.IsNullOrEmpty(documentContent))
            {
                return new ChatbotResponse 
                { 
                    Success = false,
                    Answer = "Không có nội dung tài liệu để phân tích.",
                    ErrorMessage = "Nội dung tài liệu trống" 
                };
            }

            if (string.IsNullOrEmpty(question))
            {
                return new ChatbotResponse 
                { 
                    Success = false,
                    Answer = "Vui lòng nhập câu hỏi.",
                    ErrorMessage = "Câu hỏi trống" 
                };
            }

            try
            {
                // Gọi Claude API để trả lời câu hỏi
                string answer = await CallClaudeAPIAsync(documentContent, question, history);
                
                if (!string.IsNullOrEmpty(answer))
                {
                    return new ChatbotResponse
                    {
                        Success = true,
                        Answer = answer
                    };
                }
                
                return new ChatbotResponse
                {
                    Success = false,
                    Answer = "Không thể nhận được câu trả lời từ API. Vui lòng thử lại sau.",
                    ErrorMessage = "Lỗi khi gọi API"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi trong AskQuestionAsync: {ex.Message}");
                return new ChatbotResponse
                {
                    Success = false,
                    Answer = "Đã xảy ra lỗi khi xử lý câu hỏi. Vui lòng thử lại sau.",
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> CallClaudeAPIAsync(string documentContent, string question, List<ChatMessage> history)
        {
            int retryCount = 0;
            int retryDelay = _initialRetryDelay;
            
            // Kiểm tra API key
            string apiKey = _configuration["Claude:ApiKey"];
            _logger.LogInformation($"Claude config: MaxRetries={_maxRetries}, InitialRetryDelay={_initialRetryDelay}, MaxTokens={_maxTokens}");
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("Claude API key chưa được cấu hình");
                return "Lỗi cấu hình API key. Vui lòng liên hệ quản trị viên.";
            }
            else 
            {
                _logger.LogInformation("Claude API key đã được cấu hình");
            }
            
            // Lấy model
            string modelId = _configuration["Claude:ModelId"] ?? "claude-3-sonnet-20240229";
            _logger.LogInformation($"Sử dụng model: {modelId}");
            
            // Giới hạn độ dài tài liệu nếu cần
            if (documentContent.Length > 100000)
            {
                _logger.LogWarning("Nội dung tài liệu quá dài, cắt bớt xuống 100000 ký tự");
                documentContent = documentContent.Substring(0, 100000);
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
                    client.Timeout = TimeSpan.FromSeconds(60); // Tăng timeout lên 60 giây
                    
                    // Tạo danh sách messages cho API (không bao gồm system message)
                    var messages = new List<Dictionary<string, string>>();
                    
                    // Tạo system message ngắn gọn hơn (sẽ được thêm ở cấp cao nhất)
                    var systemPrompt = "Bạn là trợ lý AI giúp trả lời câu hỏi về nội dung tài liệu.";
                    
                    // Tạo văn bản với nội dung tài liệu và hướng dẫn
                    string userPrompt = $@"Tài liệu:
```
{documentContent}
```

Hướng dẫn:
- Hãy trả lời dựa HOÀN TOÀN vào nội dung tài liệu trên.
- Nếu thông tin không có trong tài liệu, hãy nói rằng bạn không thể trả lời được.
- Trả lời bằng tiếng Việt, ngắn gọn và dễ hiểu.

Câu hỏi: {question}";
                    
                    // Thêm lịch sử trò chuyện nếu có
                    if (history != null && history.Count > 0)
                    {
                        foreach (var msg in history)
                        {
                            messages.Add(new Dictionary<string, string> { { "role", msg.Role }, { "content", msg.Content } });
                        }
                    }
                    
                    // Thêm tin nhắn người dùng
                    messages.Add(new Dictionary<string, string> { { "role", "user" }, { "content", userPrompt } });
                    
                    // Tạo request body với system parameter ở cấp cao nhất
                    var requestBody = new Dictionary<string, object>
                    {
                        { "model", modelId },
                        { "max_tokens", _maxTokens },
                        { "temperature", 0.3 },
                        { "system", systemPrompt },
                        { "messages", messages }
                    };
                    
                    // Chuyển đổi thành JSON
                    string jsonContent = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    
                    _logger.LogInformation("Đang gửi request đến Claude API...");
                    
                    // Gửi request đến API
                    var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogInformation($"Phản hồi từ Claude API (StatusCode: {response.StatusCode})");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            // Lưu response để debug
                            _logger.LogInformation($"Phản hồi thành công: {responseBody.Substring(0, Math.Min(responseBody.Length, 500))}...");
                            
                            // Phân tích JSON phản hồi
                            using JsonDocument doc = JsonDocument.Parse(responseBody);
                            JsonElement root = doc.RootElement;
                            
                            _logger.LogInformation("Xử lý phản hồi JSON từ Claude API");
                            
                            if (root.TryGetProperty("content", out JsonElement contentArray))
                            {
                                StringBuilder answer = new StringBuilder();
                                
                                foreach (var contentItem in contentArray.EnumerateArray())
                                {
                                    if (contentItem.TryGetProperty("type", out JsonElement typeElement) && 
                                        typeElement.GetString() == "text" &&
                                        contentItem.TryGetProperty("text", out JsonElement textElement))
                                    {
                                        answer.Append(textElement.GetString());
                                    }
                                }
                                
                                string result = answer.ToString();
                                _logger.LogInformation($"Trả lời thành công ({result.Length} ký tự)");
                                return result;
                            }
                            else
                            {
                                _logger.LogWarning($"Không tìm thấy 'content' trong phản hồi: {responseBody}");
                                
                                // Kiểm tra nếu có thông điệp lỗi
                                if (root.TryGetProperty("error", out JsonElement errorElement) &&
                                   errorElement.TryGetProperty("message", out JsonElement errorMessage))
                                {
                                    _logger.LogError($"Lỗi từ API: {errorMessage.GetString()}");
                                    return $"Lỗi từ Claude API: {errorMessage.GetString()}";
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogError($"Lỗi xử lý phản hồi JSON từ Claude: {ex.Message}");
                            _logger.LogError($"JSON phản hồi: {responseBody}");
                        }
                    }
                    else if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                    {
                        _logger.LogWarning($"Claude API trả về lỗi {response.StatusCode}. Thử lại lần {retryCount + 1} sau {retryDelay}ms");
                        _logger.LogWarning($"Nội dung lỗi: {responseBody}");
                        await Task.Delay(retryDelay);
                        retryDelay *= 2;
                        retryCount++;
                        continue;
                    }
                    else
                    {
                        _logger.LogError($"Claude API trả về lỗi: {response.StatusCode}, Nội dung: {responseBody}");
                        
                        // Parse error message
                        try {
                            using JsonDocument doc = JsonDocument.Parse(responseBody);
                            JsonElement root = doc.RootElement;
                            
                            if (root.TryGetProperty("error", out JsonElement errorElement) &&
                               errorElement.TryGetProperty("message", out JsonElement errorMessage))
                            {
                                _logger.LogError($"Thông điệp lỗi: {errorMessage.GetString()}");
                                return $"Lỗi từ Claude API: {errorMessage.GetString()}";
                            }
                        }
                        catch (Exception ex) {
                            _logger.LogError($"Không thể phân tích JSON lỗi: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi gọi Claude API: {ex.Message}");
                    _logger.LogError($"Exception stack trace: {ex.StackTrace}");
                    
                    if (ex.InnerException != null)
                    {
                        _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    }
                    
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
            
            return "Không thể nhận được câu trả lời từ Claude API. Vui lòng thử lại sau.";
        }
    }
    
    public class ChatbotResponse
    {
        public bool Success { get; set; }
        public string Answer { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    public class ChatMessage
    {
        public string Role { get; set; } // "user" hoặc "assistant"
        public string Content { get; set; }
    }
} 