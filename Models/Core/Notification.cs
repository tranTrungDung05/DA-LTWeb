namespace smart_hostel_management_system.Models.Core
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string TargetType { get; set; } = "All";

        public int? RoomId { get; set; }
        public virtual Room? Room { get; set; }

        public int? TenantId { get; set; }
        public virtual Tenant? Tenant { get; set; }
    }
}