using System.ComponentModel.DataAnnotations;

namespace smart_hostel_management_system.Models.ViewModels;

public class ProfileViewModel
{
    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(30)]
    public string? PhoneNumber { get; set; }

    [StringLength(240)]
    public string? Address { get; set; }
}
