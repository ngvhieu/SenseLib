using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SenseLib.Models;

namespace SenseLib.Controllers
{
    [ApiController]
    [Route("api/tts")]
    public class TextToSpeechController : Controller
    {
        private readonly ILogger<TextToSpeechController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _audioFolder;
        private readonly int _maxTextLength = 2000; // Giới hạn độ dài văn bản tối đa

        public TextToSpeechController(
            ILogger<TextToSpeechController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _audioFolder = Path.Combine(_webHostEnvironment.WebRootPath, "audio", "tts");
            
            // Đảm bảo thư mục tồn tại
            if (!Directory.Exists(_audioFolder))
            {
                Directory.CreateDirectory(_audioFolder);
            }
            
            // Xóa các file tạm cũ
            CleanupOldFiles();
        }
        
        /// <summary>
        /// Xóa các file tạm cũ để tránh tràn bộ nhớ
        /// </summary>
        private void CleanupOldFiles()
        {
            try
            {
                var directory = new DirectoryInfo(_audioFolder);
                foreach (var file in directory.GetFiles())
                {
                    // Xóa các file tạo ra cách đây hơn 1 ngày
                    if (file.CreationTime < DateTime.Now.AddDays(-1))
                    {
                        file.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Không thể xóa file tạm cũ: {ex.Message}");
            }
        }

        // Endpoint kiểm tra xem edge-tts đã hoạt động chưa
        [HttpGet("test")]
        public async Task<IActionResult> TestEdgeTTS()
        {
            try
            {
                // Tạo tên file duy nhất
                string fileName = $"test-{Guid.NewGuid()}.mp3";
                string outputPath = Path.Combine(_audioFolder, fileName);
                
                // Văn bản kiểm tra
                string testText = "Đây là bài kiểm tra Edge TTS trong ứng dụng SenseLib.";
                
                // Đường dẫn URL để truy cập file
                string audioUrl = $"/audio/tts/{fileName}";

                // Đường dẫn đến edge-tts (thường là trong thư mục Scripts của Python)
                string edgeTtsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AppData\\Roaming\\Python\\Scripts\\edge-tts.exe");
                
                bool edgeTtsFound = System.IO.File.Exists(edgeTtsPath);
                string method = "unknown";
                
                ProcessStartInfo processStartInfo;
                if (!edgeTtsFound)
                {
                    // Thử sử dụng python -m edge_tts
                    edgeTtsPath = "python";
                    method = "python -m edge_tts";
                    
                    processStartInfo = new ProcessStartInfo
                    {
                        FileName = edgeTtsPath,
                        Arguments = $"-m edge_tts --voice \"vi-VN-HoaiMyNeural\" --text \"{testText}\" --write-media \"{outputPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }
                else
                {
                    // Sử dụng đường dẫn đầy đủ đến edge-tts.exe
                    method = "edge-tts.exe";
                    
                    processStartInfo = new ProcessStartInfo
                    {
                        FileName = edgeTtsPath,
                        Arguments = $"--voice \"vi-VN-HoaiMyNeural\" --text \"{testText}\" --write-media \"{outputPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }

                string processOutput = "";
                string error = "";
                int exitCode = -1;
                
                using (var process = Process.Start(processStartInfo))
                {
                    await process.WaitForExitAsync();
                    processOutput = await process.StandardOutput.ReadToEndAsync();
                    error = await process.StandardError.ReadToEndAsync();
                    exitCode = process.ExitCode;
                }

                if (exitCode != 0)
                {
                    _logger.LogError($"Lỗi khi kiểm tra TTS: {error}");
                    return StatusCode(500, new 
                    { 
                        success = false, 
                        method = method,
                        command = $"{edgeTtsPath} {processStartInfo.Arguments}",
                        error = error,
                        output = processOutput,
                        exitCode = exitCode,
                        edgeTtsFound = edgeTtsFound
                    });
                }

                return Ok(new 
                { 
                    success = true, 
                    audioUrl = audioUrl,
                    method = method,
                    command = $"{edgeTtsPath} {processStartInfo.Arguments}",
                    output = processOutput
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi trong quá trình kiểm tra TTS: {ex.Message}");
                return StatusCode(500, new { error = "Lỗi máy chủ nội bộ", details = ex.ToString() });
            }
        }

        [HttpPost("convert")]
        public async Task<IActionResult> ConvertTextToSpeech([FromBody] TTSRequest request)
        {
            if (string.IsNullOrEmpty(request.Text))
            {
                return BadRequest(new { error = "Văn bản không được để trống" });
            }

            try
            {
                // Tiền xử lý văn bản
                string text = PreprocessText(request.Text);
                
                // Trong trường hợp văn bản quá dài, chúng ta vẫn xử lý nhưng ghi log cảnh báo
                bool isTextTooLong = false;
                if (text.Length > _maxTextLength)
                {
                    isTextTooLong = true;
                    _logger.LogWarning($"Văn bản quá dài: {text.Length} ký tự, sẽ cắt xuống còn {_maxTextLength} ký tự");
                    text = text.Substring(0, _maxTextLength);
                    // Cắt đến vị trí ký tự cuối cùng của câu gần nhất
                    text = CutToLastSentence(text);
                }

                // Tạo tên file duy nhất                
                string fileName = $"{Guid.NewGuid()}.mp3";                
                string outputPath = Path.Combine(_audioFolder, fileName);                
                
                // Đường dẫn URL để truy cập file                
                string audioUrl = $"/audio/tts/{fileName}";
                
                // Xử lý giọng đọc và tốc độ                
                string voice = string.IsNullOrEmpty(request.Voice) ? "vi-VN-HoaiMyNeural" : request.Voice;                
                string rate = string.IsNullOrEmpty(request.Rate) ? "+0%" : request.Rate;                
                string volume = string.IsNullOrEmpty(request.Volume) ? "+0%" : request.Volume;                
                
                // Log thông tin                
                _logger.LogInformation($"TTS Request - Text length: {text.Length}, Voice: {voice}, Output: {outputPath}");

                // Lưu văn bản vào file tạm để tránh vấn đề với ký tự đặc biệt                
                string tempTextFile = Path.Combine(_audioFolder, $"{Guid.NewGuid()}.txt");                
                await System.IO.File.WriteAllTextAsync(tempTextFile, text, System.Text.Encoding.UTF8);
                
                bool useFallbackMethod = false;
                string error = "";
                ProcessStartInfo processStartInfo;
                ProcessStartInfo fallbackProcessInfo = null;
                
                try
                {
                    // Kiểm tra edge-tts trong các vị trí phổ biến
                    string[] possiblePaths = {
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Roaming\\Python\\Python310\\Scripts\\edge-tts.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Roaming\\Python\\Python311\\Scripts\\edge-tts.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Roaming\\Python\\Python312\\Scripts\\edge-tts.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Roaming\\Python\\Scripts\\edge-tts.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Local\\Programs\\Python\\Python310\\Scripts\\edge-tts.exe"), 
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Local\\Programs\\Python\\Python311\\Scripts\\edge-tts.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Local\\Programs\\Python\\Python312\\Scripts\\edge-tts.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Python310\\Scripts\\edge-tts.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Python311\\Scripts\\edge-tts.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Python312\\Scripts\\edge-tts.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Python310\\Scripts\\edge-tts.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Python311\\Scripts\\edge-tts.exe"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Python312\\Scripts\\edge-tts.exe"),
                        "edge-tts", // Tìm trong PATH
                        "edge-tts.exe" // Tìm trong PATH
                    };
                        
                    // Trực tiếp kiểm tra edge-tts trong PATH bằng cách thử chạy để lấy version
                    bool edgeTtsInPath = false;
                    string edgeTtsVersion = "";
                    try
                    {
                        var versionCheck = new ProcessStartInfo
                        {
                            FileName = "edge-tts",
                            Arguments = "--version",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        
                        using (var proc = Process.Start(versionCheck))
                        {
                            if (proc != null)
                            {
                                await proc.WaitForExitAsync();
                                if (proc.ExitCode == 0)
                                {
                                    edgeTtsInPath = true;
                                    edgeTtsVersion = await proc.StandardOutput.ReadToEndAsync();
                                    _logger.LogInformation($"Tìm thấy edge-tts trong PATH: {edgeTtsVersion.Trim()}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Không tìm thấy edge-tts trong PATH: {ex.Message}");
                    }
                    
                    // Nếu edge-tts có sẵn trong PATH, sử dụng nó trực tiếp
                    if (edgeTtsInPath)
                    {
                        processStartInfo = new ProcessStartInfo
                        {
                            FileName = "edge-tts",
                            Arguments = $"--voice \"{voice}\" --rate=\"{rate}\" --volume=\"{volume}\" --file \"{tempTextFile}\" --write-media \"{outputPath}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        
                        fallbackProcessInfo = new ProcessStartInfo
                        {
                            FileName = "edge-tts",
                            Arguments = $"--voice \"{voice}\" --rate=\"{rate}\" --volume=\"{volume}\" --text \"Văn bản quá dài, không thể xử lý hoàn toàn.\" --write-media \"{outputPath}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                    }
                    else
                    {
                        string edgeTtsPath = null;
                        
                        // Tìm kiếm edge-tts.exe trong các vị trí có thể
                        foreach (var path in possiblePaths)
                        {
                            if (path == "edge-tts" || path == "edge-tts.exe")
                                continue;
                                
                            if (System.IO.File.Exists(path))
                            {
                                edgeTtsPath = path;
                                _logger.LogInformation($"Tìm thấy edge-tts tại: {edgeTtsPath}");
                                break;
                            }
                        }
                            
                            // Nếu vẫn không tìm thấy, thử sử dụng python -m edge_tts
                            if (edgeTtsPath == null)
                            {
                                _logger.LogWarning("Không tìm thấy edge-tts, sẽ sử dụng python -m edge_tts");
                                
                                // Thử kiểm tra xem python đã được cài đặt chưa
                                bool pythonInstalled = false;
                                try
                                {
                                    var pythonCheck = new ProcessStartInfo
                                    {
                                        FileName = "python",
                                        Arguments = "--version",
                                        RedirectStandardOutput = true,
                                        UseShellExecute = false,
                                        CreateNoWindow = true
                                    };
                                    
                                    using (var proc = Process.Start(pythonCheck))
                                    {
                                        if (proc != null)
                                        {
                                            await proc.WaitForExitAsync();
                                            if (proc.ExitCode == 0)
                                            {
                                                pythonInstalled = true;
                                                var pythonVersion = await proc.StandardOutput.ReadToEndAsync();
                                                _logger.LogInformation($"Python đã được cài đặt: {pythonVersion.Trim()}");
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Không tìm thấy Python: {ex.Message}");
                                    return StatusCode(500, new { error = "Không tìm thấy cài đặt Python trên hệ thống. Vui lòng cài đặt Python và edge-tts." });
                                }
                                
                                if (!pythonInstalled)
                                {
                                    return StatusCode(500, new { error = "Không tìm thấy cài đặt Python trên hệ thống. Vui lòng cài đặt Python và edge-tts." });
                                }
                                
                                // Kiểm tra xem edge-tts module đã được cài đặt chưa
                                try
                                {
                                    var edgeTtsModuleCheck = new ProcessStartInfo
                                    {
                                        FileName = "python",
                                        Arguments = "-m pip list | findstr edge-tts",
                                        RedirectStandardOutput = true,
                                        RedirectStandardError = true,
                                        UseShellExecute = false,
                                        CreateNoWindow = true
                                    };
                                    
                                    using (var proc = Process.Start(edgeTtsModuleCheck))
                                    {
                                        if (proc != null)
                                        {
                                            await proc.WaitForExitAsync();
                                            var output = await proc.StandardOutput.ReadToEndAsync();
                                            
                                            if (string.IsNullOrEmpty(output))
                                            {
                                                _logger.LogWarning("edge-tts module chưa được cài đặt trong Python");
                                                return StatusCode(500, new { error = "Chưa cài đặt edge-tts trong Python. Vui lòng cài đặt bằng lệnh: pip install edge-tts" });
                                            }
                                            else
                                            {
                                                _logger.LogInformation($"edge-tts module đã được cài đặt: {output.Trim()}");
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Không thể kiểm tra edge-tts module: {ex.Message}");
                                }
                                
                                // Sử dụng python -m edge_tts
                                processStartInfo = new ProcessStartInfo
                                {
                                    FileName = "python",
                                    Arguments = $"-m edge_tts --voice \"{voice}\" --rate=\"{rate}\" --volume=\"{volume}\" --file \"{tempTextFile}\" --write-media \"{outputPath}\"",
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };
                                
                                // Chuẩn bị phương thức dự phòng với văn bản trực tiếp
                                fallbackProcessInfo = new ProcessStartInfo
                                {
                                    FileName = "python",
                                    Arguments = $"-m edge_tts --voice \"{voice}\" --rate=\"{rate}\" --volume=\"{volume}\" --text \"Văn bản quá dài, không thể xử lý hoàn toàn.\" --write-media \"{outputPath}\"",
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };
                            }
                            else
                            {
                                _logger.LogInformation($"Sử dụng edge-tts tại: {edgeTtsPath}");
                                
                                processStartInfo = new ProcessStartInfo
                                {
                                    FileName = edgeTtsPath,
                                    Arguments = $"--voice \"{voice}\" --rate=\"{rate}\" --volume=\"{volume}\" --file \"{tempTextFile}\" --write-media \"{outputPath}\"",
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };
                                
                                // Chuẩn bị phương thức dự phòng
                                fallbackProcessInfo = new ProcessStartInfo
                                {
                                    FileName = edgeTtsPath,
                                    Arguments = $"--voice \"{voice}\" --rate=\"{rate}\" --volume=\"{volume}\" --text \"Văn bản quá dài, không thể xử lý hoàn toàn.\" --write-media \"{outputPath}\"",
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    UseShellExecute = false,
                                    CreateNoWindow = true
                                };
                            }
                        }

                    // Log lệnh đang thực hiện
                    _logger.LogInformation($"Đang chạy lệnh: {processStartInfo.FileName} {processStartInfo.Arguments}");
                    
                    string processOutput = "";
                    string fullError = "";
                    
                    using (var process = Process.Start(processStartInfo))
                    {
                        if (process == null)
                        {
                            _logger.LogError("Không thể khởi động quá trình chuyển đổi TTS");
                            return StatusCode(500, new { error = "Không thể khởi động quá trình chuyển đổi TTS" });
                        }
                        
                        var timeoutTask = Task.Delay(15000); // Timeout sau 15 giây
                        var processTask = process.WaitForExitAsync();
                        
                        // Đợi quá trình hoặc timeout
                        if (await Task.WhenAny(processTask, timeoutTask) == timeoutTask)
                        {
                            _logger.LogWarning("Quá trình chuyển đổi TTS tốn quá nhiều thời gian, có thể là do văn bản quá dài");
                            try {
                                if (!process.HasExited)
                                {
                                    process.Kill(true);
                                }
                            } catch {}
                            useFallbackMethod = true;
                        }
                        else
                        {
                            // Quá trình đã hoàn thành, kiểm tra kết quả
                            processOutput = await process.StandardOutput.ReadToEndAsync();
                            error = await process.StandardError.ReadToEndAsync();
                            fullError = error;
                            
                            if (process.ExitCode != 0)
                            {
                                _logger.LogError($"Lỗi khi chuyển đổi TTS: {error}");
                                useFallbackMethod = true;
                            }
                        }
                    }
                    
                    // Nếu phương thức chính thất bại, sử dụng phương thức dự phòng
                    if (useFallbackMethod && fallbackProcessInfo != null)
                    {
                        _logger.LogWarning("Sử dụng phương thức dự phòng để tạo audio");
                        
                        // Thử với đoạn văn bản ngắn hơn
                        string shortText = text.Length > 200 ? text.Substring(0, 200) + "..." : text;
                        
                        // Cập nhật đối số để sử dụng văn bản trực tiếp
                        if (fallbackProcessInfo.FileName.EndsWith("python") || fallbackProcessInfo.FileName.EndsWith("python.exe"))
                        {
                            fallbackProcessInfo.Arguments = $"-m edge_tts --voice \"{voice}\" --rate=\"{rate}\" --volume=\"{volume}\" --text \"{shortText}\" --write-media \"{outputPath}\"";
                        }
                        else
                        {
                            fallbackProcessInfo.Arguments = $"--voice \"{voice}\" --rate=\"{rate}\" --volume=\"{volume}\" --text \"{shortText}\" --write-media \"{outputPath}\"";
                        }
                        
                        using (var process = Process.Start(fallbackProcessInfo))
                        {
                            if (process == null)
                            {
                                _logger.LogError("Không thể khởi động quá trình dự phòng");
                                return StatusCode(500, new { 
                                    error = "Không thể chuyển đổi văn bản thành giọng nói", 
                                    details = "Xử lý chính và dự phòng đều thất bại",
                                    fullError = fullError
                                });
                            }
                            
                            await process.WaitForExitAsync();
                            if (process.ExitCode != 0)
                            {
                                string fallbackError = await process.StandardError.ReadToEndAsync();
                                _logger.LogError($"Phương thức dự phòng cũng thất bại: {fallbackError}");
                                return StatusCode(500, new { 
                                    error = "Không thể chuyển đổi văn bản thành giọng nói", 
                                    details = "Cả hai phương pháp đều thất bại",
                                    primaryError = fullError,
                                    fallbackError = fallbackError 
                                });
                            }
                        }
                    }
                    
                    // Xoá file tạm sau khi xử lý xong
                    try {
                        if (System.IO.File.Exists(tempTextFile))
                        {
                            System.IO.File.Delete(tempTextFile);
                        }
                    } catch (Exception ex) {
                        _logger.LogWarning($"Không thể xoá file tạm: {ex.Message}");
                    }
                        
                    return Ok(new { 
                        audioUrl = audioUrl,
                        isTruncated = isTextTooLong || useFallbackMethod,
                        originalLength = request.Text.Length,
                        processedLength = text.Length
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi trong quá trình chuyển đổi TTS: {ex.Message}");
                    
                    // Thử phương pháp dự phòng nếu có lỗi
                    if (fallbackProcessInfo != null)
                    {
                        try
                        {
                            _logger.LogWarning("Sử dụng phương thức dự phòng sau lỗi");
                            
                            using (var process = Process.Start(fallbackProcessInfo))
                            {
                                if (process == null)
                                {
                                    _logger.LogError("Không thể khởi động quá trình dự phòng");
                                    return StatusCode(500, new { 
                                        error = "Không thể chuyển đổi văn bản thành giọng nói", 
                                        details = ex.ToString() 
                                    });
                                }
                                
                                await process.WaitForExitAsync();
                                return Ok(new { 
                                    audioUrl = audioUrl,
                                    isTruncated = true,
                                    error = ex.Message
                                });
                            }
                        }
                        catch (Exception fallbackEx)
                        {
                            // Nếu cả phương pháp dự phòng cũng thất bại
                            _logger.LogError($"Phương pháp dự phòng cũng thất bại: {fallbackEx.Message}");
                            return StatusCode(500, new { 
                                error = "Không thể chuyển đổi văn bản thành giọng nói", 
                                details = "Cả hai phương pháp đều thất bại",
                                primaryError = ex.ToString(),
                                fallbackError = fallbackEx.ToString() 
                            });
                        }
                    }
                    
                    // Trả về thông báo lỗi nếu không có phương pháp dự phòng
                    return StatusCode(500, new { error = "Lỗi máy chủ nội bộ", details = ex.ToString() });
                }
                finally
                {
                    // Đảm bảo xoá file tạm nếu vẫn còn tồn tại
                    try
                    {
                        if (System.IO.File.Exists(tempTextFile))
                        {
                            System.IO.File.Delete(tempTextFile);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi trong quá trình xử lý yêu cầu TTS: {ex.Message}");
                return StatusCode(500, new { error = "Lỗi máy chủ nội bộ", details = ex.ToString() });
            }
        }
        
        /// <summary>
        /// Tiền xử lý văn bản để âm thanh tốt hơn
        /// </summary>
        private string PreprocessText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            // Thay thế các ký tự không hợp lệ để tránh lỗi encoding
            text = text.Replace("\u0000", " "); // Null character
            text = text.Replace("\u001A", " "); // Substitute character
            
            // Loại bỏ khoảng trắng thừa và chuẩn hóa
            text = Regex.Replace(text, @"\s+", " ");
            
            // Thêm dấu chấm vào cuối văn bản nếu không có dấu kết thúc
            if (!text.EndsWith(".") && !text.EndsWith("!") && !text.EndsWith("?"))
            {
                text += ".";
            }
            
            // Đảm bảo có khoảng trắng sau dấu câu để cải thiện nhịp đọc
            text = Regex.Replace(text, @"([.,;:!?])(\S)", "$1 $2");
            
            // Điều chỉnh khoảng dừng ngắn ở dấu phẩy (giảm ngắt quãng)
            text = text.Replace(",", ", ");
            
            // Đảm bảo không có khoảng trắng kép
            text = Regex.Replace(text, @"\s{2,}", " ");
            
            // Thay thế các ký tự đặc biệt có thể gây lỗi
            text = text.Replace("&", " và ");
            text = text.Replace("#", " số ");
            text = text.Replace("%", " phần trăm ");
            text = text.Replace("@", " at ");
            text = text.Replace("—", "-");
            text = text.Replace("–", "-");
            text = text.Replace("|", "");
            text = text.Replace("*", "");
            text = text.Replace("^", "");
            
            // Xử lý các dấu ngoặc đơn và kép
            text = text.Replace("\u201C", "\""); // Dấu ngoặc kép mở (")
            text = text.Replace("\u201D", "\""); // Dấu ngoặc kép đóng (")
            text = text.Replace("\u2018", "'"); // Dấu nháy đơn mở (')
            text = text.Replace("\u2019", "'"); // Dấu nháy đơn đóng (')
            
            // Gộp các câu ngắn với nhau bằng cách xử lý dấu chấm
            // Tăng kích thước câu lên để giảm ngắt quãng
            const int maxSentenceLength = 500; // Tăng từ 300 lên 500 để giảm tần suất cắt
            
            string[] sentences = Regex.Split(text, @"(?<=[.!?])\s+");
            var result = new StringBuilder(text.Length + 100); // Dự phòng thêm dung lượng
            
            // Kết hợp các câu ngắn thành đoạn dài hơn
            var currentParagraph = new StringBuilder();
            
            foreach (var sentence in sentences)
            {
                // Nếu câu hiện tại + câu mới vẫn dưới giới hạn, gộp chúng
                if (currentParagraph.Length + sentence.Length < maxSentenceLength)
                {
                    currentParagraph.Append(sentence);
                    if (!sentence.TrimEnd().EndsWith(".") && 
                        !sentence.TrimEnd().EndsWith("!") && 
                        !sentence.TrimEnd().EndsWith("?"))
                    {
                        currentParagraph.Append(". ");
                    }
                    else
                    {
                        currentParagraph.Append(" ");
                    }
                }
                else
                {
                    // Thêm đoạn hiện tại vào kết quả và bắt đầu đoạn mới
                    if (currentParagraph.Length > 0)
                    {
                        result.Append(currentParagraph);
                        currentParagraph.Clear();
                    }
                    
                    // Bắt đầu đoạn mới với câu hiện tại
                    if (sentence.Length > maxSentenceLength)
                    {
                        // Chia câu dài thành các đoạn ngắn hơn, ưu tiên tại dấu phẩy, chấm phẩy
                        string[] parts = Regex.Split(sentence, @"(?<=[,;:])\s+");
                        var sentencePart = new StringBuilder();
                        
                        foreach (var part in parts)
                        {
                            if (sentencePart.Length + part.Length < maxSentenceLength)
                            {
                                sentencePart.Append(part);
                                // Đảm bảo có dấu chấm nhưng không thêm khoảng trắng dư thừa ở cuối
                                if (!part.TrimEnd().EndsWith(".") && 
                                    !part.TrimEnd().EndsWith("!") && 
                                    !part.TrimEnd().EndsWith("?"))
                                {
                                    sentencePart.Append(". ");
                                }
                                else
                                {
                                    sentencePart.Append(" ");
                                }
                            }
                            else
                            {
                                // Thêm phần câu hiện tại vào kết quả
                                result.Append(sentencePart);
                                sentencePart.Clear();
                                sentencePart.Append(part);
                                if (!part.TrimEnd().EndsWith(".") && 
                                    !part.TrimEnd().EndsWith("!") && 
                                    !part.TrimEnd().EndsWith("?"))
                                {
                                    sentencePart.Append(". ");
                                }
                                else
                                {
                                    sentencePart.Append(" ");
                                }
                            }
                        }
                        
                        // Đảm bảo phần cuối cùng được thêm vào
                        if (sentencePart.Length > 0)
                        {
                            result.Append(sentencePart);
                        }
                    }
                    else
                    {
                        currentParagraph.Append(sentence);
                        if (!sentence.TrimEnd().EndsWith(".") && 
                            !sentence.TrimEnd().EndsWith("!") && 
                            !sentence.TrimEnd().EndsWith("?"))
                        {
                            currentParagraph.Append(". ");
                        }
                        else
                        {
                            currentParagraph.Append(" ");
                        }
                    }
                }
            }
            
            // Đảm bảo phần cuối cùng được thêm vào
            if (currentParagraph.Length > 0)
            {
                result.Append(currentParagraph);
            }
            
            return result.ToString().Trim();
        }
        
        /// <summary>
        /// Cắt văn bản đến dấu kết thúc câu gần nhất
        /// </summary>
        private string CutToLastSentence(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
                
            // Tìm vị trí dấu chấm, chấm hỏi, chấm than cuối cùng
            int lastPeriod = text.LastIndexOf('.');
            int lastQuestion = text.LastIndexOf('?');
            int lastExclamation = text.LastIndexOf('!');
            
            // Lấy vị trí lớn nhất
            int lastPosition = Math.Max(lastPeriod, Math.Max(lastQuestion, lastExclamation));
            
            if (lastPosition > 0)
            {
                return text.Substring(0, lastPosition + 1);
            }
            
            return text;
        }

        [HttpGet("voices")]
        public IActionResult GetAvailableVoices()
        {
            // Edge TTS có sẵn nhiều giọng nói khác nhau, đây chỉ là danh sách một số giọng tiếng Việt
            var vietnameseVoices = new[]
            {
                new { id = "vi-VN-HoaiMyNeural", name = "Hoài My (Nữ)" },
                new { id = "vi-VN-NamMinhNeural", name = "Nam Minh (Nam)" }
            };

            return Ok(new { voices = vietnameseVoices });
        }
    }

    public class TTSRequest
    {
        public string Text { get; set; }
        public string Voice { get; set; }
        public string Rate { get; set; }
        public string Volume { get; set; }
    }
} 
 