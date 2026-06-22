using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using smart_hostel_management_system.Models.Payments;

namespace smart_hostel_management_system.Services
{
    public class PaymentGatewayService
    {
        private readonly PaymentGatewayOptions _options;

        public PaymentGatewayService(IOptions<PaymentGatewayOptions> options)
        {
            _options = options.Value;
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

    }
}
