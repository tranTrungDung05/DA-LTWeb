using System.ComponentModel.DataAnnotations;

namespace smart_hostel_management_system.Models.Core;

public class AppUser
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(240)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RoleId { get; set; }

    public Role? Role { get; set; }
}
