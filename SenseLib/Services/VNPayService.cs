using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using SenseLib.Models;

namespace SenseLib.Services
{
    public class VNPayService
    {
        private readonly VNPayConfig _vnpayConfig;

        public VNPayService(IOptions<VNPayConfig> vnpayConfig)
        {
            _vnpayConfig = vnpayConfig.Value;
        }

        public class PaymentRequest
        {
            public string PaymentUrl { get; set; }
            public string TxnRef { get; set; }
        }

        public PaymentRequest CreatePaymentUrl(string orderType, decimal amount, string orderDescription, string orderInfo, string ipAddress, string returnUrl)
        {
            // Cấu hình VnPay
            VnPayLibrary vnpay = new VnPayLibrary();
            
            // Thông tin cơ bản - Thứ tự các trường phải đúng theo quy định
            vnpay.AddRequestData("vnp_Version", _vnpayConfig.Version);
            vnpay.AddRequestData("vnp_Command", _vnpayConfig.Command);
            vnpay.AddRequestData("vnp_TmnCode", _vnpayConfig.TmnCode);
            
            // Số tiền thanh toán - Nhân với 100 và chuyển thành số nguyên (VD: 10,000 VND -> 1000000)
            long amountInVND = (long)(amount * 100);
            vnpay.AddRequestData("vnp_Amount", amountInVND.ToString());
            
            // Mã hóa GD duy nhất - Sử dụng 15 số từ timestamp
            string txnRef = DateTime.Now.Ticks.ToString();
            // Đảm bảo độ dài tối đa 15 ký tự
            if (txnRef.Length > 15)
            {
                txnRef = txnRef.Substring(txnRef.Length - 15);
            }
            vnpay.AddRequestData("vnp_TxnRef", txnRef);
            
            // Tạo ngày giao dịch theo định dạng yyyyMMddHHmmss
            string createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            vnpay.AddRequestData("vnp_CreateDate", createDate);
            
            // Thông tin tiền tệ và vị trí
            vnpay.AddRequestData("vnp_CurrCode", _vnpayConfig.CurrCode); // VND
            vnpay.AddRequestData("vnp_Locale", _vnpayConfig.Locale);     // vn
            
            // Địa chỉ IP - Phải là IPv4 hợp lệ
            if (!string.IsNullOrEmpty(ipAddress) && ipAddress != "127.0.0.1" && !ipAddress.Contains(":"))
            {
                vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            }
            else
            {
                vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");
            }
            
            // Thông tin mô tả giao dịch - Không dùng ký tự đặc biệt
            string sanitizedOrderInfo = SanitizeInput(orderInfo);
            vnpay.AddRequestData("vnp_OrderInfo", sanitizedOrderInfo);
            
            // Loại hàng hóa: billpayment
            vnpay.AddRequestData("vnp_OrderType", orderType);
            
            // URL để VNPay gọi lại sau khi xử lý thanh toán - Phải là URL đầy đủ, có chứa https://
            if (!string.IsNullOrEmpty(returnUrl) && (returnUrl.StartsWith("http://") || returnUrl.StartsWith("https://")))
            {
                vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
            }
            else
            {
                // Nếu URL không hợp lệ, sử dụng URL mặc định
                vnpay.AddRequestData("vnp_ReturnUrl", "https://localhost:7065/VNPay/PaymentCallback");
            }
            
            // Tạo URL thanh toán với chữ ký bảo mật
            string paymentUrl = vnpay.CreateRequestUrl(_vnpayConfig.BaseUrl, _vnpayConfig.HashSecret);
            
            return new PaymentRequest
            {
                PaymentUrl = paymentUrl,
                TxnRef = txnRef
            };
        }
        
        // Phương thức làm sạch dữ liệu đầu vào để tránh ký tự đặc biệt
        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";
                
            // Loại bỏ các ký tự đặc biệt có thể gây lỗi khi gửi đến VNPay
            string pattern = @"[^a-zA-Z0-9\s.,:]";
            string sanitized = System.Text.RegularExpressions.Regex.Replace(input, pattern, "");
            
            // Giới hạn độ dài
            if (sanitized.Length > 255)
                sanitized = sanitized.Substring(0, 255);
                
            return sanitized;
        }

        public bool ValidatePayment(Dictionary<string, string> vnpayData)
        {
            VnPayLibrary vnpay = new VnPayLibrary();
            
            foreach (var kvp in vnpayData)
            {
                // Chỉ thêm dữ liệu vnp_
                if (kvp.Key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(kvp.Key, kvp.Value);
                }
            }
            
            string vnp_SecureHash = vnpayData.ContainsKey("vnp_SecureHash") ? vnpayData["vnp_SecureHash"] : "";
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _vnpayConfig.HashSecret);
            
            return checkSignature;
        }

        public PaymentResponse GetPaymentResponse(Dictionary<string, string> vnpayData)
        {
            string vnp_ResponseCode = vnpayData.ContainsKey("vnp_ResponseCode") ? vnpayData["vnp_ResponseCode"] : "";
            string vnp_TransactionStatus = vnpayData.ContainsKey("vnp_TransactionStatus") ? vnpayData["vnp_TransactionStatus"] : "";
            string vnp_TxnRef = vnpayData.ContainsKey("vnp_TxnRef") ? vnpayData["vnp_TxnRef"] : "";
            string vnp_Amount = vnpayData.ContainsKey("vnp_Amount") ? vnpayData["vnp_Amount"] : "0";
            
            decimal amount = decimal.Parse(vnp_Amount) / 100; // Chuyển đổi lại số tiền
            
            return new PaymentResponse
            {
                Success = (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00"),
                OrderId = vnp_TxnRef,
                TransactionId = vnpayData.ContainsKey("vnp_TransactionNo") ? vnpayData["vnp_TransactionNo"] : "",
                Amount = amount,
                ResponseCode = vnp_ResponseCode,
                Message = GetResponseMessage(vnp_ResponseCode)
            };
        }

        private string GetResponseMessage(string responseCode)
        {
            switch (responseCode)
            {
                case "00":
                    return "Giao dịch thành công";
                case "01":
                    return "Giao dịch đã tồn tại";
                case "02":
                    return "Merchant không hợp lệ";
                case "03":
                    return "Dữ liệu gửi sang không đúng định dạng";
                case "04":
                    return "Khởi tạo GD không thành công do Website đang bị tạm khóa";
                case "05":
                    return "Giao dịch không thành công do: Quý khách nhập sai mật khẩu quá số lần quy định";
                case "06":
                    return "Giao dịch không thành công do Quý khách nhập sai mật khẩu ngân hàng";
                case "07":
                    return "Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường)";
                case "09":
                    return "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking";
                case "10":
                    return "Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần";
                case "11":
                    return "Giao dịch không thành công do: Đã hết hạn chờ thanh toán";
                case "12":
                    return "Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa";
                case "24":
                    return "Giao dịch không thành công do: Khách hàng hủy giao dịch";
                case "51":
                    return "Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch";
                case "65":
                    return "Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày";
                case "75":
                    return "Ngân hàng thanh toán đang bảo trì";
                case "79":
                    return "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định";
                case "99":
                    return "Các lỗi khác";
                default:
                    return "Lỗi không xác định";
            }
        }
    }

    public class PaymentResponse
    {
        public bool Success { get; set; }
        public string OrderId { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string ResponseCode { get; set; }
        public string Message { get; set; }
    }
} 