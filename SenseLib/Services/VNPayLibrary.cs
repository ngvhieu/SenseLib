using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace SenseLib.Services
{
    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayComparer());
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayComparer());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string CreateRequestUrl(string baseUrl, string secretKey)
        {
            StringBuilder data = new StringBuilder();

            // Sắp xếp các tham số theo thứ tự key
            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    // Mã hóa URL các tham số theo đúng chuẩn RFC 3986
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            // Xóa dấu & cuối cùng
            string queryString = data.ToString();
            if (queryString.Length > 0)
            {
                queryString = queryString.Remove(queryString.Length - 1, 1);
            }

            // Tạo chuỗi URL cơ bản
            string rawData = queryString;
            baseUrl = baseUrl + "?" + rawData;

            // Tạo chữ ký bảo mật
            string vnp_SecureHash = HmacSHA512(secretKey, rawData);
            baseUrl += "&vnp_SecureHash=" + vnp_SecureHash;

            return baseUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            try
            {
                string rspRaw = GetResponseData();
                string myChecksum = HmacSHA512(secretKey, rspRaw);
                return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                Console.WriteLine($"Lỗi khi xác thực chữ ký: {ex.Message}");
                return false;
            }
        }

        private string GetResponseData()
        {
            StringBuilder data = new StringBuilder();
            
            // Xóa các trường không cần thiết cho việc tính toán chữ ký
            if (_responseData.ContainsKey("vnp_SecureHashType"))
            {
                _responseData.Remove("vnp_SecureHashType");
            }
            if (_responseData.ContainsKey("vnp_SecureHash"))
            {
                _responseData.Remove("vnp_SecureHash");
            }
            
            // Tạo chuỗi dữ liệu đã sắp xếp
            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            
            // Xóa dấu & cuối cùng
            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }
            
            return data.ToString();
        }

        private string HmacSHA512(string key, string inputData)
        {
            try
            {
                var hash = new StringBuilder();
                byte[] keyBytes = Encoding.UTF8.GetBytes(key);
                byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
                using (var hmac = new HMACSHA512(keyBytes))
                {
                    byte[] hashValue = hmac.ComputeHash(inputBytes);
                    foreach (var b in hashValue)
                    {
                        hash.Append(b.ToString("x2"));
                    }
                }
                return hash.ToString();
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                Console.WriteLine($"Lỗi khi tạo HMAC: {ex.Message}");
                return "";
            }
        }
    }

    public class VnPayComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
} 