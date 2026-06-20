using smart_hostel_management_system.Models.Enums;

namespace smart_hostel_management_system.Models.Core
{
    public class Payment
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public virtual Invoice? Invoice { get; set; }

        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DateTimePaidAt { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public string ProviderOrderId { get; set; } = string.Empty;
        public string? RequestId { get; set; }
        public string? PaymentUrl { get; set; }
        public string? QrCodeData { get; set; }
        public string? TransactionCode { get; set; }
        public string? ProviderResponseCode { get; set; }
        public string? Note { get; set; }
    }
}
