using smart_hostel_management_system.Models.Enums;

namespace smart_hostel_management_system.Models.Core
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int Month { get; set; }
        public int Year { get; set; }
        public string? MainContent { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; }
        public bool IsOutDatePay { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;

        public int RoomId { get; set; }
        public virtual Room? Room { get; set; }

        public int TenantId { get; set; }
        public virtual Tenant? Tenant { get; set; }

        public int? ContractId { get; set; }
        public virtual Contract? Contract { get; set; }

        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
