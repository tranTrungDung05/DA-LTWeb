using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smart_hostel_management_system.Models.Core;

public enum RoomStatus
{
    Available = 1,
    Occupied = 2,
    Maintenance = 3,
    Locked = 4
}

public class Room
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [Range(1, 1000)]
    public double Area { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 1_000_000_000)]
    public decimal MonthlyPrice { get; set; }

    [Range(0, 100)]
    public int MaxOccupants { get; set; } = 2;

    public RoomStatus Status { get; set; } = RoomStatus.Available;

    [StringLength(500)]
    public string? Note { get; set; }

    public int PropertyId { get; set; }

    public Property? Property { get; set; }

    public ICollection<RoomAsset> Assets { get; set; } = new List<RoomAsset>();

    public ICollection<TenantRoom> TenantRooms { get; set; } = new List<TenantRoom>();

    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
