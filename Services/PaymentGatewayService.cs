using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using smart_hostel_management_system.Models.Payments;

namespace smart_hostel_management_system.Services
{
    public class PaymentGatewayService
    {
        private readonly HttpClient _httpClient;
        private readonly PaymentGatewayOptions _options;

        public PaymentGatewayService(
            HttpClient httpClient,
            IOptions<PaymentGatewayOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<PaymentLinkResult> CreateMomoPaymentAsync(
            string orderId,
            long amount,
            string orderInfo,
            string returnUrl,
            string ipnUrl)
        {
            var options = _options.Momo;
            EnsureConfigured(options.PartnerCode, options.AccessKey, options.SecretKey, "MoMo");

            var requestId = Guid.NewGuid().ToString("N");
            const string requestType = "captureWallet";
            const string extraData = "";
            var rawSignature =
                $"accessKey={options.AccessKey}&amount={amount}&extraData={extraData}" +
                $"&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}" +
                $"&partnerCode={options.PartnerCode}&redirectUrl={returnUrl}" +
                $"&requestId={requestId}&requestType={requestType}";

            var request = new
            {
                partnerCode = options.PartnerCode,
                requestId,
                amount,
                orderId,
                orderInfo,
                redirectUrl = returnUrl,
                ipnUrl,
                requestType,
                extraData,
                lang = "vi",
                signature = HmacSha256(options.SecretKey, rawSignature)
            };

            using var response = await _httpClient.PostAsJsonAsync(options.Endpoint, request);
            var content = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var result = JsonSerializer.Deserialize<MomoCreateResponse>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null || result.ResultCode != 0 || string.IsNullOrWhiteSpace(result.PayUrl))
            {
                throw new InvalidOperationException(result?.Message ?? "MoMo không trả về link thanh toán.");
            }

            var responseSignature =
                $"accessKey={options.AccessKey}&amount={result.Amount}&orderId={result.OrderId}" +
                $"&partnerCode={result.PartnerCode}&payUrl={result.PayUrl}&requestId={result.RequestId}" +
                $"&responseTime={result.ResponseTime}&resultCode={result.ResultCode}";
            if (!FixedTimeEquals(result.Signature, HmacSha256(options.SecretKey, responseSignature)))
            {
                throw new InvalidOperationException("Chữ ký phản hồi từ MoMo không hợp lệ.");
            }

            return new PaymentLinkResult(
                result.PayUrl,
                string.IsNullOrWhiteSpace(result.QrCodeUrl) ? result.PayUrl : result.QrCodeUrl,
                requestId);
        }

        public PaymentLinkResult CreateVnPayPayment(
            string orderId,
            long amount,
            string orderInfo,
            string returnUrl,
            string ipAddress)
        {
            var options = _options.VnPay;
            EnsureConfigured(options.TmnCode, options.HashSecret, options.PaymentUrl, "VNPay");

            var now = DateTime.Now;
            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"] = options.Version,
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = options.TmnCode,
                ["vnp_Amount"] = (amount * 100).ToString(CultureInfo.InvariantCulture),
                ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = ipAddress,
                ["vnp_Locale"] = "vn",
                ["vnp_OrderInfo"] = orderInfo,
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl,
                ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss"),
                ["vnp_TxnRef"] = orderId
            };

            var query = BuildQuery(parameters);
            var secureHash = HmacSha512(options.HashSecret, query);
            var paymentUrl = $"{options.PaymentUrl}?{query}&vnp_SecureHash={secureHash}";

            return new PaymentLinkResult(paymentUrl, paymentUrl, orderId);
        }

        public bool ValidateMomoCallback(MomoCallback callback)
        {
            var options = _options.Momo;
            if (string.IsNullOrWhiteSpace(options.AccessKey)
                || string.IsNullOrWhiteSpace(options.SecretKey)
                || callback.PartnerCode != options.PartnerCode)
            {
                return false;
            }

            var rawSignature =
                $"accessKey={options.AccessKey}&amount={callback.Amount}&extraData={callback.ExtraData}" +
                $"&message={callback.Message}&orderId={callback.OrderId}&orderInfo={callback.OrderInfo}" +
                $"&orderType={callback.OrderType}&partnerCode={callback.PartnerCode}&payType={callback.PayType}" +
                $"&requestId={callback.RequestId}&responseTime={callback.ResponseTime}" +
                $"&resultCode={callback.ResultCode}&transId={callback.TransId}";

            return FixedTimeEquals(callback.Signature, HmacSha256(options.SecretKey, rawSignature));
        }

        public bool ValidateVnPayCallback(IQueryCollection query)
        {
            var secureHash = query["vnp_SecureHash"].ToString();
            if (string.IsNullOrWhiteSpace(secureHash)
                || string.IsNullOrWhiteSpace(_options.VnPay.HashSecret)
                || query["vnp_TmnCode"].ToString() != _options.VnPay.TmnCode)
            {
                return false;
            }

            var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in query)
            {
                if (item.Key.StartsWith("vnp_", StringComparison.Ordinal)
                    && item.Key != "vnp_SecureHash"
                    && item.Key != "vnp_SecureHashType")
                {
                    parameters[item.Key] = item.Value.ToString();
                }
            }

            return FixedTimeEquals(
                secureHash,
                HmacSha512(_options.VnPay.HashSecret, BuildQuery(parameters)));
        }

        private static string BuildQuery(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            return string.Join("&", parameters
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Value))
                .Select(parameter =>
                    $"{WebUtility.UrlEncode(parameter.Key)}={WebUtility.UrlEncode(parameter.Value)}"));
        }

        private static string HmacSha256(string key, string value)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        }

        private static string HmacSha512(string key, string value)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        }

        private static bool FixedTimeEquals(string actual, string expected)
        {
            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected))
            {
                return false;
            }

            var actualBytes = Encoding.UTF8.GetBytes(actual.ToLowerInvariant());
            var expectedBytes = Encoding.UTF8.GetBytes(expected.ToLowerInvariant());
            return actualBytes.Length == expectedBytes.Length
                && CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
        }

        private static void EnsureConfigured(string first, string second, string third, string provider)
        {
            if (string.IsNullOrWhiteSpace(first)
                || string.IsNullOrWhiteSpace(second)
                || string.IsNullOrWhiteSpace(third))
            {
                throw new InvalidOperationException(
                    $"Chưa cấu hình thông tin sandbox cho {provider}.");
            }
        }

        private class MomoCreateResponse
        {
            public string PartnerCode { get; set; } = string.Empty;
            public string OrderId { get; set; } = string.Empty;
            public string RequestId { get; set; } = string.Empty;
            public long Amount { get; set; }
            public long ResponseTime { get; set; }
            public int ResultCode { get; set; }
            public string Message { get; set; } = string.Empty;
            public string PayUrl { get; set; } = string.Empty;
            public string QrCodeUrl { get; set; } = string.Empty;
            public string Signature { get; set; } = string.Empty;
        }
    }
}
