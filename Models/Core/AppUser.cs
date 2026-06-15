using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace smart_hostel_management_system.Models.Core;

public class AppUser : IdentityUser<int>
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(240)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
