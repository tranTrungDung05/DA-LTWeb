using smart_hostel_management_system.Models.Enums;

namespace smart_hostel_management_system.Models.Core
{
    public class Contract
    {
        public int Id { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public ContractStatus Status { get; set; } = ContractStatus.Active;

        public int TenantId { get; set; }
        public virtual Tenant? Tenant { get; set; }

        public string? Note { get; set; }

        public virtual ICollection<ContractDetail> ContractDetails { get; set; } = new List<ContractDetail>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<ContractService> ContractServices { get; set; } = new List<ContractService>();
    }
}