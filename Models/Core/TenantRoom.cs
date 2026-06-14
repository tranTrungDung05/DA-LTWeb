namespace smart_hostel_management_system.Models.Core;

public class TenantRoom
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public Tenant? Tenant { get; set; }

    public int RoomId { get; set; }

    public Room? Room { get; set; }

    public DateTime MoveInDate { get; set; } = DateTime.Today;

    public DateTime? MoveOutDate { get; set; }

    public bool IsCurrent { get; set; } = true;
}
