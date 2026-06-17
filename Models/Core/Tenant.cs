using smart_hostel_management_system.Models.Enums;

namespace smart_hostel_management_system.Models.Core
{
    public class Tenant
    {
        public int Id { get; set; }
        public string IdentityNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PermanentAddress { get; set; }
        public TenantStatus Status { get; set; } = TenantStatus.Active;

        public int? AccountId { get; set; }
        public virtual Account? Account { get; set; }

        public virtual ICollection<TenantRoom> TenantRooms { get; set; } = new List<TenantRoom>();
        public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}