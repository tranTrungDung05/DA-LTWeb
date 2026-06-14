using System.ComponentModel.DataAnnotations;

namespace smart_hostel_management_system.Models.Core;

public class RoomAsset
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    [StringLength(160)]
    public string? Condition { get; set; }

    public int RoomId { get; set; }

    public Room? Room { get; set; }
}
