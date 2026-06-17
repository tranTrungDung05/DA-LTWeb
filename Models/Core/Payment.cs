namespace smart_hostel_management_system.Models.Core
{
    public class Payment
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public virtual Invoice? Invoice { get; set; }

        public decimal Amount { get; set; }
        public DateTime DateTimePaidAt { get; set; } = DateTime.Now;
        public string PaymentMethod { get; set; } = "VietQR";
        public string? TransactionCode { get; set; }
        public string? Note { get; set; }
    }
}