using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SenseLib.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
        Task SendContactNotificationAsync(string name, string email, string subject, string message);
        Task SendPasswordResetAsync(string email, string userName, string resetLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogError("Không thể gửi email. Địa chỉ email không được cung cấp.");
                throw new ArgumentException("Email không được trống", nameof(email));
            }

            var mailSettings = _configuration.GetSection("MailSettings");
            
            var fromMail = mailSettings["Mail"];
            var fromPassword = mailSettings["Password"];
            var fromDisplayName = mailSettings["DisplayName"];
            var smtpHost = mailSettings["Host"];
            var smtpPort = int.Parse(mailSettings["Port"]);
            var enableSsl = bool.Parse(mailSettings["EnableSsl"]);

            if (string.IsNullOrWhiteSpace(fromMail) || string.IsNullOrWhiteSpace(fromPassword))
            {
                _logger.LogError("Không thể gửi email. Chưa cấu hình thông tin email.");
                throw new InvalidOperationException("Chưa cấu hình thông tin email của hệ thống");
            }

            try
            {
                _logger.LogInformation($"Bắt đầu gửi email tới {email} với tiêu đề: {subject}");
                
                var mail = new MailMessage
                {
                    From = new MailAddress(fromMail, fromDisplayName),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };

                mail.To.Add(new MailAddress(email));

                using (var smtp = new SmtpClient(smtpHost, smtpPort))
                {
                    smtp.Credentials = new NetworkCredential(fromMail, fromPassword);
                    smtp.EnableSsl = enableSsl;
                    
                    // Thử gửi mail với timeout phù hợp
                    smtp.Timeout = 30000; // 30 giây
                    await smtp.SendMailAsync(mail);
                }
                
                _logger.LogInformation($"Đã gửi email thành công tới {email}");
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, $"Lỗi SMTP khi gửi email tới {email}: {ex.Message}, Status Code: {ex.StatusCode}");
                throw new InvalidOperationException($"Không thể gửi email qua máy chủ SMTP: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi không xác định khi gửi email tới {email}: {ex.Message}");
                throw new InvalidOperationException($"Không thể gửi email: {ex.Message}", ex);
            }
        }

        public async Task SendContactNotificationAsync(string name, string email, string subject, string message)
        {
            var adminEmail = _configuration["AdminEmail"];
            
            if (string.IsNullOrEmpty(adminEmail))
            {
                _logger.LogWarning("Email quản trị viên chưa được cấu hình, sử dụng mặc định");
                adminEmail = "admin@senselib.com"; // Mặc định nếu không có cấu hình
            }

            try
            {
                var htmlMessage = $@"
                    <h2>Thông báo liên hệ mới từ website</h2>
                    <p><strong>Từ:</strong> {name} ({email})</p>
                    <p><strong>Tiêu đề:</strong> {subject}</p>
                    <p><strong>Nội dung:</strong></p>
                    <div style='padding: 15px; border-left: 4px solid #ccc;'>
                        {message.Replace(Environment.NewLine, "<br />")}
                    </div>
                    <p>Vui lòng đăng nhập vào hệ thống quản trị để xem chi tiết.</p>
                ";

                await SendEmailAsync(adminEmail, $"Liên hệ mới: {subject}", htmlMessage);
                _logger.LogInformation($"Đã gửi thông báo liên hệ mới từ {email} đến quản trị viên");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Không thể gửi thông báo liên hệ đến quản trị viên: {ex.Message}");
                // Không throw exception ở đây để tránh ảnh hưởng đến luồng chính
                // nhưng vẫn đảm bảo có log lỗi
            }
        }
        
        public async Task SendPasswordResetAsync(string email, string userName, string resetLink)
        {
            try
            {
                _logger.LogInformation($"Chuẩn bị gửi email đặt lại mật khẩu cho {email}");
                
                string subject = "Yêu cầu đặt lại mật khẩu - SenseLib";
                
                string message = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <div style='background-color: #3498db; color: white; padding: 20px; text-align: center;'>
                            <h1 style='margin: 0;'>SenseLib</h1>
                            <p style='margin: 5px 0 0 0;'>Thư viện điện tử</p>
                        </div>
                        
                        <div style='padding: 20px; border: 1px solid #ddd; border-top: none;'>
                            <h2>Xin chào {userName},</h2>
                            <p>Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
                            <p>Để đặt lại mật khẩu, vui lòng nhấp vào liên kết bên dưới:</p>
                            
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetLink}' style='background-color: #3498db; color: white; padding: 12px 25px; text-decoration: none; border-radius: 4px; font-weight: bold;'>Đặt lại mật khẩu</a>
                            </div>
                            
                            <p>Liên kết này sẽ hết hạn sau 24 giờ. Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                            <p>Lưu ý: Không tiết lộ liên kết này cho bất kỳ ai khác, vì họ có thể sử dụng nó để thay đổi mật khẩu của bạn.</p>
                            
                            <p style='margin-top: 30px;'>Trân trọng,<br>Đội ngũ SenseLib</p>
                        </div>
                        
                        <div style='background-color: #f4f4f4; padding: 15px; text-align: center; font-size: 12px; color: #666;'>
                            <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                            <p>&copy; @DateTime.Now.Year SenseLib. Tất cả các quyền được bảo lưu.</p>
                        </div>
                    </div>
                ";
                
                await SendEmailAsync(email, subject, message);
                _logger.LogInformation($"Đã gửi email đặt lại mật khẩu thành công cho {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi email đặt lại mật khẩu cho {email}: {ex.Message}");
                throw new InvalidOperationException($"Không thể gửi email đặt lại mật khẩu: {ex.Message}", ex);
            }
        }
    }
} 