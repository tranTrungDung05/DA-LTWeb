using System.ComponentModel.DataAnnotations;

namespace smart_hostel_management_system.Models.Core;

public class Property
{
    public int Id { get; set; }

    [Required, StringLength(140)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(240)]
    public string Address { get; set; } = string.Empty;

    [StringLength(30)]
    public string? PhoneNumber { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
