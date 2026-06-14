using System.ComponentModel.DataAnnotations;

namespace smart_hostel_management_system.Models.Core;

public class Role
{
    public int Id { get; set; }

    [Required, StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [StringLength(160)]
    public string? Description { get; set; }

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
