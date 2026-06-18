using smart_hostel_management_system.Models.Enums;

namespace smart_hostel_management_system.Models.Core
{
    public class Room
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public double Area { get; set; }
        public decimal MonthlyPrice { get; set; }
        public int MaxOccupants { get; set; }
        public RoomStatus Status { get; set; } = RoomStatus.Available;
        public string? Note { get; set; }

        public virtual ICollection<TenantRoom> TenantRooms { get; set; } = new List<TenantRoom>();
        public virtual ICollection<ContractDetail> ContractDetails { get; set; } = new List<ContractDetail>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}