namespace smart_hostel_management_system.Models.Core
{
    public class TenantRoom
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public virtual Tenant? Tenant { get; set; }

        public int RoomId { get; set; }
        public virtual Room? Room { get; set; }

        public bool IsRepresentative { get; set; } = false;
        public bool IsCurrent { get; set; } = true;
    }
}