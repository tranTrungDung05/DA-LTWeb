namespace smart_hostel_management_system.Models.Core
{
    public class InvoiceDetail
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public virtual Invoice? Invoice { get; set; }

        public int ServiceId { get; set; }
        public virtual Service? Service { get; set; }

        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }
}