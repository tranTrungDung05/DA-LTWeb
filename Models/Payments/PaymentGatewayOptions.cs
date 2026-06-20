namespace smart_hostel_management_system.Models.Payments
{
    public class PaymentGatewayOptions
    {
        public string? PublicBaseUrl { get; set; }
        public MomoOptions Momo { get; set; } = new();
        public VnPayOptions VnPay { get; set; } = new();
    }

    public class MomoOptions
    {
        public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";
        public string PartnerCode { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
    }

    public class VnPayOptions
    {
        public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        public string TmnCode { get; set; } = string.Empty;
        public string HashSecret { get; set; } = string.Empty;
        public string Version { get; set; } = "2.1.0";
    }
}
